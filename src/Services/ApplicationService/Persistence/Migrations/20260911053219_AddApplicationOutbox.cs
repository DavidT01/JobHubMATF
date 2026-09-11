using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ApplicationService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "application_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_outbox", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_application_outbox_application",
                table: "application_outbox",
                columns: new[] { "application_id", "sequence" });

            migrationBuilder.CreateIndex(
                name: "ix_application_outbox_pending",
                table: "application_outbox",
                columns: new[] { "next_attempt_at_utc", "sequence" },
                filter: "published_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_application_outbox_sequence",
                table: "application_outbox",
                column: "sequence",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_outbox");
        }
    }
}
