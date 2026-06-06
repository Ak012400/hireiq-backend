using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireIQ.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateStatusToScreeningResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "candidate_status",
                table: "screening_results",
                type: "text",
                nullable: false,
                defaultValue: "Screened");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "candidate_status",
                table: "screening_results");
        }
    }
}
