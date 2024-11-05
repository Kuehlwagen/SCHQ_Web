using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCHQ_Blazor.Migrations {
  /// <inheritdoc />
  public partial class Date_Description : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
      migrationBuilder.AddColumn<DateTime>(
          name: "DateCreated",
          table: "Relations",
          type: "TEXT",
          nullable: false,
          defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

      migrationBuilder.AddColumn<int>(
          name: "UpdateCount",
          table: "Relations",
          type: "INTEGER",
          nullable: false,
          defaultValue: 0);

      migrationBuilder.Sql("UPDATE Relations SET DateCreated = Timestamp");

      migrationBuilder.AddColumn<DateTime>(
          name: "DateCreated",
          table: "Channels",
          type: "TEXT",
          nullable: false,
          defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

      migrationBuilder.AddColumn<string>(
          name: "Description",
          table: "Channels",
          type: "TEXT",
          nullable: true,
          collation: "NOCASE");

      migrationBuilder.AddColumn<DateTime>(
          name: "Timestamp",
          table: "Channels",
          type: "TEXT",
          nullable: false,
          defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

      migrationBuilder.Sql("UPDATE Channels SET DateCreated = DATETIME(), Timestamp = DATETIME()");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropColumn(
          name: "DateCreated",
          table: "Relations");

      migrationBuilder.DropColumn(
          name: "UpdateCount",
          table: "Relations");

      migrationBuilder.DropColumn(
          name: "DateCreated",
          table: "Channels");

      migrationBuilder.DropColumn(
          name: "Description",
          table: "Channels");

      migrationBuilder.DropColumn(
          name: "Timestamp",
          table: "Channels");
    }
  }
}
