using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FNBReservation.Modules.Authentication.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ManuallyAddedNewUserFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("37d299bb-08dc-4794-bcff-c05c8ab7f49b"));

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "OutletId", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "Role", "UpdatedAt", "UserId", "Username" },
                values: new object[] { new Guid("82f3b68a-5774-4f97-96cc-1bf80071b49e"), new DateTime(2025, 3, 15, 4, 54, 21, 70, DateTimeKind.Utc).AddTicks(3365), "admin@fnbreservation.com", true, null, "AQAAAAEAACcQAAAAEKND4k6EtZZbkwsOVZl8s5WQy59k8/MEP5aqO4vWu2Y5OnUW9DSx9STiUsolFq/llg==", null, null, null, "Admin", new DateTime(2025, 3, 15, 4, 54, 21, 70, DateTimeKind.Utc).AddTicks(3366), "ADMIN001", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("82f3b68a-5774-4f97-96cc-1bf80071b49e"));

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "OutletId", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "Role", "UpdatedAt", "UserId", "Username" },
                values: new object[] { new Guid("37d299bb-08dc-4794-bcff-c05c8ab7f49b"), new DateTime(2025, 3, 15, 4, 31, 40, 890, DateTimeKind.Utc).AddTicks(2692), "admin@fnbreservation.com", true, null, "AQAAAAEAACcQAAAAEKND4k6EtZZbkwsOVZl8s5WQy59k8/MEP5aqO4vWu2Y5OnUW9DSx9STiUsolFq/llg==", null, null, null, "Admin", new DateTime(2025, 3, 15, 4, 31, 40, 890, DateTimeKind.Utc).AddTicks(2693), "ADMIN001", "admin" });
        }
    }
}
