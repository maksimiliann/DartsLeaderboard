using DartsLeaderboard.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartsLeaderboard.Infrastructure.Persistence.Configurations;

public sealed class ThrowConfiguration : IEntityTypeConfiguration<Throw>
{
    public void Configure(EntityTypeBuilder<Throw> builder)
    {
        builder.ToTable("throws");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.MatchId).HasColumnName("match_id");
        builder.Property(t => t.ParticipantId).HasColumnName("participant_id");
        builder.Property(t => t.RoundNumber).HasColumnName("round_number");
        builder.Property(t => t.Points).HasColumnName("points");
        builder.Property(t => t.RecordedAt).HasColumnName("recorded_at");

        builder.HasOne<MatchParticipant>()
            .WithMany()
            .HasForeignKey(t => t.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.MatchId, t.ParticipantId, t.RoundNumber })
            .IsUnique()
            .HasDatabaseName("ix_throws_match_participant_round");
    }
}
