using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualityControl.WPF.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Models",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperationCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResultTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VINs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VINs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Week = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StartImportAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndImportAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Project_Id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Files_Projects_Project_Id",
                        column: x => x.Project_Id,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DataSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    License_Id = table.Column<int>(type: "INTEGER", nullable: false),
                    File_Id = table.Column<int>(type: "INTEGER", nullable: false),
                    VIN_Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Project_Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Model_Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ReportType_Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Category_Id = table.Column<int>(type: "INTEGER", nullable: false),
                    OperationCode_Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ResultType_Id = table.Column<int>(type: "INTEGER", nullable: false),
                    UTC_DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ElapsedTime = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true),
                    BCAVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AppName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    WUVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    AdditionalInfo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AffectedControllers = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSets_Categories_Category_Id",
                        column: x => x.Category_Id,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataSets_Files_File_Id",
                        column: x => x.File_Id,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataSets_Licenses_License_Id",
                        column: x => x.License_Id,
                        principalTable: "Licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataSets_Models_Model_Id",
                        column: x => x.Model_Id,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataSets_OperationCodes_OperationCode_Id",
                        column: x => x.OperationCode_Id,
                        principalTable: "OperationCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataSets_Projects_Project_Id",
                        column: x => x.Project_Id,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataSets_ReportTypes_ReportType_Id",
                        column: x => x.ReportType_Id,
                        principalTable: "ReportTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataSets_ResultTypes_ResultType_Id",
                        column: x => x.ResultType_Id,
                        principalTable: "ResultTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataSets_VINs_VIN_Id",
                        column: x => x.VIN_Id,
                        principalTable: "VINs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_Category_Id",
                table: "DataSets",
                column: "Category_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_File_Id",
                table: "DataSets",
                column: "File_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_License_Id",
                table: "DataSets",
                column: "License_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_Model_Id",
                table: "DataSets",
                column: "Model_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_OperationCode_Id",
                table: "DataSets",
                column: "OperationCode_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_Project_Id",
                table: "DataSets",
                column: "Project_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_ReportType_Id",
                table: "DataSets",
                column: "ReportType_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_ResultType_Id",
                table: "DataSets",
                column: "ResultType_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_VIN_Id",
                table: "DataSets",
                column: "VIN_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Files_Name_Hash",
                table: "Files",
                columns: new[] { "Name", "Hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Files_Project_Id",
                table: "Files",
                column: "Project_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_Name",
                table: "Licenses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Models_Name",
                table: "Models",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationCodes_Name",
                table: "OperationCodes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Name",
                table: "Projects",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportTypes_Name",
                table: "ReportTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultTypes_Name",
                table: "ResultTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VINs_Code",
                table: "VINs",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataSets");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "Licenses");

            migrationBuilder.DropTable(
                name: "Models");

            migrationBuilder.DropTable(
                name: "OperationCodes");

            migrationBuilder.DropTable(
                name: "ReportTypes");

            migrationBuilder.DropTable(
                name: "ResultTypes");

            migrationBuilder.DropTable(
                name: "VINs");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
