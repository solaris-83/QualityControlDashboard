using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualityControl.WPF.Migrations
{
    /// <inheritdoc />
    public partial class NumberOfRecordsToFilesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumberOfRecords",
                table: "Files",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfRecords",
                table: "Files");
        }
    }
}
