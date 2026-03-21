namespace DollyZoomd.DTOs.Shows;

public class CommentListDto
{
    public IReadOnlyList<CommentDto> Comments { get; set; } = Array.Empty<CommentDto>();
    public int TotalCount { get; set; }
}
