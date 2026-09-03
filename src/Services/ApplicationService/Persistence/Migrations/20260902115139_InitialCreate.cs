using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApplicationService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cover_letter = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_applications", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_job_applications_candidate_submitted",
                table: "job_applications",
                columns: new[] { "candidate_id", "submitted_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_job_applications_job_status",
                table: "job_applications",
                columns: new[] { "job_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_job_applications_candidate_job",
                table: "job_applications",
                columns: new[] { "candidate_id", "job_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_applications");
        }
    }
}
