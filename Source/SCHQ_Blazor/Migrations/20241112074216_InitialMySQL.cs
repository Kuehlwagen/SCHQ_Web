using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace SCHQ_Blazor.Migrations {
  /// <inheritdoc />
  public partial class InitialMySQL : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
      migrationBuilder.AlterDatabase()
          .Annotation("MySQL:Charset", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "Channels",
          columns: table => new {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
            DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            Name = table.Column<string>(type: "varchar(255)", nullable: true, collation: "NOCASE"),
            Description = table.Column<string>(type: "longtext", nullable: true, collation: "NOCASE"),
            Password = table.Column<string>(type: "longtext", nullable: true, collation: "NOCASE"),
            AdminPassword = table.Column<string>(type: "longtext", nullable: true, collation: "NOCASE"),
            Permissions = table.Column<int>(type: "int", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_Channels", x => x.Id);
          })
          .Annotation("MySQL:Charset", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "Relations",
          columns: table => new {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
            DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            UpdateCount = table.Column<int>(type: "int", nullable: false),
            Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            ChannelId = table.Column<int>(type: "int", nullable: false),
            Type = table.Column<int>(type: "int", nullable: false),
            Name = table.Column<string>(type: "varchar(255)", nullable: true, collation: "NOCASE"),
            Value = table.Column<int>(type: "int", nullable: false),
            Comment = table.Column<string>(type: "longtext", nullable: true, collation: "NOCASE")
          },
          constraints: table => {
            table.PrimaryKey("PK_Relations", x => x.Id);
            table.ForeignKey(
                      name: "FK_Relations_Channels_ChannelId",
                      column: x => x.ChannelId,
                      principalTable: "Channels",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          })
          .Annotation("MySQL:Charset", "utf8mb4");

      migrationBuilder.CreateIndex(
          name: "ChannelName",
          table: "Channels",
          column: "Name",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "RelationID",
          table: "Relations",
          columns: new[] { "ChannelId", "Type", "Name" },
          unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropTable(
          name: "Relations");

      migrationBuilder.DropTable(
          name: "Channels");
    }
  }
}
