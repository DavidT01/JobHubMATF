using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.API.Migrations
{
    /// <inheritdoc />
    public partial class ForceUniqueUserIdOnCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_UserId",
                table: "CompanyProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_UserId",
                table: "CompanyProfiles");
        }
    }
}
