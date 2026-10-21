using System.ComponentModel.DataAnnotations;

namespace OnlineBanking.Api.Dtos.Auth;

public class LoginRequest
{
    [Required]
    [MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}
