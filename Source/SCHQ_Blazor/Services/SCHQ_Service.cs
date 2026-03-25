using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SCHQ_Blazor.Classes;
using SCHQ_Blazor.Locales;
using SCHQ_Blazor.Models;
using SCHQ_Protos;
using System.Collections.Concurrent;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SCHQ_Blazor.Services;

public partial class SCHQ_Service(IStringLocalizer<Resource> localizer, RelationsContext dbContext, ChannelRelationsNotifier notifier, IHttpClientFactory httpClientFactory) : SCHQ_Relations.SCHQ_RelationsBase {

  #region Channels
  public override Task<SuccessReply> AddChannel(ChannelRequest request, ServerCallContext context) {
    return AddChannel(request);
  }

  public async Task<SuccessReply> AddChannel(ChannelRequest request) {
    SuccessReply rtnVal = new();

    if (!string.IsNullOrWhiteSpace(request.Channel)) {
      if (!string.IsNullOrWhiteSpace(request.AdminPassword)) {
        request.Channel = request.Channel.Trim();
        try {
          if (!await dbContext.Channels!.AnyAsync(c => c.Name != null && c.Name == request.Channel)) {
            DateTime utcNow = DateTime.UtcNow;
            dbContext.Add(new Channel() {
              DateCreated = utcNow,
              Timestamp = utcNow,
              Name = request.Channel,
              Description = request.Description,
              DecryptedPassword = request.Password,
              DecryptedAdminPassword = request.AdminPassword,
              Permissions = request.Permissons,
              DecryptedReadOnlyPassword = request.ReadOnlyPassword,
              DiscordWebhookUrl = request.DiscordWebhookUrl
            });
            rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
            if (!rtnVal.Success) {
              rtnVal.Info = localizer["No entries written"];
            }
          } else {
            rtnVal.Info = localizer["Channel already exists"];
          }
        } catch (Exception ex) {
          rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
        }
      } else {
        rtnVal.Info = localizer["No admin password was given"];
      }
    } else {
      rtnVal.Info = localizer["No channel name was given"];
    }

    return rtnVal;
  }

  public override Task<ChannelsReply> GetChannels(Empty request, ServerCallContext context) {
    return GetChannels();
  }

  public async Task<ChannelsReply> GetChannels() {
    ChannelsReply rtnVal = new();

    try {
      IOrderedQueryable<Channel> results = from c in dbContext.Channels!.AsNoTracking()
                                           where !c.Private
                                           orderby c.Name
                                           select c;
      foreach (Channel c in await results.ToListAsync()) {
        rtnVal.Channels.Add(new ChannelInfo() {
          Name = c.Name,
          Description = c.Description ?? string.Empty,
          HasPassword = c.Password?.Length > 0,
          Permissions = c.Permissions
        });
      }
    } catch { }

    return rtnVal;
  }

  public override Task<ChannelReply> GetChannel(ChannelNameRequest request, ServerCallContext context) {
    return GetChannel(request);
  }

  public async Task<ChannelReply> GetChannel(ChannelNameRequest request) {
    ChannelReply rtnVal = new();

    if (!string.IsNullOrWhiteSpace(request.Channel)) {
      request.Channel = request.Channel.Trim();
      try {
        if (await dbContext.Channels!.AsNoTracking().FirstOrDefaultAsync(c => c.Name == request.Channel) is Channel channel) {
          rtnVal = new ChannelReply() {
            Found = true,
            Channel = new() {
              Name = channel.Name,
              Description = channel.Description ?? string.Empty,
              HasPassword = channel.Password?.Length > 0,
              Permissions = channel.Permissions,
              Private = channel.Private
            }
          };
        }
      } catch { }
    }

    return rtnVal;
  }

  public override Task<SuccessReply> UpdateChannel(UpdateChannelRequest request, ServerCallContext context) {
    return UpdateChannel(request);
  }

