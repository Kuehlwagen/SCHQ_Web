using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCHQ_Blazor.Migrations
{
    /// <inheritdoc />
    public partial class ReadOnlyPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReadOnlyPassword",
                table: "Channels",
                type: "longtext",
                nullable: true,
                collation: "NOCASE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReadOnlyPassword",
                table: "Channels");
        }
    }
}
