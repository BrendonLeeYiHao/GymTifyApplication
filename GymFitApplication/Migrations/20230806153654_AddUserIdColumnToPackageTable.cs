using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFitApplication.Migrations
{
    public partial class AddUserIdColumnToPackageTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PackageTable",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PackageTable");
        }
    }
}
