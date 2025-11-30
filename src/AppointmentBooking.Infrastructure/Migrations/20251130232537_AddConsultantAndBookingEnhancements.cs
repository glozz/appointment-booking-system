using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultantAndBookingEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_BranchId",
                table: "Appointments");

            migrationBuilder.AddColumn<int>(
                name: "ConsultantId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Consultants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consultants_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BranchId_AppointmentDate_StartTime",
                table: "Appointments",
                columns: new[] { "BranchId", "AppointmentDate", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ConsultantId",
                table: "Appointments",
                column: "ConsultantId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultants_BranchId",
                table: "Consultants",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Consultants_ConsultantId",
                table: "Appointments",
                column: "ConsultantId",
                principalTable: "Consultants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Consultants_ConsultantId",
                table: "Appointments");

            migrationBuilder.DropTable(
                name: "Consultants");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_BranchId_AppointmentDate_StartTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ConsultantId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ConsultantId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BranchId",
                table: "Appointments",
                column: "BranchId");
        }
    }
}
