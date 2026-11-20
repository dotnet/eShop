namespace eShop.Identity.API.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string Street { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
        [Required]
        public string Country { get; set; }
        [Required]
        public string ZipCode { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string LastName { get; set; }
    }
}
