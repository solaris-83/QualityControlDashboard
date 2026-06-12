using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualityControl.WPF.Migrations
{
    /// <inheritdoc />
    public partial class ErrorMessageColumnToFilesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Files",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Files");
        }
    }
}
