using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qala.Game.Matches.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "matches");

            migrationBuilder.CreateTable(
                name: "Matches",
                schema: "matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RulesVersion = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ChallengeCode = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: true),
                    CreatorPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatorDisplayName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatorRating = table.Column<double>(type: "float", nullable: true),
                    SouthPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SouthDisplayName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SouthRating = table.Column<double>(type: "float", nullable: true),
                    NorthPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NorthDisplayName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    NorthRating = table.Column<double>(type: "float", nullable: true),
                    Position = table.Column<string>(type: "varchar(96)", unicode: false, maxLength: 96, nullable: false),
                    Ply = table.Column<int>(type: "int", nullable: false),
                    InitialMs = table.Column<long>(type: "bigint", nullable: false),
                    IncrementMs = table.Column<long>(type: "bigint", nullable: false),
                    SouthClockMs = table.Column<long>(type: "bigint", nullable: false),
                    NorthClockMs = table.Column<long>(type: "bigint", nullable: false),
                    TurnStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FlagFallsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Winner = table.Column<int>(type: "int", nullable: true),
                    EndReason = table.Column<int>(type: "int", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerRatings",
                schema: "matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    RatingDeviation = table.Column<double>(type: "float", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRatings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchMoves",
                schema: "matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ply = table.Column<int>(type: "int", nullable: false),
                    Notation = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClockAfterMs = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchMoves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchMoves_Matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "matches",
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ChallengeCode",
                schema: "matches",
                table: "Matches",
                column: "ChallengeCode",
                unique: true,
                filter: "[ChallengeCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_IsDeleted",
                schema: "matches",
                table: "Matches",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_NorthPlayerId",
                schema: "matches",
                table: "Matches",
                column: "NorthPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_SouthPlayerId",
                schema: "matches",
                table: "Matches",
                column: "SouthPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Status_FinishedAt_RulesVersion",
                schema: "matches",
                table: "Matches",
                columns: new[] { "Status", "FinishedAt", "RulesVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Status_FlagFallsAt",
                schema: "matches",
                table: "Matches",
                columns: new[] { "Status", "FlagFallsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchMoves_MatchId_Ply",
                schema: "matches",
                table: "MatchMoves",
                columns: new[] { "MatchId", "Ply" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchMoves",
                schema: "matches");

            migrationBuilder.DropTable(
                name: "PlayerRatings",
                schema: "matches");

            migrationBuilder.DropTable(
                name: "Matches",
                schema: "matches");
        }
    }
}
