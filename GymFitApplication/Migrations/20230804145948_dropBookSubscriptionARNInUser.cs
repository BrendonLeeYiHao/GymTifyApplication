using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFitApplication.Migrations
{
    public partial class dropBookSubscriptionARNInUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookARNSubscription",
                table: "AspNetUsers");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookARNSubscription",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
