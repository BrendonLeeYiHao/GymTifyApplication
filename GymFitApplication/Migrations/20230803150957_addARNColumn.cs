using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFitApplication.Migrations
{
    public partial class addARNColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookingARNSubscription",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingARNSubscription",
                table: "AspNetUsers");
        }
    }
}
