using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartsLeaderboard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerWinArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_win_archive",
                columns: table => new
                {
                    player_id = table.Column<int>(type: "integer", nullable: false),
                    wins_x01 = table.Column<int>(type: "integer", nullable: false),
                    wins_highest_total = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_win_archive", x => x.player_id);
                    table.ForeignKey(
                        name: "FK_player_win_archive_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_win_archive");
        }
    }
}
