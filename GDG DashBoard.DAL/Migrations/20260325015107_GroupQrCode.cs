using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GDG_DashBoard.DAL.Migrations
{
    /// <inheritdoc />
    public partial class GroupQrCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JoinCode",
                table: "CommunityGroups",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JoinCode",
                table: "CommunityGroups");
        }
    }
}
