using Fandom_copy.DTOs.Auth;
using Fandom_copy.DTOs.Profile;
using Fandom_copy.Models;

namespace Fandom_copy.Services
{
    public interface IUserService
    {
        Task<ServiceResult<User>> RegisterAsync(RegisterRequestDto dto);
        Task<ServiceResult<User>> LoginAsync(LoginRequestDto dto);

        Task<ServiceResult> ConfirmEmailAsync(Guid userId, string token);
        Task<ServiceResult> ResendEmailConfirmationAsync(ResendConfirmationRequestDto dto);

        Task<ServiceResult> RequestPasswordResetAsync(ForgotPasswordRequestDto dto);
        Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequestDto dto);

        Task<ServiceResult<UserProfileDto>> GetProfileAsync(Guid userId);
        Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
        Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);

        Task<ServiceResult> SetBanStatusAsync(Guid userId, bool isBanned);
    }
}