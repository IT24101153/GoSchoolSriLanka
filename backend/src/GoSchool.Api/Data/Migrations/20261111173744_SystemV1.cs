using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GoSchool.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SystemV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ApplicationId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SchoolsCovered",
                table: "Routes");

            migrationBuilder.AddColumn<string>(
                name: "ApiToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeAddress",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeLatitude",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeLongitude",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolAddress",
                table: "Students",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "SchoolLatitude",
                table: "Students",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SchoolLongitude",
                table: "Students",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "RouteStopId",
                table: "Applications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DriverSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    BillingYear = table.Column<int>(type: "integer", nullable: false),
                    BillingMonth = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverSubscriptions_Users_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteStops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RouteId = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    School = table.Column<string>(type: "text", nullable: false),
                    PickupAddress = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    ArrivalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteStops_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ApiToken",
                table: "Users",
                column: "ApiToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ApplicationId_BillingYear_BillingMonth",
                table: "Payments",
                columns: new[] { "ApplicationId", "BillingYear", "BillingMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_RouteStopId",
                table: "Applications",
                column: "RouteStopId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverSubscriptions_DriverId_BillingYear_BillingMonth",
                table: "DriverSubscriptions",
                columns: new[] { "DriverId", "BillingYear", "BillingMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteId",
                table: "RouteStops",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_RouteStops_RouteStopId",
                table: "Applications",
                column: "RouteStopId",
                principalTable: "RouteStops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_RouteStops_RouteStopId",
                table: "Applications");

            migrationBuilder.DropTable(
                name: "DriverSubscriptions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "RouteStops");

            migrationBuilder.DropIndex(
                name: "IX_Users_ApiToken",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ApplicationId_BillingYear_BillingMonth",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Applications_RouteStopId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ApiToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HomeAddress",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HomeLatitude",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HomeLongitude",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SchoolAddress",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SchoolLatitude",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SchoolLongitude",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RouteStopId",
                table: "Applications");

            migrationBuilder.AddColumn<List<string>>(
                name: "SchoolsCovered",
                table: "Routes",
                type: "text[]",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ApplicationId",
                table: "Payments",
                column: "ApplicationId");
        }
    }
}
