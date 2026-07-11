using Fandom_copy.Data;
using Fandom_copy.DTOs.Auth;
using Fandom_copy.DTOs.Profile;
using Fandom_copy.Models;
using Fandom_copy.Services.Email;
using Fandom_copy.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fandom_copy.Services
{
    public class UserService : IUserService
    {
        private static readonly TimeSpan EmailConfirmationTokenLifetime = TimeSpan.FromHours(24);
        private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;
        private readonly AppSettings _appSettings;

        public UserService(ApplicationDbContext db, IEmailService emailService, IOptions<AppSettings> appSettings)
        {
            _db = db;
            _emailService = emailService;
            _appSettings = appSettings.Value;
        }

        public async Task<ServiceResult<User>> RegisterAsync(RegisterRequestDto dto)
        {
            var login = dto.Login.Trim();
            var email = dto.Email.Trim().ToLowerInvariant();

            bool loginTaken = await _db.Users.AnyAsync(u => u.Login == login);
            if (loginTaken)
                return ServiceResult<User>.Fail("Користувач з таким логіном вже існує");

            bool emailTaken = await _db.Users.AnyAsync(u => u.Email == email);
            if (emailTaken)
                return ServiceResult<User>.Fail("Користувач з таким email вже існує");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Login = login,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                GlobalRole = GlobalRole.User,
                RegistrationDate = DateTime.UtcNow,
                IsBanned = false,
                EmailConfirmed = false
            };

            SetEmailConfirmationToken(user);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await SendEmailConfirmationLinkAsync(user);

            return ServiceResult<User>.Ok(user);
        }

        public async Task<ServiceResult<User>> LoginAsync(LoginRequestDto dto)
        {
            var loginOrEmail = dto.LoginOrEmail.Trim();

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Login == loginOrEmail || u.Email == loginOrEmail.ToLower());

            if (user is null)
                return ServiceResult<User>.Fail("Невірний логін/email або пароль");

            if (user.IsBanned)
                return ServiceResult<User>.Fail("Акаунт заблоковано");

            bool passwordOk = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!passwordOk)
                return ServiceResult<User>.Fail("Невірний логін/email або пароль");

            // Пошту можна не підтверджувати, щоб не блокувати вхід новим користувачам,
            // фронтенд може показати банер "підтвердіть email" за user.EmailConfirmed.
            return ServiceResult<User>.Ok(user);
        }

        public async Task<ServiceResult<User>> ExternalLoginAsync(string email, string? displayName)
        {
            email = email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult<User>.Fail("Провайдер не повернув email. Дозвольте доступ до email у Google/Facebook.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is not null)
            {
                if (user.IsBanned)
                    return ServiceResult<User>.Fail("Акаунт заблоковано");

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    user.EmailConfirmationToken = null;
                    user.EmailConfirmationTokenExpiresAt = null;
                    await _db.SaveChangesAsync();
                }

                return ServiceResult<User>.Ok(user);
            }

            user = new User
            {
                Id = Guid.NewGuid(),
                Login = await GenerateUniqueLoginAsync(displayName, email),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(TokenGenerator.Generate(64)),
                GlobalRole = GlobalRole.User,
                RegistrationDate = DateTime.UtcNow,
                IsBanned = false,
                EmailConfirmed = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return ServiceResult<User>.Ok(user);
        }

        public async Task<ServiceResult> ConfirmEmailAsync(Guid userId, string token)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null)
                return ServiceResult.Fail("Користувача не знайдено");

            if (user.EmailConfirmed)
                return ServiceResult.Ok();

            if (string.IsNullOrEmpty(user.EmailConfirmationToken) ||
                user.EmailConfirmationTokenExpiresAt is null ||
                user.EmailConfirmationTokenExpiresAt < DateTime.UtcNow ||
                !string.Equals(user.EmailConfirmationToken, token, StringComparison.Ordinal))
            {
                return ServiceResult.Fail("Посилання для підтвердження недійсне або застаріле");
            }

            user.EmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiresAt = null;
            await _db.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> ResendEmailConfirmationAsync(ResendConfirmationRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Не розкриваємо, чи існує такий email - завжди повертаємо Ok
            if (user is null || user.EmailConfirmed)
                return ServiceResult.Ok();

            SetEmailConfirmationToken(user);
            await _db.SaveChangesAsync();

            await SendEmailConfirmationLinkAsync(user);

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> RequestPasswordResetAsync(ForgotPasswordRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Навмисно не повідомляємо, чи існує акаунт з таким email (захист від enumeration)
            if (user is null || user.IsBanned)
                return ServiceResult.Ok();

            user.PasswordResetToken = TokenGenerator.Generate();
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.Add(PasswordResetTokenLifetime);
            await _db.SaveChangesAsync();

            var resetLink = BuildClientLink("Account/ResetPassword", new Dictionary<string, string>
            {
                ["email"] = user.Email,
                ["token"] = user.PasswordResetToken
            });

            await _emailService.SendPasswordResetAsync(user.Email, user.Login, resetLink);

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user is null ||
                string.IsNullOrEmpty(user.PasswordResetToken) ||
                user.PasswordResetTokenExpiresAt is null ||
                user.PasswordResetTokenExpiresAt < DateTime.UtcNow ||
                !string.Equals(user.PasswordResetToken, dto.Token, StringComparison.Ordinal))
            {
                return ServiceResult.Fail("Посилання для відновлення паролю недійсне або застаріле");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;
            await _db.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult<UserProfileDto>> GetProfileAsync(Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null)
                return ServiceResult<UserProfileDto>.Fail("Користувача не знайдено");

            return ServiceResult<UserProfileDto>.Ok(UserProfileDto.FromUser(user));
        }

        public async Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null)
                return ServiceResult<UserProfileDto>.Fail("Користувача не знайдено");

            var login = dto.Login.Trim();
            var email = dto.Email.Trim().ToLowerInvariant();

            bool loginTaken = await _db.Users.AnyAsync(u => u.Login == login && u.Id != userId);
            if (loginTaken)
                return ServiceResult<UserProfileDto>.Fail("Такий логін вже зайнятий");

            bool emailTaken = await _db.Users.AnyAsync(u => u.Email == email && u.Id != userId);
            if (emailTaken)
                return ServiceResult<UserProfileDto>.Fail("Такий email вже зайнятий");

            bool emailChanged = user.Email != email;

            user.Login = login;
            user.Email = email;

            if (emailChanged)
            {
                // При зміні email потрібно підтвердити його заново
                user.EmailConfirmed = false;
                SetEmailConfirmationToken(user);
            }

            await _db.SaveChangesAsync();

            if (emailChanged)
                await SendEmailConfirmationLinkAsync(user);

            return ServiceResult<UserProfileDto>.Ok(UserProfileDto.FromUser(user));
        }

        public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null)
                return ServiceResult.Fail("Користувача не знайдено");

            bool oldPasswordOk = BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash);
            if (!oldPasswordOk)
                return ServiceResult.Fail("Поточний пароль вказано невірно");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _db.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> SetBanStatusAsync(Guid userId, bool isBanned)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null)
                return ServiceResult.Fail("Користувача не знайдено");

            user.IsBanned = isBanned;
            await _db.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        private static void SetEmailConfirmationToken(User user)
        {
            user.EmailConfirmationToken = TokenGenerator.Generate();
            user.EmailConfirmationTokenExpiresAt = DateTime.UtcNow.Add(EmailConfirmationTokenLifetime);
        }

        private async Task<string> GenerateUniqueLoginAsync(string? displayName, string email)
        {
            var source = string.IsNullOrWhiteSpace(displayName)
                ? email.Split('@')[0]
                : displayName.Trim();

            var baseLogin = new string(source
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
                .ToArray())
                .Trim('_');

            if (baseLogin.Length < 3)
                baseLogin = "user";

            if (baseLogin.Length > 24)
                baseLogin = baseLogin[..24].Trim('_');

            var login = baseLogin;
            var suffix = 1;

            while (await _db.Users.AnyAsync(u => u.Login == login))
            {
                var suffixText = suffix.ToString();
                var maxBaseLength = Math.Min(baseLogin.Length, 32 - suffixText.Length - 1);
                login = $"{baseLogin[..maxBaseLength]}_{suffixText}";
                suffix++;
            }

            return login;
        }

        private async Task SendEmailConfirmationLinkAsync(User user)
        {
            var confirmationLink = BuildClientLink("Account/ConfirmEmail", new Dictionary<string, string>
            {
                ["userId"] = user.Id.ToString(),
                ["token"] = user.EmailConfirmationToken!
            });

            await _emailService.SendEmailConfirmationAsync(user.Email, user.Login, confirmationLink);
        }

        private string BuildClientLink(string path, Dictionary<string, string> queryParams)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_appSettings.ClientBaseUrl)
                ? "http://localhost:5000"
                : _appSettings.ClientBaseUrl.TrimEnd('/');

            var query = string.Join("&", queryParams.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            return $"{baseUrl}/{path}?{query}";
        }
    }
}
