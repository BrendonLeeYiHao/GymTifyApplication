using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFitApplication.Migrations
{
    public partial class AddPackageColumnToAspNetUsersTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Packageid",
                table: "AspNetUsers",
                type: "int",
                nullable: true);
        }

            

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Packageid",
                table: "AspNetUsers");
        }
    }
}
