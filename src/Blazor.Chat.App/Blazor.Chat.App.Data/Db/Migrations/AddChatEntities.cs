using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blazor.Chat.App.Data.Db.Migrations;

/// <inheritdoc />
public partial class AddChatEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserClaims_ApplicationUser_UserId",
            table: "AspNetUserClaims");

        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserLogins_ApplicationUser_UserId",
            table: "AspNetUserLogins");

        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserRoles_ApplicationUser_UserId",
            table: "AspNetUserRoles");

        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserTokens_ApplicationUser_UserId",
            table: "AspNetUserTokens");

        migrationBuilder.DropTable(
            name: "ApplicationUser");

        migrationBuilder.CreateTable(
            name: "ChatOutbox",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MessageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Attempts = table.Column<int>(type: "int", nullable: false),
                LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChatOutbox", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ChatSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                IsGroup = table.Column<bool>(type: "bit", nullable: false),
                CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                State = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChatSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ChatSessions_AspNetUsers_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ChatMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SenderUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Preview = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                MessageLength = table.Column<int>(type: "int", nullable: false),
                MessageStatus = table.Column<int>(type: "int", nullable: false),
                OutboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReplyToMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Version = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChatMessages", x => x.Id);
                table.ForeignKey(
                    name: "FK_ChatMessages_AspNetUsers_SenderUserId",
                    column: x => x.SenderUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChatMessages_ChatMessages_ReplyToMessageId",
                    column: x => x.ReplyToMessageId,
                    principalTable: "ChatMessages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChatMessages_ChatOutbox_OutboxId",
                    column: x => x.OutboxId,
                    principalTable: "ChatOutbox",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChatMessages_ChatSessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "ChatSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ChatParticipants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsMuted = table.Column<bool>(type: "bit", nullable: false),
                Role = table.Column<int>(type: "int", nullable: false),
                LastReadMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChatParticipants", x => x.Id);
                table.ForeignKey(
                    name: "FK_ChatParticipants_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChatParticipants_ChatMessages_LastReadMessageId",
                    column: x => x.LastReadMessageId,
                    principalTable: "ChatMessages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_ChatParticipants_ChatSessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "ChatSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ChatMessages_OutboxId",
            table: "ChatMessages",
            column: "OutboxId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ChatMessages_ReplyToMessageId",
            table: "ChatMessages",
            column: "ReplyToMessageId");

        migrationBuilder.CreateIndex(
            name: "IX_ChatMessages_SenderUserId",
            table: "ChatMessages",
            column: "SenderUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ChatMessages_SessionId_SentAt",
            table: "ChatMessages",
            columns: new[] { "SessionId", "SentAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ChatOutbox_CreatedAt",
            table: "ChatOutbox",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_ChatOutbox_MessageId",
            table: "ChatOutbox",
            column: "MessageId");

        migrationBuilder.CreateIndex(
            name: "IX_ChatOutbox_SessionId",
            table: "ChatOutbox",
            column: "SessionId");

        migrationBuilder.CreateIndex(
            name: "IX_ChatOutbox_Status_NextRetryAt",
            table: "ChatOutbox",
            columns: new[] { "Status", "NextRetryAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ChatParticipants_LastReadAt",
            table: "ChatParticipants",
            column: "LastReadAt");

        migrationBuilder.CreateIndex(
            name: "IX_ChatParticipants_LastReadMessageId",
            table: "ChatParticipants",
            column: "LastReadMessageId");

        migrationBuilder.CreateIndex(
            name: "IX_ChatParticipants_SessionId_UserId",
            table: "ChatParticipants",
            columns: new[] { "SessionId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ChatParticipants_UserId",
            table: "ChatParticipants",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_ChatSessions_CreatedByUserId",
            table: "ChatSessions",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ChatSessions_LastActivityAt",
            table: "ChatSessions",
            column: "LastActivityAt");

        migrationBuilder.CreateIndex(
            name: "IX_ChatSessions_TenantId_State",
            table: "ChatSessions",
            columns: new[] { "TenantId", "State" });

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserClaims_AspNetUsers_UserId",
            table: "AspNetUserClaims",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserLogins_AspNetUsers_UserId",
            table: "AspNetUserLogins",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserRoles_AspNetUsers_UserId",
            table: "AspNetUserRoles",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserTokens_AspNetUsers_UserId",
            table: "AspNetUserTokens",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserClaims_AspNetUsers_UserId",
            table: "AspNetUserClaims");

        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserLogins_AspNetUsers_UserId",
            table: "AspNetUserLogins");

        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserRoles_AspNetUsers_UserId",
            table: "AspNetUserRoles");

        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserTokens_AspNetUsers_UserId",
            table: "AspNetUserTokens");

        migrationBuilder.DropTable(
            name: "ChatParticipants");

        migrationBuilder.DropTable(
            name: "ChatMessages");

        migrationBuilder.DropTable(
            name: "ChatOutbox");

        migrationBuilder.DropTable(
            name: "ChatSessions");

        migrationBuilder.CreateTable(
            name: "ApplicationUser",
            columns: table => new
            {
                TempId1 = table.Column<string>(type: "nvarchar(450)", nullable: false),
                TempId2 = table.Column<string>(type: "nvarchar(450)", nullable: false),
                TempId3 = table.Column<string>(type: "nvarchar(450)", nullable: false),
                TempId4 = table.Column<string>(type: "nvarchar(450)", nullable: false)
            },
            constraints: table =>
            {
                table.UniqueConstraint("AK_ApplicationUser_TempId1", x => x.TempId1);
                table.UniqueConstraint("AK_ApplicationUser_TempId2", x => x.TempId2);
                table.UniqueConstraint("AK_ApplicationUser_TempId3", x => x.TempId3);
                table.UniqueConstraint("AK_ApplicationUser_TempId4", x => x.TempId4);
            });

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserClaims_ApplicationUser_UserId",
            table: "AspNetUserClaims",
            column: "UserId",
            principalTable: "ApplicationUser",
            principalColumn: "TempId1",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserLogins_ApplicationUser_UserId",
            table: "AspNetUserLogins",
            column: "UserId",
            principalTable: "ApplicationUser",
            principalColumn: "TempId2",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserRoles_ApplicationUser_UserId",
            table: "AspNetUserRoles",
            column: "UserId",
            principalTable: "ApplicationUser",
            principalColumn: "TempId3",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserTokens_ApplicationUser_UserId",
            table: "AspNetUserTokens",
            column: "UserId",
            principalTable: "ApplicationUser",
            principalColumn: "TempId4",
            onDelete: ReferentialAction.Cascade);
    }
}