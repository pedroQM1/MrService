using System.ComponentModel.DataAnnotations;

namespace MrService.Services.Identity.Identity.API.Models.AccountViewModel{


    public record ForgotPasswordViewModel{
        
       [Required]
       [EmailAddress]
        public string Email { get; init; }
    }
}