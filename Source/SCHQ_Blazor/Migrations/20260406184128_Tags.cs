using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace SCHQ_Blazor.Migrations {
  /// <inheritdoc />
  public partial class Tags : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
      migrationBuilder.AddColumn<string>(
          name: "TagIds",
          table: "Relations",
          type: "longtext",
          nullable: true,
          collation: "NOCASE");

      migrationBuilder.CreateTable(
          name: "Tags",
          columns: table => new {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
            ChannelId = table.Column<int>(type: "int", nullable: false),
            Value = table.Column<string>(type: "varchar(255)", nullable: true, collation: "NOCASE"),
            Description = table.Column<string>(type: "longtext", nullable: true, collation: "NOCASE"),
            Color = table.Column<int>(type: "int", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_Tags", x => x.Id);
            table.ForeignKey(
                      name: "FK_Tags_Channels_ChannelId",
                      column: x => x.ChannelId,
                      principalTable: "Channels",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          })
          .Annotation("MySQL:Charset", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "RelationTag",
          columns: table => new {
            RelationId = table.Column<int>(type: "int", nullable: false),
            TagsId = table.Column<int>(type: "int", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_RelationTag", x => new { x.RelationId, x.TagsId });
            table.ForeignKey(
                      name: "FK_RelationTag_Relations_RelationId",
                      column: x => x.RelationId,
                      principalTable: "Relations",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_RelationTag_Tags_TagsId",
                      column: x => x.TagsId,
                      principalTable: "Tags",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          })
          .Annotation("MySQL:Charset", "utf8mb4");

      migrationBuilder.CreateIndex(
          name: "IX_RelationTag_TagsId",
          table: "RelationTag",
          column: "TagsId");

      migrationBuilder.CreateIndex(
          name: "ChannelTagIndex",
          table: "Tags",
          columns: new[] { "ChannelId", "Value" },
          unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropTable(
          name: "RelationTag");

      migrationBuilder.DropTable(
          name: "Tags");

      migrationBuilder.DropColumn(
          name: "TagIds",
          table: "Relations");
    }
  }
}
