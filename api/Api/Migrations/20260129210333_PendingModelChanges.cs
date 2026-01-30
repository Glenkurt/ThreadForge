using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeedbackTags",
                table: "ThreadDrafts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentThreadId",
                table: "ThreadDrafts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "ThreadDrafts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegenerationCount",
                table: "ThreadDrafts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "WasFinalVersion",
                table: "ThreadDrafts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ThreadDrafts_ParentThreadId",
                table: "ThreadDrafts",
                column: "ParentThreadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ThreadDrafts_ParentThreadId",
                table: "ThreadDrafts");

            migrationBuilder.DropColumn(
                name: "FeedbackTags",
                table: "ThreadDrafts");

            migrationBuilder.DropColumn(
                name: "ParentThreadId",
                table: "ThreadDrafts");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "ThreadDrafts");

            migrationBuilder.DropColumn(
                name: "RegenerationCount",
                table: "ThreadDrafts");

            migrationBuilder.DropColumn(
                name: "WasFinalVersion",
                table: "ThreadDrafts");
        }
    }
}
