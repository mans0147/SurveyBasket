using System.ComponentModel.DataAnnotations;

namespace SurveyBasket.API.Authentication;

public class JwtOptions
{
    public static string SectionName = "JWT";

    [Required]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ExpirayMinutes { get; set; }
}
