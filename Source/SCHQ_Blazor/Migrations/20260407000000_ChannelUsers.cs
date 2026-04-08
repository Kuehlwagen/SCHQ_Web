using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace SCHQ_Blazor.Migrations {
  /// <inheritdoc />
  public partial class ChannelUsers : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropColumn(
          name: "Password",
          table: "Channels");

      migrationBuilder.DropColumn(
          name: "Permissions",
          table: "Channels");

      migrationBuilder.DropColumn(
          name: "ReadOnlyPassword",
          table: "Channels");

      migrationBuilder.CreateTable(
          name: "Users",
          columns: table => new {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
            ChannelId = table.Column<int>(type: "int", nullable: false),
            Username = table.Column<string>(type: "varchar(255)", nullable: true, collation: "NOCASE"),
            Password = table.Column<string>(type: "longtext", nullable: true, collation: "NOCASE"),
            Permissions = table.Column<int>(type: "int", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_Users", x => x.Id);
            table.ForeignKey(
                      name: "FK_Users_Channels_ChannelId",
                      column: x => x.ChannelId,
                      principalTable: "Channels",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          })
          .Annotation("MySQL:Charset", "utf8mb4");

      migrationBuilder.CreateIndex(
          name: "UsersIndex",
          table: "Users",
          columns: ["ChannelId", "Username"],
          unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropTable(
          name: "Users");

      migrationBuilder.AddColumn<string>(
          name: "Password",
          table: "Channels",
          type: "longtext",
          nullable: true,
          collation: "NOCASE");

      migrationBuilder.AddColumn<int>(
          name: "Permissions",
          table: "Channels",
          type: "int",
          nullable: false,
          defaultValue: 0);

      migrationBuilder.AddColumn<string>(
          name: "ReadOnlyPassword",
          table: "Channels",
          type: "longtext",
          nullable: true,
          collation: "NOCASE");
    }
  }
}
