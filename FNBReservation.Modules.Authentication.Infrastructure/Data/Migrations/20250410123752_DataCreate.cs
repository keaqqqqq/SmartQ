using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FNBReservation.Modules.Authentication.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DataCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("0e5c2719-7ea6-4cfa-aa4b-aa95bb438b43"));

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "OutletId", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "Role", "UpdatedAt", "UserId", "Username" },
                values: new object[] { new Guid("a83b8f6d-a399-424a-952f-40e22cda75f0"), new DateTime(2025, 4, 10, 12, 37, 50, 479, DateTimeKind.Utc).AddTicks(4086), "admin@fnbreservation.com", "System Administrator", true, null, "AQAAAAEAACcQAAAAEKND4k6EtZZbkwsOVZl8s5WQy59k8/MEP5aqO4vWu2Y5OnUW9DSx9STiUsolFq/llg==", null, null, null, "Admin", new DateTime(2025, 4, 10, 12, 37, 50, 479, DateTimeKind.Utc).AddTicks(4089), "ADMIN001", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("a83b8f6d-a399-424a-952f-40e22cda75f0"));

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "OutletId", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "Role", "UpdatedAt", "UserId", "Username" },
                values: new object[] { new Guid("0e5c2719-7ea6-4cfa-aa4b-aa95bb438b43"), new DateTime(2025, 3, 31, 8, 36, 19, 116, DateTimeKind.Utc).AddTicks(9209), "admin@fnbreservation.com", "System Administrator", true, null, "AQAAAAEAACcQAAAAEKND4k6EtZZbkwsOVZl8s5WQy59k8/MEP5aqO4vWu2Y5OnUW9DSx9STiUsolFq/llg==", null, null, null, "Admin", new DateTime(2025, 3, 31, 8, 36, 19, 116, DateTimeKind.Utc).AddTicks(9210), "ADMIN001", "admin" });
        }
    }
}
