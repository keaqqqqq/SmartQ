using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FNBReservation.Modules.Authentication.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffFieldsToExistingUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("70d484a5-21b2-4b4b-9870-3c0c4d031317"));

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Users");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "OutletId", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "Role", "UpdatedAt", "UserId", "Username" },
                values: new object[] { new Guid("37d299bb-08dc-4794-bcff-c05c8ab7f49b"), new DateTime(2025, 3, 15, 4, 31, 40, 890, DateTimeKind.Utc).AddTicks(2692), "admin@fnbreservation.com", true, null, "AQAAAAEAACcQAAAAEKND4k6EtZZbkwsOVZl8s5WQy59k8/MEP5aqO4vWu2Y5OnUW9DSx9STiUsolFq/llg==", null, null, null, "Admin", new DateTime(2025, 3, 15, 4, 31, 40, 890, DateTimeKind.Utc).AddTicks(2693), "ADMIN001", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("37d299bb-08dc-4794-bcff-c05c8ab7f49b"));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Users",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Users",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Email", "IsActive", "OutletId", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "Role", "UpdatedAt", "UpdatedBy", "UserId", "Username" },
                values: new object[] { new Guid("70d484a5-21b2-4b4b-9870-3c0c4d031317"), new DateTime(2025, 3, 15, 4, 3, 43, 140, DateTimeKind.Utc).AddTicks(2784), new Guid("00000000-0000-0000-0000-000000000000"), "admin@fnbreservation.com", true, null, "AQAAAAEAACcQAAAAEKND4k6EtZZbkwsOVZl8s5WQy59k8/MEP5aqO4vWu2Y5OnUW9DSx9STiUsolFq/llg==", null, null, null, "Admin", new DateTime(2025, 3, 15, 4, 3, 43, 140, DateTimeKind.Utc).AddTicks(2785), null, "ADMIN001", "admin" });
        }
    }
}
