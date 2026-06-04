using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trail.Api.Migrations
{
    /// <inheritdoc />
    public partial class LeanSubmissionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Submissions");

            migrationBuilder.RenameColumn(
                name: "DeliveryUrl",
                table: "Submissions",
                newName: "GitHubUrl");

            migrationBuilder.AddColumn<string>(
                name: "MentorComment",
                table: "Submissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YouTubeUrl",
                table: "Challenges",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            // Migrate existing "Reviewed" status to "Approved"
            // (old enum had no distinction; all historic reviews are treated as approved)
            migrationBuilder.Sql(
                "UPDATE Submissions SET Status = 'Approved' WHERE Status = 'Reviewed'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MentorComment",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "YouTubeUrl",
                table: "Challenges");

            migrationBuilder.RenameColumn(
                name: "GitHubUrl",
                table: "Submissions",
                newName: "DeliveryUrl");

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "Submissions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Submissions",
                type: "int",
                nullable: true);
        }
    }
}
