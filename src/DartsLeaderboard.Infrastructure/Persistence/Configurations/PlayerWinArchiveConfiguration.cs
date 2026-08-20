using DartsLeaderboard.Domain.Players;
using DartsLeaderboard.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartsLeaderboard.Infrastructure.Persistence.Configurations;

public sealed class PlayerWinArchiveConfiguration : IEntityTypeConfiguration<PlayerWinArchive>
{
    public void Configure(EntityTypeBuilder<PlayerWinArchive> builder)
    {
        builder.ToTable("player_win_archive");
        builder.HasKey(a => a.PlayerId);
        builder.Property(a => a.PlayerId).HasColumnName("player_id");
        builder.Property(a => a.WinsX01).HasColumnName("wins_x01");
        builder.Property(a => a.WinsHighestTotal).HasColumnName("wins_highest_total");

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(a => a.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
