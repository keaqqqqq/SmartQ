using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FNBReservation.Modules.Outlet.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRamadan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PeakHourSettings_OutletId_IsRamadanSetting",
                table: "PeakHourSettings");

            migrationBuilder.DropColumn(
                name: "IsRamadanSetting",
                table: "PeakHourSettings");

            migrationBuilder.DropColumn(
                name: "RamadanEndDate",
                table: "PeakHourSettings");

            migrationBuilder.DropColumn(
                name: "RamadanStartDate",
                table: "PeakHourSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRamadanSetting",
                table: "PeakHourSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RamadanEndDate",
                table: "PeakHourSettings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RamadanStartDate",
                table: "PeakHourSettings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeakHourSettings_OutletId_IsRamadanSetting",
                table: "PeakHourSettings",
                columns: new[] { "OutletId", "IsRamadanSetting" });
        }
    }
}
