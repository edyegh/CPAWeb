namespace CPAWeb.Services.DTOs
{
    public class LoginRequestDto
    {
        // Ամբողջական email ("test@gmail.com") կամ միայն '@'-ից առաջվա մասը ("test")
        public string Login { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
