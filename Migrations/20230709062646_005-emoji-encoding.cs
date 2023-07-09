using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NursingBot.Migrations
{
    public partial class _005emojiencoding : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Emoji",
                table: "roles",
                type: "longblob",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmojiForDebug",
                table: "roles",
                type: "longtext",
                nullable: false,
                comment: "유니코드 코드 페이지 문제인지 string을 사용해서 emoji의 정상적인 쿼리가 불가합니다. 바이트로 인코딩해서 쿼리하되, 디버깅 시 편의성을 위해 문자열은 함께 기록합니다.")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmojiForDebug",
                table: "roles");

            migrationBuilder.AlterColumn<string>(
                name: "Emoji",
                table: "roles",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "longblob")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