  public async Task<SuccessReply> UpdateChannel(UpdateChannelRequest request) {
    SuccessReply rtnVal = new();

    if (!string.IsNullOrWhiteSpace(request.Channel)) {
      request.Channel = request.Channel.Trim();
      request.NewChannelName = request.NewChannelName.Trim();
      request.AdminPassword = !string.IsNullOrWhiteSpace(request.AdminPassword) ? Encryption.EncryptText(request.AdminPassword) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.FirstOrDefaultAsync(c => c.Name == request.Channel);
        if (channel != null) {
          if (channel.AdminPassword == request.AdminPassword) {
            if (!string.IsNullOrWhiteSpace(request.NewPassword) && !string.IsNullOrWhiteSpace(request.NewPasswordConfirm)) {
              if (request.NewPassword == request.NewPasswordConfirm) {
                channel.DecryptedPassword = request.NewPassword;
              } else {
                rtnVal.Info = localizer["New channel passwords don't match"];
              }
            }
            if (!string.IsNullOrWhiteSpace(request.NewReadOnlyPassword) && !string.IsNullOrWhiteSpace(request.NewReadOnlyPasswordConfirm)) {
              if (request.NewReadOnlyPassword == request.NewReadOnlyPasswordConfirm) {
                channel.DecryptedReadOnlyPassword = request.NewReadOnlyPassword;
              } else {
                rtnVal.Info = localizer["New channel R/O passwords don't match"];
              }
            }
            if (string.IsNullOrWhiteSpace(rtnVal.Info) && !string.IsNullOrWhiteSpace(request.NewAdminPassword) && !string.IsNullOrWhiteSpace(request.NewAdminPasswordConfirm)) {
              if (request.NewAdminPassword == request.NewAdminPasswordConfirm) {
                channel.DecryptedAdminPassword = request.NewAdminPassword;
              } else {
                rtnVal.Info = localizer["New admin passwords don't match"];
              }
            }
            if (string.IsNullOrWhiteSpace(rtnVal.Info) && !string.IsNullOrWhiteSpace(request.NewChannelName) && request.NewChannelName != channel.Name) {
              if (!await dbContext.Channels!.AnyAsync(c => c.Name!.Equals(request.NewChannelName, StringComparison.InvariantCultureIgnoreCase))) {
                channel.Name = request.NewChannelName;
              } else {
                rtnVal.Info = localizer["Channel already exists"];
              }
            }
            if (string.IsNullOrWhiteSpace(rtnVal.Info)) {
              channel.Permissions = request.Permissions;
              channel.Timestamp = DateTime.UtcNow;
              channel.Description = request.Description;
              channel.Private = request.Private;
              channel.DiscordWebhookUrl = request.DiscordWebhookUrl;
              dbContext.Update(channel);
              rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
              if (!rtnVal.Success) {
                rtnVal.Info = localizer["No entries written"];
              }
            }
          } else {
            rtnVal.Info = localizer["Access denied"];
          }
        } else {
          rtnVal.Info = localizer["Channel not found"];
        }
      } catch (Exception ex) {
        rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
      }
    } else {
      rtnVal.Info = localizer["No channel name was given"];
    }

