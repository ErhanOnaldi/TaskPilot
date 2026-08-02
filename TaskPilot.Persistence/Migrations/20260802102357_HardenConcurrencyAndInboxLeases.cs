using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPilot.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenConcurrencyAndInboxLeases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyReports_SourceEventId",
                table: "WeeklyReports");

            migrationBuilder.DropIndex(
                name: "IX_WeeklyReportCheckpoints_ProjectId",
                table: "WeeklyReportCheckpoints");

            migrationBuilder.DropIndex(
                name: "IX_WeeklyReportCheckpoints_SourceEventId_Stage",
                table: "WeeklyReportCheckpoints");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "WeeklyReports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "LockId",
                table: "InboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "InboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReports_ProjectId_SourceEventId",
                table: "WeeklyReports",
                columns: new[] { "ProjectId", "SourceEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReportCheckpoints_ProjectId_SourceEventId_Stage",
                table: "WeeklyReportCheckpoints",
                columns: new[] { "ProjectId", "SourceEventId", "Stage" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_LockedUntilUtc",
                table: "InboxMessages",
                column: "LockedUntilUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyReports_ProjectId_SourceEventId",
                table: "WeeklyReports");

            migrationBuilder.DropIndex(
                name: "IX_WeeklyReportCheckpoints_ProjectId_SourceEventId_Stage",
                table: "WeeklyReportCheckpoints");

            migrationBuilder.DropIndex(
                name: "IX_InboxMessages_LockedUntilUtc",
                table: "InboxMessages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "WeeklyReports");

            migrationBuilder.DropColumn(
                name: "LockId",
                table: "InboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "InboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReports_SourceEventId",
                table: "WeeklyReports",
                column: "SourceEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReportCheckpoints_ProjectId",
                table: "WeeklyReportCheckpoints",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReportCheckpoints_SourceEventId_Stage",
                table: "WeeklyReportCheckpoints",
                columns: new[] { "SourceEventId", "Stage" },
                unique: true);
        }
    }
}
