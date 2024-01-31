namespace eShop.Identity.API.Models.ManageViewModels
{
    public record AddPhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; init; }
    }
}
