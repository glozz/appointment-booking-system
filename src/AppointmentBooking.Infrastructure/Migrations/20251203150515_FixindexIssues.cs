using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixindexIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_BranchId_AppointmentDate_StartTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ConsultantId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BranchId_AppointmentDate",
                table: "Appointments",
                columns: new[] { "BranchId", "AppointmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ConsultantId_AppointmentDate_StartTime",
                table: "Appointments",
                columns: new[] { "ConsultantId", "AppointmentDate", "StartTime" },
                unique: true,
                filter: "[ConsultantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_BranchId_AppointmentDate",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ConsultantId_AppointmentDate_StartTime",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BranchId_AppointmentDate_StartTime",
                table: "Appointments",
                columns: new[] { "BranchId", "AppointmentDate", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ConsultantId",
                table: "Appointments",
                column: "ConsultantId");
        }
    }
}
