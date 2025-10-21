namespace SCHQ_Blazor.Classes;

#pragma warning disable IDE1006 // Benennungsstile
public class DiscordWebhook {
  public List<DiscordEmbed>? embeds { get; set; } = [];
}

public class DiscordEmbed {
  public string? description { get; set; }
}
#pragma warning restore IDE1006 // Benennungsstile
