namespace EShop.UseCases.Identity.Dtos
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string NormalizedUserName {get; set; }
        public string Email {get;}
        public string NormalizedEmail {get; set; }
        public bool EmailConfirmed {get; set; }
        public string SecurityStamp {get; set; }
        public string PasswordHash {get; set; }
        public bool LockoutEnabled {get; set; }
        public int AccessFailedCount { get; set; }
    }
}