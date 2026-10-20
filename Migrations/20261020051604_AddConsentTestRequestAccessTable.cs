using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NMB_HLabSys.Migrations
{
    /// <inheritdoc />
    public partial class AddConsentTestRequestAccessTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentTestRequest");

            migrationBuilder.AlterColumn<decimal>(
                name: "NumericValue",
                table: "TestResults",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "PatientConsentManagements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "NumericValue",
                table: "TestResults",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "PatientConsentManagements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ConsentTestRequest",
                columns: table => new
                {
                    ConsentTestRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsentID = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TestRequestID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentTestRequest", x => x.ConsentTestRequestID);
                    table.ForeignKey(
                        name: "FK_ConsentTestRequest_PatientConsentManagements_ConsentID",
                        column: x => x.ConsentID,
                        principalTable: "PatientConsentManagements",
                        principalColumn: "ConsentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentTestRequest_ConsentID",
                table: "ConsentTestRequest",
                column: "ConsentID");
        }
    }
}
