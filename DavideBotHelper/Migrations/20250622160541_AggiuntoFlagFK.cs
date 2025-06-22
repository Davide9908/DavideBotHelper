using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DavideBotHelper.Migrations
{
    /// <inheritdoc />
    public partial class AggiuntoFlagFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "download_url",
                table: "rc_repository_release",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "repository_id",
                table: "rc_repository_release",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "flag_reset_release_cache",
                table: "rc_github_repository",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_rc_repository_release_repository_id",
                table: "rc_repository_release",
                column: "repository_id");

            migrationBuilder.AddForeignKey(
                name: "FK_rc_repository_release_rc_github_repository_repository_id",
                table: "rc_repository_release",
                column: "repository_id",
                principalTable: "rc_github_repository",
                principalColumn: "repo_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rc_repository_release_rc_github_repository_repository_id",
                table: "rc_repository_release");

            migrationBuilder.DropIndex(
                name: "IX_rc_repository_release_repository_id",
                table: "rc_repository_release");

            migrationBuilder.DropColumn(
                name: "repository_id",
                table: "rc_repository_release");

            migrationBuilder.DropColumn(
                name: "flag_reset_release_cache",
                table: "rc_github_repository");

            migrationBuilder.AlterColumn<string>(
                name: "download_url",
                table: "rc_repository_release",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
