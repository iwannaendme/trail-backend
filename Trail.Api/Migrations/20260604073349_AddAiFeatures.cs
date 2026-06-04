using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trail.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiSearchTerms",
                table: "Challenges",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OnboardingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TechnicalDepth = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    WeeklyHours = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    LearningStyle = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProjectGoal = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingProfiles_UserId",
                table: "OnboardingProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnboardingProfiles");

            migrationBuilder.DropColumn(
                name: "AiSearchTerms",
                table: "Challenges");
        }
    }
}
