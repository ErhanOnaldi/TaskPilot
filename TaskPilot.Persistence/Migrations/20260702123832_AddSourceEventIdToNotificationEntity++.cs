using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPilot.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSourceEventIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_SourceEventId",
                table: "Notifications",
                columns: new[] { "UserId", "SourceEventId" },
                unique: true,
                filter: "\"SourceEventId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_SourceEventId",
                table: "Notifications");
        }
    }
}
