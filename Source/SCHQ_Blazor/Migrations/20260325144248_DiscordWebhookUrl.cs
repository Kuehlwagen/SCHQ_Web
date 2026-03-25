using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCHQ_Blazor.Migrations {
  /// <inheritdoc />
  public partial class DiscordWebhookUrl : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
      migrationBuilder.AddColumn<string>(
          name: "DiscordWebhookUrl",
          table: "Channels",
          type: "longtext",
          nullable: true,
          collation: "NOCASE");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropColumn(
          name: "DiscordWebhookUrl",
          table: "Channels");
    }
  }
}
