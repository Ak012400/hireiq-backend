using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireIQ.API.Migrations
{
    /// <inheritdoc />
    public partial class AddConsentRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consent_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    policy_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    granted = table.Column<bool>(type: "boolean", nullable: false),
                    withdrawn = table.Column<bool>(type: "boolean", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_consent_records_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_consent_records_user_id_kind_related_entity_id_recorded_at",
                table: "consent_records",
                columns: new[] { "user_id", "kind", "related_entity_id", "recorded_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "consent_records");
        }
    }
}
