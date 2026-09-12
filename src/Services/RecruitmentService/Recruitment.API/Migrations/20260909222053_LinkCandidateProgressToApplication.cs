using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recruitment.API.Migrations
{
    /// <inheritdoc />
    public partial class LinkCandidateProgressToApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                table: "Progresses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Progresses_ApplicationId",
                table: "Progresses",
                column: "ApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Progresses_ApplicationId",
                table: "Progresses");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "Progresses");
        }
    }
}
