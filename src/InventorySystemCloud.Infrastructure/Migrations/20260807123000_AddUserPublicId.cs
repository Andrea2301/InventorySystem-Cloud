using System;
using InventorySystemCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventorySystemCloud.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260807123000_AddUserPublicId")]
    public partial class AddUserPublicId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "PublicId", table: "users", type: "char(36)", nullable: true);
            migrationBuilder.Sql("UPDATE `users` SET `PublicId` = UUID() WHERE `PublicId` IS NULL;");
            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "users", type: "char(36)", nullable: false, oldClrType: typeof(Guid), oldType: "char(36)", oldNullable: true);
            migrationBuilder.CreateIndex(name: "IX_users_PublicId", table: "users", column: "PublicId", unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_users_PublicId", table: "users");
            migrationBuilder.DropColumn(name: "PublicId", table: "users");
        }
    }
}
