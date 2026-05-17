using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPictureUrlToCandidateProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PictureUrl",
                table: "CandidateProfiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PictureUrl",
                table: "CandidateProfiles");
        }
    }
}
