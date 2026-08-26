namespace CPAWeb.Services.DTOs
{
    public class CreateUserResultDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public UserDto? User { get; set; }
    }
}
