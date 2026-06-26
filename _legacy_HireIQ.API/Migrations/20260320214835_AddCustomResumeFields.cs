using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireIQ.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomResumeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "custom_resume_fields",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_name = table.Column<string>(type: "text", nullable: false),
                    field_value = table.Column<string>(type: "text", nullable: false),
                    field_type = table.Column<string>(type: "text", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_resume_fields", x => x.id);
                    table.ForeignKey(
                        name: "FK_custom_resume_fields_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_custom_resume_fields_user_id",
                table: "custom_resume_fields",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "custom_resume_fields");
        }
    }
}