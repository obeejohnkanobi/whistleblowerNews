using System.ComponentModel.DataAnnotations;

namespace WhistleblowerNews.Web.Models.Account;

public sealed class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
