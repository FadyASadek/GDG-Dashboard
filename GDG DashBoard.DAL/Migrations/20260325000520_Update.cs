using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GDG_DashBoard.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserEnrollments_UserId",
                table: "UserEnrollments");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedByUserId",
                table: "Roadmaps",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_UserEnrollments_UserId_RoadmapId",
                table: "UserEnrollments",
                columns: new[] { "UserId", "RoadmapId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roadmaps_CreatedByUserId",
                table: "Roadmaps",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Roadmaps_AspNetUsers_CreatedByUserId",
                table: "Roadmaps",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roadmaps_AspNetUsers_CreatedByUserId",
                table: "Roadmaps");

            migrationBuilder.DropIndex(
                name: "IX_UserEnrollments_UserId_RoadmapId",
                table: "UserEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_Roadmaps_CreatedByUserId",
                table: "Roadmaps");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "Roadmaps",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_UserEnrollments_UserId",
                table: "UserEnrollments",
                column: "UserId");
        }
    }
}
