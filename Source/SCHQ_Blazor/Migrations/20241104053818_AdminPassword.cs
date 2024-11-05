using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCHQ_Blazor.Migrations {
  /// <inheritdoc />
  public partial class AdminPassword : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
      migrationBuilder.AddColumn<string>(
          name: "AdminPassword",
          table: "Channels",
          type: "TEXT",
          nullable: true,
          collation: "NOCASE");
      migrationBuilder.Sql("UPDATE Channels SET AdminPassword = Password");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropColumn(
          name: "AdminPassword",
          table: "Channels");
    }
  }
}
