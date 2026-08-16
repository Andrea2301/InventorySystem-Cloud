using System;
using InventorySystemCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventorySystemCloud.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260807120000_AddAuthenticationSecurityFields")]
    public partial class AddAuthenticationSecurityFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaptchaToken",
                table: "users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "users",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CaptchaToken", table: "users");
            migrationBuilder.DropColumn(name: "FailedLoginAttempts", table: "users");
            migrationBuilder.DropColumn(name: "LockoutEnd", table: "users");
            migrationBuilder.DropColumn(name: "SecurityStamp", table: "users");
        }
    }
}
