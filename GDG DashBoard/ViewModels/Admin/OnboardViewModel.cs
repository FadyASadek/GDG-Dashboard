using System.ComponentModel.DataAnnotations;

namespace GDG_DashBoard.ViewModels.Admin;

public class OnboardViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Season / Cohort")]
    [StringLength(50)]
    public string? Season { get; set; }

    [Required(ErrorMessage = "Please select a role.")]
    [Display(Name = "Role")]
    public string Role { get; set; } = "Member";

    /// <summary>Populated only on success — shown to admin after onboarding.</summary>
    public string? GeneratedPasswordResetToken { get; set; }
    public bool OnboardingSucceeded { get; set; }
}