    return rtnVal;
  }

  public override Task<SuccessReply> RemoveChannel(ChannelRequest request, ServerCallContext context) {
    return RemoveChannel(request);
  }

  public async Task<SuccessReply> RemoveChannel(ChannelRequest request) {
    SuccessReply rtnVal = new();

    if (!string.IsNullOrWhiteSpace(request.Channel)) {
      request.Channel = request.Channel.Trim();
      request.AdminPassword = !string.IsNullOrWhiteSpace(request.AdminPassword) ? Encryption.EncryptText(request.AdminPassword) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.FirstOrDefaultAsync(c => c.Name == request.Channel);
        if (channel != null) {
          if (channel.AdminPassword == request.AdminPassword) {
            dbContext.Remove(channel);
            rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
            if (!rtnVal.Success) {
              rtnVal.Info = localizer["No entries written"];
            }
          } else {
            rtnVal.Info = localizer["Access denied"];
          }
        } else {
          rtnVal.Info = localizer["Channel not found"];
        }
      } catch (Exception ex) {
        rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
      }
    } else {
      rtnVal.Info = localizer["No channel name was given"];
    }

    return rtnVal;
  }
  #endregion

  #region Relations
  public override Task<SuccessReply> SetRelations(SetRelationsRequest request, ServerCallContext context) {
    return SetRelations(request);
  }

  public async Task<SuccessReply> SetRelations(SetRelationsRequest request) {
    SuccessReply rtnVal = new();

    if (!string.IsNullOrWhiteSpace(request?.Channel)) {
      if (request?.Relations?.Count > 0) {
        request.Channel = request.Channel.Trim();
        request.Password = !string.IsNullOrWhiteSpace(request.Password) ? Encryption.EncryptText(request.Password) : string.Empty;
        try {
          Channel? channel = await dbContext.Channels!.FirstOrDefaultAsync(c => c.Name == request.Channel);
          if (channel != null) {
            if (channel.AdminPassword == request.Password) {
              List<Relation?> relations = [];
              foreach (RelationInfo relationInfo in request.Relations) {
                Relation? relation = await dbContext.Relations!.FirstOrDefaultAsync(r => r.Type == relationInfo.Type && r.ChannelId == channel.Id && r.Name == relationInfo.Name);
                DateTime utcNow = DateTime.UtcNow;
                relation ??= new() {
                  ChannelId = channel.Id,
                  Type = relationInfo.Type,
                  Name = relationInfo.Name,
                  DateCreated = utcNow
                };
                relation.Timestamp = utcNow;
                relation.UpdateCount++;
                if (relationInfo.Relation > RelationValue.NotAssigned) {
                  relation.Value = relationInfo.Relation;
                }
                if (relationInfo.Comment != null) {
                  relation.Comment = relationInfo.Comment;
                }
                relations.Add(relation);
              }
              dbContext.UpdateRange(relations!);
              rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
              if (!rtnVal.Success) {
                rtnVal.Info = localizer["No entries written"];
              } else {
                foreach (Relation rel in relations.OfType<Relation>()) {
                  notifier.Notify(request.Channel, new RelationChangedNotification {
                    ChannelName = request.Channel,
                    Relation = new RelationInfo {
                      Type = rel.Type,
                      Name = rel.Name,
                      Relation = rel.Value,
                      Comment = rel.Comment ?? string.Empty,
                      Timestamp = DateTime.SpecifyKind(rel.Timestamp, DateTimeKind.Utc).ToTimestamp()
                    }
                  });
                }
              }
            } else {
              rtnVal.Info = localizer["Access denied"];
            }
          } else {
            rtnVal.Info = localizer["Channel not found"];
          }
        } catch (Exception ex) {
          rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
        }
      } else {
        rtnVal.Info = localizer["No relations were given"];
      }
    } else {
      rtnVal.Info = localizer["No channel name was given"];
    }

    return rtnVal;
  }

  public override Task<SuccessReply> SetRelation(SetRelationRequest request, ServerCallContext context) {
    return SetRelation(request);
  }

  public async Task<SuccessReply> SetRelation(SetRelationRequest request, HandleInfo? handleInfo = null) {
    SuccessReply rtnVal = new();

    if (!string.IsNullOrWhiteSpace(request.Channel)) {
      if (!string.IsNullOrWhiteSpace(request.Relation?.Name)) {
        request.Channel = request.Channel.Trim();
        request.Relation.Name = request.Relation.Name.Trim();
        request.Password = !string.IsNullOrWhiteSpace(request.Password) ? Encryption.EncryptText(request.Password) : string.Empty;
        try {
          Channel? channel = await dbContext.Channels!.FirstOrDefaultAsync(c => c.Name == request.Channel);
          if (channel != null) {
            if (channel.Permissions >= ChannelPermissions.Write || channel.Password == request.Password) {
              Relation? relation = await dbContext.Relations!.FirstOrDefaultAsync(r => r.Type == request.Relation.Type && r.ChannelId == channel.Id && r.Name == request.Relation.Name);
              DateTime utcNow = DateTime.UtcNow;
              relation ??= new() {
                ChannelId = channel.Id,
                Type = request.Relation.Type,
                Name = request.Relation.Name,
                DateCreated = utcNow
              };
              DiscordWebhookRelationInfo webhookRelationInfo = new() {
                WebhookUrl = channel.DiscordWebhookUrl,
                Type = request.Relation.Type,
                Name = request.Relation.Name,
                OldRelation = relation.Value,
                OldComment = relation.Comment ?? string.Empty,
                NewRelation = request.Relation.Relation,
                NewComment = request.Relation.Comment ?? string.Empty
              };
              relation.Timestamp = utcNow;
              relation.UpdateCount++;
              relation.Value = request.Relation.Relation;
              if (request.Relation.Comment != null) {
                relation.Comment = request.Relation.Comment;
              }
              dbContext.Update(relation);
              rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
              if (!rtnVal.Success) {
                rtnVal.Info = localizer["No entries written"];
              } else {
                notifier.Notify(request.Channel, new RelationChangedNotification {
                  ChannelName = request.Channel,
                  Relation = new RelationInfo {
                    Type = relation.Type,
                    Name = relation.Name,
                    Relation = relation.Value,
                    Comment = relation.Comment ?? string.Empty,
                    Timestamp = DateTime.SpecifyKind(relation.Timestamp, DateTimeKind.Utc).ToTimestamp()
                  }
                });
                PushRelationWebhook(webhookRelationInfo, handleInfo);
              }
            } else {
              rtnVal.Info = localizer["Access denied"];
            }
          } else {
            rtnVal.Info = localizer["Channel not found"];
          }
        } catch (Exception ex) {
          rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
        }
      } else {
        rtnVal.Info = localizer["No relation name was given"];
      }
    } else {
      rtnVal.Info = localizer["No channel name was given"];
    }

    return rtnVal;
  }

  public override Task<RelationsReply> GetRelations(ChannelRequest request, ServerCallContext context) {
    return GetRelations(request);
  }

  public async Task<RelationsReply> GetRelations(ChannelRequest request) {
    RelationsReply rtnVal = new();

    if (!string.IsNullOrWhiteSpace(request.Channel)) {
      request.Channel = request.Channel.Trim();
      request.Password = !string.IsNullOrWhiteSpace(request.Password) ? Encryption.EncryptText(request.Password) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.AsNoTracking().FirstOrDefaultAsync(c => c.Name == request.Channel && (c.Permissions >= ChannelPermissions.Read || c.AdminPassword == request.Password || c.Password == request.Password || (!string.IsNullOrWhiteSpace(c.ReadOnlyPassword) && c.ReadOnlyPassword == request.Password)));
        if (channel != null) {
          IOrderedQueryable<Relation> results = from rel in dbContext.Relations!.AsNoTracking()
                                                where rel.ChannelId == channel.Id
                                                orderby rel.Type descending, rel.Name
                                                select rel;
          foreach (Relation rel in await results.ToListAsync()) {
            rtnVal.Relations.Add(new RelationInfo() {
              Type = rel.Type,
              Name = rel.Name,
              Relation = rel.Value,
              Comment = !string.IsNullOrWhiteSpace(rel.Comment) ? rel.Comment : null,
              Timestamp = DateTime.SpecifyKind(rel.Timestamp, DateTimeKind.Utc).ToTimestamp()
            });
          }
        } else {
          rtnVal.Info = localizer["Access denied"];
        }
      } catch (Exception ex) {
        rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
      }
    }

    return rtnVal;
  }

  public override Task<RelationReply> GetRelation(RelationRequest request, ServerCallContext context) {
    return GetRelation(request);
  }

  public async Task<RelationReply> GetRelation(RelationRequest request) {
    RelationReply rtnVal = new();

    if (!string.IsNullOrWhiteSpace(request.Channel) && !string.IsNullOrWhiteSpace(request.Name)) {
      request.Channel = request.Channel.Trim();
      request.Name = request.Name.Trim();
      request.Password = !string.IsNullOrWhiteSpace(request.Password) ? Encryption.EncryptText(request.Password) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.AsNoTracking().FirstOrDefaultAsync(c => c.Name == request.Channel && (c.Permissions >= ChannelPermissions.Read || c.Password == request.Password || (!string.IsNullOrWhiteSpace(c.ReadOnlyPassword) && c.ReadOnlyPassword == request.Password)));
        if (channel != null) {
          IQueryable<Relation> results = from rel in dbContext.Relations!.AsNoTracking()
                                         where rel.ChannelId == channel.Id && rel.Type == request.Type && rel.Name == request.Name
                                         select rel;
          foreach (Relation rel in await results.ToListAsync()) {
            rtnVal = new RelationReply() {
              Found = true,
              Relation = rel.Value,
              Timestamp = DateTime.SpecifyKind(rel.Timestamp, DateTimeKind.Utc).ToTimestamp()
            };
          }
        }
      } catch { }
    }

    return rtnVal;
  }

  public override async Task SyncRelations(ChannelRequest request, IServerStreamWriter<SyncRelationsReply> responseStream, ServerCallContext context) {
    if (!string.IsNullOrWhiteSpace(request.Channel)) {
      request.Channel = request.Channel.Trim();
      request.Password = !string.IsNullOrWhiteSpace(request.Password) ? Encryption.EncryptText(request.Password) : string.Empty;
      try {
        Channel? channel = dbContext.Channels!.FirstOrDefault(c => c.Name == request.Channel && (c.Permissions >= ChannelPermissions.Read || c.Password == request.Password || (!string.IsNullOrWhiteSpace(c.ReadOnlyPassword) && c.ReadOnlyPassword == request.Password)));
        if (channel != null) {
          var reader = notifier.Subscribe(request.Channel);
          try {
            await foreach (var notification in reader.ReadAllAsync(context.CancellationToken)) {
              if (notification.Relation != null) {
                await responseStream.WriteAsync(new SyncRelationsReply() {
                  Channel = request.Channel,
                  Relation = notification.Relation
                });
              }
            }
          } catch (OperationCanceledException) { } finally {
            notifier.Unsubscribe(request.Channel, reader);
          }
        }
      } catch { }
    }

  }

  public override Task<SuccessReply> RemoveRelations(ChannelRequest request, ServerCallContext context) {
    return RemoveRelations(request);
  }

  public async Task<SuccessReply> RemoveRelations(ChannelRequest request) {
    SuccessReply rtnVal = new();

    if (!string.IsNullOrWhiteSpace(request.Channel)) {
      request.Channel = request.Channel.Trim();
      request.AdminPassword = !string.IsNullOrWhiteSpace(request.AdminPassword) ? Encryption.EncryptText(request.AdminPassword) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.FirstOrDefaultAsync(c => c.Name == request.Channel);
        if (channel != null) {
          if (channel.AdminPassword == request.AdminPassword) {
            dbContext.RemoveRange(dbContext.Relations!.Where(r => r.ChannelId == channel.Id));
            rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
            if (!rtnVal.Success) {
              rtnVal.Info = localizer["No entries written"];
            } else {
              notifier.Notify(request.Channel, new RelationChangedNotification { ChannelName = request.Channel });
            }
          } else {
            rtnVal.Info = localizer["Access denied"];
          }
        } else {
          rtnVal.Info = localizer["Channel not found"];
        }
      } catch (Exception ex) {
        rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
      }
    } else {
      rtnVal.Info = localizer["No channel name was given"];
    }

    return rtnVal;
  }
  #endregion

  #region Webhooks
  private static readonly Regex RgxDiscordWebhookUrl = RegexDiscordWebhookUrl();
  [GeneratedRegex(@"^https:\/\/discord.com\/api\/webhooks\/\d+\/[a-zA-Z0-9_-]+$", RegexOptions.Compiled)]
  private static partial Regex RegexDiscordWebhookUrl();

  private static bool IsValidDiscordWebhookUrl(string url) => !string.IsNullOrWhiteSpace(url) && RgxDiscordWebhookUrl.IsMatch(url);

  private static readonly ConcurrentDictionary<string, byte> DiscordWebhooks = [];

  private static async Task RemoveDiscordWebhookLater(string key) {
    await Task.Delay(TimeSpan.FromSeconds(30));
    _ = DiscordWebhooks.TryRemove(key, out _);
  }

  public override async Task<SuccessReply> PushWebhook(WebhookRequest request, ServerCallContext context) {
    return await PushWebhook(request, true);
  }

  public async Task<SuccessReply> PushWebhook(WebhookRequest request, bool withWait = false) {
    SuccessReply rtnVal = new();

    if (IsValidDiscordWebhookUrl(request.Url) && !string.IsNullOrWhiteSpace(request.Body)) {
      try {
        DiscordWebhook? webhook = JsonSerializer.Deserialize<DiscordWebhook?>(request.Body);
        if (webhook != null) {
          string? key = webhook?.embeds?.Count == 2 ? $"{request.Url},{webhook.embeds[0].description},{webhook.embeds[1].description}" : string.Empty;
          if (!withWait || DiscordWebhooks.TryAdd(key, 0)) {
            if (withWait) {
              _ = RemoveDiscordWebhookLater(key);
            }
            using HttpClient client = httpClientFactory.CreateClient();
            HttpResponseMessage response = await client.PostAsync(request.Url, new StringContent(request.Body, Encoding.UTF8, MediaTypeNames.Application.Json));
            rtnVal.Success = response.IsSuccessStatusCode;
            if (!rtnVal.Success) {
              rtnVal.Info = $"{response.StatusCode} ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}";
            }
          } else {
            rtnVal.Info = "Event already exists";
          }
        } else {
          rtnVal.Info = "Webhook body invalid";
        }
      } catch (Exception ex) {
        rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
      }
    }

    return rtnVal;
  }

  private async void PushRelationWebhook(DiscordWebhookRelationInfo webhookRelationInfo, HandleInfo? handleInfo) {
    if (webhookRelationInfo.OldRelation != webhookRelationInfo.NewRelation || webhookRelationInfo.OldComment != webhookRelationInfo.NewComment) {
      string avatarUrl = HandleQuery.DefaultAvatarUrl;
      if (webhookRelationInfo.Type == RelationType.Handle) {
        if (!string.IsNullOrWhiteSpace(handleInfo?.Profile?.AvatarUrl)) {
          avatarUrl = handleInfo.Profile.AvatarUrl;
        }
      } else {
        if (handleInfo?.Organizations?.MainOrganization != null && !string.IsNullOrWhiteSpace(handleInfo.Organizations.MainOrganization.AvatarUrl)) {
          avatarUrl = handleInfo.Organizations.MainOrganization.AvatarUrl;
        } else if (handleInfo?.Organizations?.Affiliations?.Count > 0) {
          OrganizationInfo? orgInfo = handleInfo.Organizations.Affiliations.FirstOrDefault(aff => aff?.Sid != null && aff.Sid.Equals(webhookRelationInfo.Name, StringComparison.OrdinalIgnoreCase));
          if (!string.IsNullOrWhiteSpace(orgInfo?.AvatarUrl)) {
            avatarUrl = orgInfo.AvatarUrl;
          }
        }
      }
      List<DiscordEmbedField> fields = [
        new() {
          name = "TYP",
          value = $"{webhookRelationInfo.Type}"[..1].ToUpper(),
          inline = true
        }
      ];
      if (webhookRelationInfo.NewRelation != webhookRelationInfo.OldRelation) {
        fields.AddRange(new() {
          name = "ALT",
          value = $"{webhookRelationInfo.OldRelation}"[..2].ToUpper(),
          inline = true
        },
        new() {
          name = "NEU",
          value = $"{webhookRelationInfo.NewRelation}"[..2].ToUpper(),
          inline = true
        });
      }
      if (webhookRelationInfo.OldComment != webhookRelationInfo.NewComment) {
        fields.Add(new() {
          name = "KOMMENTAR ALT",
          value = webhookRelationInfo.OldComment
        });
        fields.Add(new() {
          name = "KOMMENTAR NEU",
          value = webhookRelationInfo.NewComment
        });
      }
      List<DiscordEmbed> embeds = [
        new() {
          author = new() {
            name = webhookRelationInfo.Name,
            url = webhookRelationInfo.Url,
            icon_url = CorrectUrl(avatarUrl)
          },
          color = GetWebhookRelationColor(webhookRelationInfo.NewRelation ?? RelationValue.NotAssigned),
          fields = fields
        }
      ];
      DiscordWebhook webhook = new() {
        embeds = embeds
      };
      await PushWebhook(new() {
        Url = webhookRelationInfo.WebhookUrl,
        Body = JsonSerializer.Serialize(webhook)
      });
    }
  }

  private static int? GetWebhookRelationColor(RelationValue relation) {
    return relation switch {
      RelationValue.Friendly => 5763719,
      RelationValue.Neutral => 9807270,
      RelationValue.Bogey => 15105570,
      RelationValue.Bandit => 15548997,
      _ => null
    };
  }
  private static string CorrectUrl(string url) {
    return url.StartsWith('/') ? $"https://robertsspaceindustries.com{url}" : url;
  }

  #endregion

}
