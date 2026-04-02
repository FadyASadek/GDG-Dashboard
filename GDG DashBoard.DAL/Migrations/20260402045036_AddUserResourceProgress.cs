using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GDG_DashBoard.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserResourceProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserResourceProgresses_AspNetUsers_UserId",
                table: "UserResourceProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserResourceProgresses_Resources_ResourceId",
                table: "UserResourceProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserResourceProgresses_ResourceId",
                table: "UserResourceProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserResourceProgresses_UserId",
                table: "UserResourceProgresses");

            migrationBuilder.CreateIndex(
                name: "IX_UserResourceProgresses_ResourceId",
                table: "UserResourceProgresses",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserResourceProgresses_UserId_ResourceId",
                table: "UserResourceProgresses",
                columns: new[] { "UserId", "ResourceId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserResourceProgresses_AspNetUsers_UserId",
                table: "UserResourceProgresses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserResourceProgresses_Resources_ResourceId",
                table: "UserResourceProgresses",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserResourceProgresses_AspNetUsers_UserId",
                table: "UserResourceProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserResourceProgresses_Resources_ResourceId",
                table: "UserResourceProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserResourceProgresses_ResourceId",
                table: "UserResourceProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserResourceProgresses_UserId_ResourceId",
                table: "UserResourceProgresses");

            migrationBuilder.CreateIndex(
                name: "IX_UserResourceProgresses_ResourceId",
                table: "UserResourceProgresses",
                column: "ResourceId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserResourceProgresses_UserId",
                table: "UserResourceProgresses",
                column: "UserId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_UserResourceProgresses_AspNetUsers_UserId",
                table: "UserResourceProgresses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserResourceProgresses_Resources_ResourceId",
                table: "UserResourceProgresses",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
