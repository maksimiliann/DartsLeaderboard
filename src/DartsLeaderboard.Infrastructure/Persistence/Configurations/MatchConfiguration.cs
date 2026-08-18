using DartsLeaderboard.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartsLeaderboard.Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.Mode).HasColumnName("mode").HasConversion<int>();
        builder.Property(m => m.StartingScore).HasColumnName("starting_score");
        builder.Property(m => m.RoundLimit).HasColumnName("round_limit");
        builder.Property(m => m.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(m => m.StartedAt).HasColumnName("started_at");
        builder.Property(m => m.FinishedAt).HasColumnName("finished_at");
        builder.Property(m => m.WinnerParticipantId).HasColumnName("winner_participant_id");

        builder.HasMany(m => m.AllParticipants)
            .WithOne()
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.AllThrows)
            .WithOne()
            .HasForeignKey(t => t.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.AllParticipants)
            .HasField("_participants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(m => m.AllThrows)
            .HasField("_throws")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(m => m.Participants);
        builder.Ignore(m => m.Throws);
    }
}
