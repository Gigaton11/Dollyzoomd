using DollyZoomd.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DollyZoomd.Data.Configurations;

public class UserCommentVoteConfiguration : IEntityTypeConfiguration<UserCommentVote>
{
    public void Configure(EntityTypeBuilder<UserCommentVote> builder)
    {
        builder.ToTable("user_comment_votes");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Comment)
            .WithMany(x => x.Votes)
            .HasForeignKey(x => x.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.CommentVotes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CommentId, x.UserId })
            .IsUnique();
    }
}
