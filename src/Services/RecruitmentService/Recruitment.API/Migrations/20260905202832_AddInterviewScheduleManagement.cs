using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recruitment.API.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewScheduleManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "AdditionalAttendeeEmails",
                table: "InterviewSchedules",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "InterviewSchedules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "InterviewSchedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "InterviewSchedules",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalAttendeeEmails",
                table: "InterviewSchedules");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "InterviewSchedules");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "InterviewSchedules");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "InterviewSchedules");
        }
    }
}
