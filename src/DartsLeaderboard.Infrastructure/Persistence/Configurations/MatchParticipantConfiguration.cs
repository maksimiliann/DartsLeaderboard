using DartsLeaderboard.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartsLeaderboard.Infrastructure.Persistence.Configurations;

public sealed class MatchParticipantConfiguration : IEntityTypeConfiguration<MatchParticipant>
{
    public void Configure(EntityTypeBuilder<MatchParticipant> builder)
    {
        builder.ToTable("match_participants");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.MatchId).HasColumnName("match_id");
        builder.Property(p => p.PlayerId).HasColumnName("player_id");
        builder.Property(p => p.SeatOrder).HasColumnName("seat_order");

        builder.HasOne(p => p.Player)
            .WithMany()
            .HasForeignKey(p => p.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.MatchId, p.PlayerId }).IsUnique();
        builder.HasIndex(p => new { p.MatchId, p.SeatOrder }).IsUnique();
        builder.Ignore(p => p.PlayerName);
    }
}
