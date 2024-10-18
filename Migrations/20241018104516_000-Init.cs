using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NursingBot.Migrations
{
    public partial class _000Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "servers",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DiscordUID = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "partyChannels",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ServerId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partyChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_partyChannels_servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roleManagers",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ServerId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    MessageId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roleManagers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roleManagers_servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "votes",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ServerId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    AuthorId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    MessageId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Choices = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsClosed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_votes_servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wardConfigs",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ServerId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CategoryId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    HospitalizationId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wardConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wardConfigs_servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "partyRecruits",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PartyChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    MessageId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    AuthorId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsClosed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partyRecruits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_partyRecruits_partyChannels_PartyChannelId",
                        column: x => x.PartyChannelId,
                        principalTable: "partyChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoleManagerId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    DiscordRoleId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Emoji = table.Column<byte[]>(type: "longblob", nullable: false),
                    EmojiForDebug = table.Column<string>(type: "longtext", nullable: false, comment: "유니코드 코드 페이지 문제인지 string을 사용해서 emoji의 정상적인 쿼리가 불가합니다. 바이트로 인코딩해서 쿼리하되, 디버깅 시 편의성을 위해 문자열은 함께 기록합니다.")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roles_roleManagers_RoleManagerId",
                        column: x => x.RoleManagerId,
                        principalTable: "roleManagers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_partyChannels_ServerId",
                table: "partyChannels",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_partyRecruits_PartyChannelId",
                table: "partyRecruits",
                column: "PartyChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_roleManagers_ServerId",
                table: "roleManagers",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_roles_RoleManagerId",
                table: "roles",
                column: "RoleManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_servers_DiscordUID",
                table: "servers",
                column: "DiscordUID");

            migrationBuilder.CreateIndex(
                name: "IX_votes_ServerId",
                table: "votes",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_wardConfigs_ServerId",
                table: "wardConfigs",
                column: "ServerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partyRecruits");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "votes");

            migrationBuilder.DropTable(
                name: "wardConfigs");

            migrationBuilder.DropTable(
                name: "partyChannels");

            migrationBuilder.DropTable(
                name: "roleManagers");

            migrationBuilder.DropTable(
                name: "servers");
        }
    }
}
