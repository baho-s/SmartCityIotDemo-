using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCityIotDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceCode = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceCode = table.Column<string>(type: "TEXT", nullable: false),
                    Temperature = table.Column<decimal>(type: "TEXT", nullable: false),
                    Humidity = table.Column<decimal>(type: "TEXT", nullable: false),
                    BatteryLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    SignalStrength = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelemetryData_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceCode",
                table: "Devices",
                column: "DeviceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryData_CreatedAt",
                table: "TelemetryData",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryData_DeviceCode",
                table: "TelemetryData",
                column: "DeviceCode");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryData_DeviceId",
                table: "TelemetryData",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelemetryData");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
