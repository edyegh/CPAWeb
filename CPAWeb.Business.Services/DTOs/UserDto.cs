using System;

namespace CPAWeb.Services.DTOs
{
    // Օգտատերը՝ առանց գաղտնաբառի հեշի
    public class UserDto
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // "Admin" կամ "User"
        public string Role { get; set; } = "User";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }
}
