using System.ComponentModel.DataAnnotations;

namespace ai_clinic.UI.Pages.Auth;

public class AuthModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Credential { get; set; } = "";
    
    public string OtpCode { get; set; } = "";
}
