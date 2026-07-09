using Fandom_copy.Data;
using Fandom_copy.DTOs.Auth;
using Fandom_copy.DTOs.Profile;
using Fandom_copy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;

        public UserService(ApplicationDbContext db)
        {
            _db = db;
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
                IsBanned = false
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

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

            return ServiceResult<User>.Ok(user);
        }

        public async Task<ServiceResult> RequestPasswordResetAsync(ForgotPasswordRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user is null)
                return ServiceResult.Ok();

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

            user.Login = login;
            user.Email = email;
            await _db.SaveChangesAsync();

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
    }
}