using System;

namespace CPAWeb.Services.DTOs
{
    // Հաջող մուտքից հետո client-ին վերադարձվող տվյալները
    public class LoginResultDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        // JWT — client-ը պահում է localStorage-ում և ուղարկում Authorization header-ով
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public UserDto? User { get; set; }
    }
}
