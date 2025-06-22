using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DavideBotHelper.Migrations
{
    /// <inheritdoc />
    public partial class AddSizeAndRegexs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "size",
                table: "rc_repository_release",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "rc_github_repository",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "tag_regex",
                table: "rc_github_repository",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "version_regex",
                table: "rc_github_repository",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "size",
                table: "rc_repository_release");

            migrationBuilder.DropColumn(
                name: "tag_regex",
                table: "rc_github_repository");

            migrationBuilder.DropColumn(
                name: "version_regex",
                table: "rc_github_repository");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "rc_github_repository",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);
        }
    }
}
