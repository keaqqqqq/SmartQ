using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FNBReservation.Modules.Queue.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QueueEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    QueueCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OutletId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomerName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerPhone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PartySize = table.Column<int>(type: "int", nullable: false),
                    SpecialRequests = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QueuePosition = table.Column<int>(type: "int", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CalledAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SeatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsHeld = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    HeldSince = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EstimatedWaitMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueEntries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QueueNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    QueueEntryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    NotificationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Channel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueNotifications_QueueEntries_QueueEntryId",
                        column: x => x.QueueEntryId,
                        principalTable: "QueueEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QueueStatusChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    QueueEntryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OldStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ChangedById = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueStatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueStatusChanges_QueueEntries_QueueEntryId",
                        column: x => x.QueueEntryId,
                        principalTable: "QueueEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QueueTableAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    QueueEntryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TableId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TableNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    SeatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AssignedBy = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueTableAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueTableAssignments_QueueEntries_QueueEntryId",
                        column: x => x.QueueEntryId,
                        principalTable: "QueueEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_CustomerPhone",
                table: "QueueEntries",
                column: "CustomerPhone");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_OutletId",
                table: "QueueEntries",
                column: "OutletId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_OutletId_Status",
                table: "QueueEntries",
                columns: new[] { "OutletId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_QueueCode",
                table: "QueueEntries",
                column: "QueueCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_QueuePosition",
                table: "QueueEntries",
                column: "QueuePosition");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_Status",
                table: "QueueEntries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QueueNotifications_QueueEntryId",
                table: "QueueNotifications",
                column: "QueueEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueNotifications_Status",
                table: "QueueNotifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QueueStatusChanges_ChangedAt",
                table: "QueueStatusChanges",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QueueStatusChanges_QueueEntryId",
                table: "QueueStatusChanges",
                column: "QueueEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueTableAssignments_QueueEntryId",
                table: "QueueTableAssignments",
                column: "QueueEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueTableAssignments_Status",
                table: "QueueTableAssignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QueueTableAssignments_TableId",
                table: "QueueTableAssignments",
                column: "TableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueueNotifications");

            migrationBuilder.DropTable(
                name: "QueueStatusChanges");

            migrationBuilder.DropTable(
                name: "QueueTableAssignments");

            migrationBuilder.DropTable(
                name: "QueueEntries");
        }
    }
}
