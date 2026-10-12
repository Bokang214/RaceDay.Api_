using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organisers",
                columns: table => new
                {
                    OrganiserId = table.Column<int>(type: "int", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisers", x => x.OrganiserId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Participant"),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Distance = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrganiserId = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_Events_Organisers_OrganiserId",
                        column: x => x.OrganiserId,
                        principalTable: "Organisers",
                        principalColumn: "OrganiserId",
                        onDelete: ReferentialAction.Restrict);
                    table.CheckConstraint("CHK_Events_Distance", "[Distance] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                    table.ForeignKey(
                        name: "FK_Categories_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    EnrolmentId = table.Column<int>(type: "int", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    EnrolmentDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.EnrolmentId);
                    table.ForeignKey(
                        name: "FK_Enrolments_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrolments_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrolments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentId = table.Column<int>(type: "int", nullable: false),
                    FinishTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultId);
                    table.ForeignKey(
                        name: "FK_Results_Enrolments_EnrolmentId",
                        column: x => x.EnrolmentId,
                        principalTable: "Enrolments",
                        principalColumn: "EnrolmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.CheckConstraint("CHK_Results_Position", "[Position] IS NULL OR [Position] > 0");
                });

            migrationBuilder.CreateIndex(name: "IX_Organisers_Email", table: "Organisers", column: "Email", unique: true);

            migrationBuilder.CreateIndex(name: "IX_Users_Email", table: "Users", column: "Email", unique: true);

            migrationBuilder.CreateIndex(name: "IX_Events_OrganiserId", table: "Events", column: "OrganiserId");

            migrationBuilder.CreateIndex(name: "IX_Categories_EventId", table: "Categories", column: "EventId");

            migrationBuilder.CreateIndex(name: "IX_Enrolments_CategoryId", table: "Enrolments", column: "CategoryId");

            migrationBuilder.CreateIndex(name: "IX_Enrolments_EventId", table: "Enrolments", column: "EventId");

            migrationBuilder.CreateIndex(name: "IX_Enrolments_UserId", table: "Enrolments", column: "UserId");

            migrationBuilder.CreateIndex(name: "IX_Results_EnrolmentId", table: "Results", column: "EnrolmentId", unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Results");
            migrationBuilder.DropTable(name: "Enrolments");
            migrationBuilder.DropTable(name: "Categories");
            migrationBuilder.DropTable(name: "Events");
            migrationBuilder.DropTable(name: "Users");
            migrationBuilder.DropTable(name: "Organisers");
        }
    }
}
