using Microsoft.EntityFrameworkCore;
using SCHQ_Blazor.Classes;
using SCHQ_Protos;
using System.ComponentModel.DataAnnotations.Schema;
using DAS = System.ComponentModel.DataAnnotations.Schema;

namespace SCHQ_Blazor.Models;

public class RelationsContext(string connectionString) : DbContext {

  public DbSet<Relation>? Relations { get; set; }
  public DbSet<Channel>? Channels { get; set; }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseMySQL(connectionString);

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    => configurationBuilder.Properties<string>().UseCollation("NOCASE");

}

[Table("Relations"), Index("ChannelId", ["Type", "Name"], IsUnique = true, Name = "RelationID"), PrimaryKey("Id")]
public class Relation {
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; }
  public DateTime DateCreated {  get; set; }
  public int UpdateCount { get; set; }
  public DateTime Timestamp { get; set; }
  public int ChannelId { get; set; }
  public Channel? Channel { get; set; }
  public RelationType Type { get; set; }
  public string? Name { get; set; }
  public RelationValue Value { get; set; }
  public string? Comment { get; set; }
}

[Table("Channels"), Index("Name", [], IsUnique = true, Name = "ChannelName"), PrimaryKey("Id")]
public class Channel {
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; }
  public DateTime DateCreated { get; set; }
  public DateTime Timestamp { get; set; }
  public string? Name { get; set; }
  public string? Description { get; set; }
  [DAS.NotMapped]
  public string? DecryptedPassword {
    get { return Encryption.DecryptText(Password); }
    set { Password = !string.IsNullOrEmpty(value) ? Encryption.EncryptText(value) : string.Empty; }
  }
  public string? Password { get; set; }
  [DAS.NotMapped]
  public string? DecryptedAdminPassword {
    get { return Encryption.DecryptText(AdminPassword); }
    set { AdminPassword = !string.IsNullOrEmpty(value) ? Encryption.EncryptText(value) : string.Empty; }
  }
  public string? AdminPassword { get; set; }
  public ChannelPermissions Permissions { get; set; }
}
