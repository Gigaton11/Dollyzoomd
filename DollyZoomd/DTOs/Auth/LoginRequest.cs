using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DollyZoomd.DTOs.Auth;

public class LoginRequest
{
    [Required]
    [StringLength(120, MinimumLength = 3)]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    // Backward compatibility for older clients that still post "email".
    [JsonPropertyName("email")]
    public string? LegacyEmail
    {
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Identifier = value;
            }
        }
    }

    // Optional compatibility for clients that post "username" directly.
    [JsonPropertyName("username")]
    public string? LegacyUsername
    {
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Identifier = value;
            }
        }
    }
}
