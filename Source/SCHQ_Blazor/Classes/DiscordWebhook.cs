using SCHQ_Protos;

namespace SCHQ_Blazor.Classes;

#pragma warning disable IDE1006 // Benennungsstile
public class DiscordWebhook {
  public string? username { get; set; }
  public string? avatar_url { get; set; }
  public string? content { get; set; }
  public List<DiscordEmbed>? embeds { get; set; } = [];
}

public class DiscordEmbed {
  public int? color { get; set; }
  public DiscordEmbedAuthor? author { get; set; }
  public string? title { get; set; }
  public string? url { get; set; }
  public string? description { get; set; }
  public List<DiscordEmbedField>? fields { get; set; }
  public DiscordEmbedFooter? footer { get; set; }
}

public class DiscordEmbedAuthor {
  public string? name { get; set; }
  public string? url { get; set; }
  public string? icon_url { get; set; }
}

public class DiscordEmbedField {
  public string? name { get; set; }
  public string? value { get; set; }
  public bool? inline { get; set; }
}

public class DiscordEmbedFooter {
  public string? text { get; set; }
  public string? icon_url { get; set; }
}
#pragma warning restore IDE1006 // Benennungsstile

public class DiscordWebhookRelationInfo {
  public string? WebhookUrl { get; set; }
  public string? Username { get; set; }
  public RelationType? Type { get; set; }
  public string? Name { get; set; }
  public RelationValue? OldRelation { get; set; }
  public RelationValue? NewRelation { get; set; }
  public string? OldComment { get; set; }
  public string? NewComment { get; set; }
  public string? TagValue { get; set; }
  public bool? TagAdded { get; set; }
  public string? Url => Type switch {
    RelationType.Organization => $"https://robertsspaceindustries.com/orgs/{Name}",
    RelationType.Handle => $"https://robertsspaceindustries.com/citizens/{Name}",
    _ => null
  };
}
