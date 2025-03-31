using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FNBReservation.Modules.Authentication.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffAdminFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("82f3b68a-5774-4f97-96cc-1bf80071b49e"));

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "OutletId", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "Role", "UpdatedAt", "UserId", "Username" },
                values: new object[] { new Guid("45c525ab-00e7-4738-a56e-4a817ec8239a"), new DateTime(2025, 3, 31, 8, 31, 14, 168, DateTimeKind.Utc).AddTicks(230), "admin@fnbreservation.com", "System Administrator", true, null, "AQAAAAEAACcQAAAAEKND4k6EtZZbkwsOVZl8s5WQy59k8/MEP5aqO4vWu2Y5OnUW9DSx9STiUsolFq/llg==", null, null, null, "Admin", new DateTime(2025, 3, 31, 8, 31, 14, 168, DateTimeKind.Utc).AddTicks(233), "ADMIN001", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("45c525ab-00e7-4738-a56e-4a817ec8239a"));

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "OutletId", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "Role", "UpdatedAt", "UserId", "Username" },
                values: new object[] { new Guid("82f3b68a-5774-4f97-96cc-1bf80071b49e"), new DateTime(2025, 3, 15, 4, 54, 21, 70, DateTimeKind.Utc).AddTicks(3365), "admin@fnbreservation.com", true, null, "AQAAAAEAACcQAAAAEKND4k6EtZZbkwsOVZl8s5WQy59k8/MEP5aqO4vWu2Y5OnUW9DSx9STiUsolFq/llg==", null, null, null, "Admin", new DateTime(2025, 3, 15, 4, 54, 21, 70, DateTimeKind.Utc).AddTicks(3366), "ADMIN001", "admin" });
        }
    }
}
