namespace CPAWeb.Services.DTOs
{
    // "users" էջի ձևը՝ անուն, email, գաղտնաբառ
    public class CreateUserDto
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        // Դատարկ թողնելու դեպքում ստեղծվում է սովորական օգտատեր ("User")
        public string Role { get; set; } = "User";
    }
}
