using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhistleblowerNews.Migrations
{
    /// <inheritdoc />
    public partial class AddWhistleblower : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.CaseId);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogEntries_Reports_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Reports",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditLogEntries_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvestigatorAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvestigatorUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestigatorAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestigatorAssignments_Reports_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Reports",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvestigatorAssignments_Users_InvestigatorUserId",
                        column: x => x.InvestigatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReporterSecrets",
                columns: table => new
                {
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SecretHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReporterSecrets", x => x.CaseId);
                    table.ForeignKey(
                        name: "FK_ReporterSecrets_Reports_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Reports",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SenderType = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportMessages_Reports_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Reports",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ActorUserId",
                table: "AuditLogEntries",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_CaseId",
                table: "AuditLogEntries",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestigatorAssignments_CaseId",
                table: "InvestigatorAssignments",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestigatorAssignments_InvestigatorUserId",
                table: "InvestigatorAssignments",
                column: "InvestigatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMessages_CaseId",
                table: "ReportMessages",
                column: "CaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogEntries");

            migrationBuilder.DropTable(
                name: "InvestigatorAssignments");

            migrationBuilder.DropTable(
                name: "ReporterSecrets");

            migrationBuilder.DropTable(
                name: "ReportMessages");

            migrationBuilder.DropTable(
                name: "Reports");
        }
    }
}

