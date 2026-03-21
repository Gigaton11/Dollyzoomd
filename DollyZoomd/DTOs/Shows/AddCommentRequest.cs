using System.ComponentModel.DataAnnotations;

namespace DollyZoomd.DTOs.Shows;

public class AddCommentRequest
{
    [Required]
    [MaxLength(240)]
    public string Text { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ShowName { get; set; } = string.Empty;

    public string? PosterUrl { get; set; }
    public string? GenresCsv { get; set; }
}
