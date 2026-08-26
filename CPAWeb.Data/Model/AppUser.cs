using System;

namespace CPAWeb.Data.Model
{
    // cpa_web_user աղյուսակի տողը (տես db/003_create_web_users.sql)
    public class AppUser
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // PBKDF2-HMAC-SHA256, բաց տեքստով գաղտնաբառ երբեք չի պահվում
        public string PasswordHash { get; set; } = string.Empty;

        // UserRoles.Admin կամ UserRoles.User
        public string Role { get; set; } = UserRoles.User;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        // Ո՞ր ադմինն է ավելացրել այս օգտատիրոջը
        public string CreatedBy { get; set; } = string.Empty;
    }

    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }
}
