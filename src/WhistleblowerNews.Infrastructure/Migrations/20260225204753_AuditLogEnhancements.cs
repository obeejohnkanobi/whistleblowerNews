using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhistleblowerNews.Migrations
{
    /// <inheritdoc />
    public partial class AuditLogEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Action",
                table: "AuditLogEntries",
                newName: "Outcome");

            migrationBuilder.AlterColumn<Guid>(
                name: "CaseId",
                table: "AuditLogEntries",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "ActorUserId",
                table: "AuditLogEntries",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "ActorRole",
                table: "AuditLogEntries",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "AuditLogEntries",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "AuditLogEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AuditLogEntries",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetId",
                table: "AuditLogEntries",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetType",
                table: "AuditLogEntries",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AuditLogEntries",
                type: "TEXT",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorRole",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "TargetType",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AuditLogEntries");

            migrationBuilder.RenameColumn(
                name: "Outcome",
                table: "AuditLogEntries",
                newName: "Action");

            migrationBuilder.AlterColumn<Guid>(
                name: "CaseId",
                table: "AuditLogEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ActorUserId",
                table: "AuditLogEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
