using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recruitment.API.Migrations
{
    /// <inheritdoc />
    public partial class UseCatalogJobIdStrings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Processes");

            migrationBuilder.AddColumn<string>(
                name: "JobId",
                table: "Processes",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Processes_JobId",
                table: "Processes",
                column: "JobId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Processes_JobId",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Processes");

            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "Processes",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);
        }
    }
}
