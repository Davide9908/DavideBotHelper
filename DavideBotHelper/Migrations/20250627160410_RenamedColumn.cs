using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DavideBotHelper.Migrations
{
    /// <inheritdoc />
    public partial class RenamedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "version_regex",
                table: "rc_github_repository",
                newName: "asset_name_regex");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "asset_name_regex",
                table: "rc_github_repository",
                newName: "version_regex");
        }
    }
}
