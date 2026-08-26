using System.Collections.Generic;
using System.Threading.Tasks;
using CPAWeb.Services.DTOs;

namespace CPAWeb.Services.Interface
{
    public interface IUserService
    {
        // Մուտք — վերադարձնում է օգտատիրոջը միայն ճիշտ մուտքանվան/գաղտնաբառի
        // և is_active = 1-ի դեպքում: Token-ը լրացնում է AuthController-ը:
        Task<LoginResultDto> AuthenticateAsync(LoginRequestDto request);

        // "users" էջի ցանկը
        Task<List<UserDto>> GetUsersAsync();

        // Նոր օգտատեր — կանչում է միայն ադմինը
        Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto, string createdBy);

        // Օգտատիրոջ ջնջում — կանչում է միայն ադմինը, ինքն իրեն ջնջել չի կարող
        Task<DeleteUserResultDto> DeleteUserAsync(long id, string requestedByEmail);

        // Ծրագրի մեկնարկին՝ appsettings-ի "SeedAdmins"-ից բացակայող ադմինների ստեղծում
        Task<int> EnsureSeedAdminsAsync(IEnumerable<CreateUserDto> admins);
    }
}
