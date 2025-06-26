using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DavideBotHelper.Migrations
{
    /// <inheritdoc />
    public partial class AggiunteColonne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "require_download",
                table: "rc_repository_release",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "to_send",
                table: "rc_repository_release",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "require_download",
                table: "rc_repository_release");

            migrationBuilder.DropColumn(
                name: "to_send",
                table: "rc_repository_release");
        }
    }
}
