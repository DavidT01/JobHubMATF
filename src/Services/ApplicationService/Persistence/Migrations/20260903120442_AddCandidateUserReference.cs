using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApplicationService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateUserReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "candidate_user_id",
                table: "job_applications",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "candidate_user_id",
                table: "job_applications");
        }
    }
}
