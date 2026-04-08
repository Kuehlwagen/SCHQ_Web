using Microsoft.EntityFrameworkCore;
using SCHQ_Blazor.Classes;
using SCHQ_Protos;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCHQ_Blazor.Models;

public class RelationsContext(DbContextOptions<RelationsContext> options) : DbContext(options) {

  public DbSet<Relation> Relations => Set<Relation>();
  public DbSet<Channel> Channels => Set<Channel>();
  public DbSet<Tag> Tags => Set<Tag>();
  public DbSet<User> Users => Set<User>();

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    => configurationBuilder.Properties<string>().UseCollation("NOCASE");

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<Relation>()
      .HasMany(r => r.Tags)
      .WithMany();
    modelBuilder.Entity<Channel>()
      .HasMany(c => c.Users)
      .WithOne(u => u.Channel)
      .HasForeignKey(u => u.ChannelId)
      .OnDelete(DeleteBehavior.Cascade);
  }

}

[Table("Relations"), Index("ChannelId", ["Type", "Name"], IsUnique = true, Name = "RelationID"), PrimaryKey("Id")]
public class Relation {
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; }
  public DateTime DateCreated { get; set; }
  public int UpdateCount { get; set; }
  public DateTime Timestamp { get; set; }
  public int ChannelId { get; set; }
  public Channel? Channel { get; set; }
  public RelationType Type { get; set; }
  public string? Name { get; set; }
  public RelationValue Value { get; set; }
  public string? Comment { get; set; }
  public string? TagIds { get; set; }
  public List<Tag> Tags { get; set; } = [];
}

[Table("Channels"), Index("Name", [], IsUnique = true, Name = "ChannelName"), PrimaryKey("Id")]
public class Channel {
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; }
  public DateTime DateCreated { get; set; }
  public DateTime Timestamp { get; set; }
  public string? Name { get; set; }
  public string? Description { get; set; }
  [NotMapped]
  public string? DecryptedAdminPassword {
    get { return Encryption.DecryptText(AdminPassword); }
    set { AdminPassword = !string.IsNullOrEmpty(value) ? Encryption.EncryptText(value) : string.Empty; }
  }
  public string? AdminPassword { get; set; }
  public bool Private { get; set; }
  public string? DiscordWebhookUrl { get; set; }
  public List<Tag> Tags { get; set; } = [];
  public List<User> Users { get; set; } = [];
}

[Table("Tags"), Index("ChannelId", ["Value"], IsUnique = true, Name = "ChannelTagIndex"), PrimaryKey("Id")]
public class Tag {
  public int Id { get; set; }
  public int ChannelId { get; set; }
  public string? Value { get; set; }
  public string? Description { get; set; }
  public TagColor Color { get; set; }
}

[Table("Users"), Index("ChannelId", ["Username"], IsUnique = true, Name = "UsersIndex"), PrimaryKey("Id")]
public class User {
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; }
  public int ChannelId { get; set; }
  public Channel? Channel { get; set; }
  public string? Username { get; set; }
  public string? Password { get; set; }
  [NotMapped]
  public string? DecryptedPassword {
    get { return Encryption.DecryptText(Password); }
    set { Password = !string.IsNullOrEmpty(value) ? Encryption.EncryptText(value) : string.Empty; }
  }
  public ChannelPermissions Permissions { get; set; }
}

public enum TagColor {
  Success,
  Secondary,
  Warning,
  Danger,
  Dark
}
