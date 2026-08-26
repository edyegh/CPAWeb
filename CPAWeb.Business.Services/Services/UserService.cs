using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CPAWeb.Data.Interface;
using CPAWeb.Data.Model;
using CPAWeb.Services.DTOs;
using CPAWeb.Services.Interface;

namespace CPAWeb.Business.Services.Services
{
    public class UserService : IUserService
    {
        // Նվազագույն երկարությունը՝ և՛ "users" էջի, և՛ seed ադմինների համար
        public const int MinPasswordLength = 6;

        // Չգտնված մուտքանունն ու սխալ գաղտնաբառը նույն պատասխանն են ստանում
        private const string InvalidCredentialsMessage = "invalid username or password.";

        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // =====================================================================
        // ՄՈՒՏՔ
        // Մուտք գործել կարող են միայն cpa_web_user աղյուսակում առկա (ադմինի
        // ավելացրած) և ակտիվ օգտատերերը: Մուտքանունը կարող է լինել
        // ամբողջական email ("test@gmail.com") կամ միայն '@'-ից առաջվա մասը ("test"):
        // =====================================================================
        public async Task<LoginResultDto> AuthenticateAsync(LoginRequestDto request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Login) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return LoginFailure(InvalidCredentialsMessage);
            }

            var matches = await _userRepository.FindByLoginAsync(request.Login.Trim());

            // Կարճ մուտքանունը կարող է համապատասխանել մի քանի email-ի
            // (test@gmail.com և test@yahoo.com) — այդ դեպքում պահանջում ենք ամբողջականը
            if (matches.Count > 1)
                return LoginFailure("several users share this username — please enter the full email address.");

            var user = matches.Count == 1 ? matches[0] : null;

            // Չգտնված մուտքանվան և սխալ գաղտնաբառի պատասխանը նույնն է՝ չհուշելու համար
            if (user == null || !user.IsActive)
                return LoginFailure(InvalidCredentialsMessage);

            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
                return LoginFailure(InvalidCredentialsMessage);

            return new LoginResultDto
            {
                Success = true,
                Message = "signed in successfully.",
                User = ToDto(user)
            };
        }

        public async Task<List<UserDto>> GetUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(ToDto).ToList();
        }

        // =====================================================================
        // ՆՈՐ ՕԳՏԱՏԵՐ (միայն ադմինի կողմից)
        // =====================================================================
        public async Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto, string createdBy)
        {
            var validationMessage = Validate(dto);

            if (validationMessage != null)
                return Failure(validationMessage);

            string email = dto.Email.Trim();

            if (await _userRepository.EmailExistsAsync(email))
                return Failure($"a user with the email '{email}' already exists.");

            var user = new AppUser
            {
                Name = dto.Name.Trim(),
                Email = email,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                Role = NormalizeRole(dto.Role),
                IsActive = true,
                CreatedBy = createdBy ?? string.Empty
            };

            user.Id = await _userRepository.CreateAsync(user);
            user.CreatedAt = DateTime.UtcNow;

            return new CreateUserResultDto
            {
                Success = true,
                Message = $"user '{user.Name}' has been added.",
                User = ToDto(user)
            };
        }

        // =====================================================================
        // ՋՆՋՈՒՄ (միայն ադմինի կողմից)
        // Ադմինը չի կարող ջնջել ինքն իրեն — դա և պատահական ելքից է պաշտպանում,
        // և երաշխավորում է, որ գոնե մեկ ադմին միշտ մնա:
        // =====================================================================
        public async Task<DeleteUserResultDto> DeleteUserAsync(long id, string requestedByEmail)
        {
            var user = await _userRepository.GetByIdAsync(id);

            if (user == null)
                return new DeleteUserResultDto { Success = false, Message = "user not found." };

            if (string.Equals(user.Email, requestedByEmail?.Trim(), StringComparison.OrdinalIgnoreCase))
                return new DeleteUserResultDto { Success = false, Message = "you cannot delete your own account." };

            if (!await _userRepository.DeleteAsync(id))
                return new DeleteUserResultDto { Success = false, Message = "user not found." };

            return new DeleteUserResultDto
            {
                Success = true,
                Message = $"user '{user.Name}' has been deleted."
            };
        }

        // =====================================================================
        // SEED ԱԴՄԻՆՆԵՐ
        // Ծրագրի մեկնարկին ստեղծվում են appsettings-ի "SeedAdmins"-ի այն
        // գրառումները, որոնց email-ը դեռ աղյուսակում չկա: Արդեն գոյություն
        // ունեցող ադմինի գաղտնաբառը չի փոխվում:
        // =====================================================================
        public async Task<int> EnsureSeedAdminsAsync(IEnumerable<CreateUserDto> admins)
        {
            if (admins == null)
                return 0;

            int created = 0;

            foreach (var admin in admins)
            {
                if (admin == null || Validate(admin) != null)
                    continue;

                string email = admin.Email.Trim();

                if (await _userRepository.EmailExistsAsync(email))
                    continue;

                await _userRepository.CreateAsync(new AppUser
                {
                    Name = admin.Name.Trim(),
                    Email = email,
                    PasswordHash = PasswordHasher.Hash(admin.Password),
                    Role = UserRoles.Admin,
                    IsActive = true,
                    CreatedBy = "system"
                });

                created++;
            }

            return created;
        }

        // Դատարկ/սխալ դաշտերի ստուգում — null նշանակում է վավեր
        private static string? Validate(CreateUserDto dto)
        {
            if (dto == null)
                return "no data was supplied.";

            if (string.IsNullOrWhiteSpace(dto.Name))
                return "name is required.";

            if (string.IsNullOrWhiteSpace(dto.Email))
                return "email is required.";

            if (!IsValidEmail(dto.Email.Trim()))
                return "invalid email format.";

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < MinPasswordLength)
                return $"password must be at least {MinPasswordLength} characters.";

            return null;
        }

        // Պարզ ձևաչափի ստուգում — մեկ '@', դրանից հետո կետ, առանց բացատների
        private static bool IsValidEmail(string email)
        {
            int at = email.IndexOf('@');
            int lastDot = email.LastIndexOf('.');

            return at > 0
                && at == email.LastIndexOf('@')
                && lastDot > at + 1
                && lastDot < email.Length - 1
                && !email.Any(char.IsWhiteSpace);
        }

        private static string NormalizeRole(string? role)
        {
            return string.Equals(role?.Trim(), UserRoles.Admin, StringComparison.OrdinalIgnoreCase)
                ? UserRoles.Admin
                : UserRoles.User;
        }

        private static CreateUserResultDto Failure(string message)
        {
            return new CreateUserResultDto { Success = false, Message = message };
        }

        private static LoginResultDto LoginFailure(string message)
        {
            return new LoginResultDto { Success = false, Message = message };
        }

        private static UserDto ToDto(AppUser user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                CreatedBy = user.CreatedBy
            };
        }
    }
}
