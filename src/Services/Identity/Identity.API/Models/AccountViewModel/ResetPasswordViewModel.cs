using System.ComponentModel.DataAnnotations;

namespace MrService.Services.Identity.Identity.API.Models.AccountViewModel{


    public record ResetPasswordViewModel{
        
        [Required]
        [EmailAddress]
        public string Email { get; init; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; init; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; init; }

        public string Code { get; init; }
    }
}