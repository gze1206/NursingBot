using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NursingBot.Migrations
{
    public partial class _003serversremoveprefix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "servers");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "servers",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
