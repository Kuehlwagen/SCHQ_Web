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
              DecryptedAdminPassword = request.AdminPassword,
              Private = request.Private,
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
      List<Channel> channels = await dbContext.Channels!.AsNoTracking()
        .Include(c => c.Users)
        .Where(c => !c.Private)
        .OrderBy(c => c.Name)
        .ToListAsync();
      foreach (Channel c in channels) {
        rtnVal.Channels.Add(new ChannelInfo() {
          Name = c.Name,
          Description = c.Description ?? string.Empty,
          HasUsers = c.Users.Count != 0
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
        if (await dbContext.Channels!.AsNoTracking().Include(c => c.Users).FirstOrDefaultAsync(c => c.Name == request.Channel) is Channel channel) {
          rtnVal = new ChannelReply() {
            Found = true,
            Channel = new() {
              Name = channel.Name,
              Description = channel.Description ?? string.Empty,
              HasUsers = channel.Users.Count != 0,
              Private = channel.Private,
              DiscordWebhookUrl = channel.DiscordWebhookUrl ?? string.Empty
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
            if (!string.IsNullOrWhiteSpace(request.NewAdminPassword) && !string.IsNullOrWhiteSpace(request.NewAdminPasswordConfirm)) {
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

  public async Task<bool> ValidateAdminPassword(string? channelName, string? adminPassword) {
    if (string.IsNullOrWhiteSpace(channelName) || string.IsNullOrWhiteSpace(adminPassword)) {
      return false;
    }
    channelName = channelName.Trim();
    string encryptedPassword = Encryption.EncryptText(adminPassword);
    try {
      return await dbContext.Channels!.AsNoTracking().AnyAsync(c => c.Name == channelName && c.AdminPassword == encryptedPassword);
    } catch { }
    return false;
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
        request.Username = request.Username?.Trim() ?? string.Empty;
        try {
          Channel? channel = await dbContext.Channels!.Include(c => c.Users).FirstOrDefaultAsync(c => c.Name == request.Channel);
          if (channel != null) {
            if (channel.Users.Count == 0 || channel.Users.Any(u => u.Permissions >= ChannelPermissions.Write && u.Username == request.Username && u.Password == request.Password)) {
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
                Username = request.Username,
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
      request.Username = request.Username?.Trim() ?? string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.AsNoTracking().Include(c => c.Users).FirstOrDefaultAsync(c => c.Name == request.Channel);
        if (channel != null && (channel.Users.Count == 0 || channel.Users.Any(u => u.Permissions >= ChannelPermissions.Read && u.Username == request.Username && u.Password == request.Password))) {
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
      request.Username = request.Username?.Trim() ?? string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.AsNoTracking().Include(c => c.Users).FirstOrDefaultAsync(c => c.Name == request.Channel);
        if (channel != null && (channel.Users.Count == 0 || channel.Users.Any(u => u.Permissions >= ChannelPermissions.Read && u.Username == request.Username && u.Password == request.Password))) {
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
      request.Username = request.Username?.Trim() ?? string.Empty;
      try {
        Channel? channel = dbContext.Channels!.Include(c => c.Users).FirstOrDefault(c => c.Name == request.Channel);
        if (channel != null && (channel.Users.Count == 0 || channel.Users.Any(u => u.Permissions >= ChannelPermissions.Read && u.Username == request.Username && u.Password == request.Password))) {
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

  #region Tags
  public async Task<List<Tag>> GetTags(string channelName) {
    List<Tag> rtnVal = [];
    if (!string.IsNullOrWhiteSpace(channelName)) {
      channelName = channelName.Trim();
      try {
        Channel? channel = await dbContext.Channels!.AsNoTracking().FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel != null) {
          rtnVal = await dbContext.Tags!.AsNoTracking().Where(t => t.ChannelId == channel.Id).OrderBy(t => t.Value).ToListAsync();
        }
      } catch { }
    }
    return rtnVal;
  }

  public async Task<SuccessReply> AddTag(string channelName, string adminPassword, string value, string? description, TagColor color) {
    SuccessReply rtnVal = new();
    if (!string.IsNullOrWhiteSpace(channelName) && !string.IsNullOrWhiteSpace(value)) {
      channelName = channelName.Trim();
      value = value.Trim();
      adminPassword = !string.IsNullOrWhiteSpace(adminPassword) ? Encryption.EncryptText(adminPassword) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.FirstOrDefaultAsync(c => c.Name == channelName && c.AdminPassword == adminPassword);
        if (channel != null) {
          if (!await dbContext.Tags!.AnyAsync(t => t.ChannelId == channel.Id && t.Value == value)) {
            dbContext.Add(new Tag() {
              ChannelId = channel.Id,
              Value = value,
              Description = description,
              Color = color
            });
            rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
            if (!rtnVal.Success) {
              rtnVal.Info = localizer["No entries written"];
            }
          } else {
            rtnVal.Info = localizer["Tag already exists"];
          }
        } else {
          rtnVal.Info = localizer["Access denied"];
        }
      } catch (Exception ex) {
        rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
      }
    } else {
      rtnVal.Info = localizer["No tag value was given"];
    }
    return rtnVal;
  }

  public async Task<SuccessReply> UpdateTag(string channelName, string adminPassword, int tagId, string value, string? description, TagColor color) {
    SuccessReply rtnVal = new();
    if (!string.IsNullOrWhiteSpace(channelName) && !string.IsNullOrWhiteSpace(value)) {
      channelName = channelName.Trim();
      value = value.Trim();
      adminPassword = !string.IsNullOrWhiteSpace(adminPassword) ? Encryption.EncryptText(adminPassword) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.FirstOrDefaultAsync(c => c.Name == channelName && c.AdminPassword == adminPassword);
        if (channel != null) {
          Tag? tag = await dbContext.Tags!.FirstOrDefaultAsync(t => t.Id == tagId && t.ChannelId == channel.Id);
          if (tag != null) {
            tag.Value = value;
            tag.Description = description;
            tag.Color = color;
            dbContext.Update(tag);
            rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
            if (!rtnVal.Success) {
              rtnVal.Info = localizer["No entries written"];
            }
          } else {
            rtnVal.Info = localizer["Tag not found"];
          }
        } else {
          rtnVal.Info = localizer["Access denied"];
        }
      } catch (Exception ex) {
        rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
      }
    } else {
      rtnVal.Info = localizer["No tag value was given"];
    }
    return rtnVal;
  }

  public async Task<SuccessReply> RemoveTag(string channelName, string adminPassword, int tagId) {
    SuccessReply rtnVal = new();
    if (!string.IsNullOrWhiteSpace(channelName)) {
      channelName = channelName.Trim();
      adminPassword = !string.IsNullOrWhiteSpace(adminPassword) ? Encryption.EncryptText(adminPassword) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.FirstOrDefaultAsync(c => c.Name == channelName && c.AdminPassword == adminPassword);
        if (channel != null) {
          Tag? tag = await dbContext.Tags!.FirstOrDefaultAsync(t => t.Id == tagId && t.ChannelId == channel.Id);
          if (tag != null) {
            dbContext.Remove(tag);
            rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
            if (!rtnVal.Success) {
              rtnVal.Info = localizer["No entries written"];
            }
          } else {
            rtnVal.Info = localizer["Tag not found"];
          }
        } else {
          rtnVal.Info = localizer["Access denied"];
        }
      } catch (Exception ex) {
        rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
      }
    } else {
      rtnVal.Info = localizer["No channel name was given"];
    }
    return rtnVal;
  }

  public async Task<Dictionary<string, List<int>>> GetRelationTagIds(string channelName) {
    Dictionary<string, List<int>> rtnVal = [];
    if (!string.IsNullOrWhiteSpace(channelName)) {
      channelName = channelName.Trim();
      try {
        Channel? channel = await dbContext.Channels!.AsNoTracking().FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel != null) {
          var data = await dbContext.Relations!
            .AsNoTracking()
            .Where(r => r.ChannelId == channel.Id)
            .Select(r => new { Key = $"{(int)r.Type}|{r.Name}", TagIds = r.Tags.Select(t => t.Id).ToList() })
            .ToListAsync();
          rtnVal = data.Where(r => r.TagIds.Count > 0).ToDictionary(r => r.Key, r => r.TagIds);
        }
      } catch { }
    }
    return rtnVal;
  }

  public async Task<SuccessReply> AddRelationTag(string channelName, string username, string password, RelationType type, string name, int tagId, HandleInfo? handleInfo = null) {
    SuccessReply rtnVal = new();
    if (!string.IsNullOrWhiteSpace(channelName) && !string.IsNullOrWhiteSpace(name)) {
      channelName = channelName.Trim();
      name = name.Trim();
      username = username?.Trim() ?? string.Empty;
      password = !string.IsNullOrWhiteSpace(password) ? Encryption.EncryptText(password) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.Include(c => c.Users).FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel != null && (channel.Users.Count == 0 || channel.Users.Any(u => u.Permissions >= ChannelPermissions.Write && u.Username == username && u.Password == password))) {
          Tag? tag = await dbContext.Tags!.FirstOrDefaultAsync(t => t.Id == tagId && t.ChannelId == channel.Id);
          if (tag != null) {
            Relation? relation = await dbContext.Relations!.Include(r => r.Tags).FirstOrDefaultAsync(r => r.ChannelId == channel.Id && r.Type == type && r.Name == name);
            if (relation == null) {
              DateTime utcNow = DateTime.UtcNow;
              relation = new() { ChannelId = channel.Id, Type = type, Name = name, DateCreated = utcNow, Timestamp = utcNow, Value = RelationValue.NotAssigned };
              dbContext.Relations!.Add(relation);
              await dbContext.SaveChangesAsync();
              relation = await dbContext.Relations!.Include(r => r.Tags).FirstOrDefaultAsync(r => r.ChannelId == channel.Id && r.Type == type && r.Name == name);
            }
            if (relation != null && !relation.Tags.Any(t => t.Id == tagId)) {
              relation.Tags.Add(tag);
              rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
              if (!rtnVal.Success) {
                rtnVal.Info = localizer["No entries written"];
              } else {
                notifier.Notify(channelName, new RelationChangedNotification { ChannelName = channelName, Relation = new RelationInfo { Type = type, Name = name, Relation = relation.Value, Comment = relation.Comment ?? string.Empty, Timestamp = DateTime.UtcNow.ToTimestamp(), TagId = tagId, TagAdded = true } });
                PushRelationWebhook(new() { WebhookUrl = channel.DiscordWebhookUrl, Username = username, Type = type, Name = name, OldRelation = relation.Value, OldComment = relation.Comment ?? string.Empty, NewRelation = relation.Value, NewComment = relation.Comment ?? string.Empty, TagValue = tag.Value, TagAdded = true }, handleInfo);
              }
            } else {
              rtnVal.Success = true;
            }
          } else {
            rtnVal.Info = localizer["Tag not found"];
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

  public async Task<SuccessReply> RemoveRelationTag(string channelName, string username, string password, RelationType type, string name, int tagId, HandleInfo? handleInfo = null) {
    SuccessReply rtnVal = new();
    if (!string.IsNullOrWhiteSpace(channelName) && !string.IsNullOrWhiteSpace(name)) {
      channelName = channelName.Trim();
      name = name.Trim();
      username = username?.Trim() ?? string.Empty;
      password = !string.IsNullOrWhiteSpace(password) ? Encryption.EncryptText(password) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.Include(c => c.Users).FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel != null && (channel.Users.Count == 0 || channel.Users.Any(u => u.Permissions >= ChannelPermissions.Write && u.Username == username && u.Password == password))) {
          Relation? relation = await dbContext.Relations!.Include(r => r.Tags).FirstOrDefaultAsync(r => r.ChannelId == channel.Id && r.Type == type && r.Name == name);
          if (relation != null) {
            Tag? tag = relation.Tags.FirstOrDefault(t => t.Id == tagId);
            if (tag != null) {
              relation.Tags.Remove(tag);
              rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
              if (!rtnVal.Success) {
                rtnVal.Info = localizer["No entries written"];
              } else {
                notifier.Notify(channelName, new RelationChangedNotification { ChannelName = channelName, Relation = new RelationInfo { Type = type, Name = name, Relation = relation.Value, Comment = relation.Comment ?? string.Empty, Timestamp = DateTime.UtcNow.ToTimestamp(), TagId = tagId, TagAdded = false } });
                PushRelationWebhook(new() { WebhookUrl = channel.DiscordWebhookUrl, Username = username, Type = type, Name = name, OldRelation = relation.Value, OldComment = relation.Comment ?? string.Empty, NewRelation = relation.Value, NewComment = relation.Comment ?? string.Empty, TagValue = tag.Value, TagAdded = false }, handleInfo);
              }
            } else {
              rtnVal.Success = true;
            }
          } else {
            rtnVal.Success = true;
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
  #endregion

  #region Users
  public async Task<List<User>> GetUsers(string channelName, string adminPassword) {
    List<User> rtnVal = [];
    if (!string.IsNullOrWhiteSpace(channelName)) {
      channelName = channelName.Trim();
      adminPassword = !string.IsNullOrWhiteSpace(adminPassword) ? Encryption.EncryptText(adminPassword) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.AsNoTracking().FirstOrDefaultAsync(c => c.Name == channelName && c.AdminPassword == adminPassword);
        if (channel != null) {
          rtnVal = await dbContext.Users!.AsNoTracking().Where(u => u.ChannelId == channel.Id).OrderBy(u => u.Username).ToListAsync();
        }
      } catch { }
    }
    return rtnVal;
  }

  public async Task<SuccessReply> AddUser(string channelName, string adminPassword, string username, string password, ChannelPermissions permissions) {
    SuccessReply rtnVal = new();
    if (!string.IsNullOrWhiteSpace(channelName) && !string.IsNullOrWhiteSpace(username)) {
      channelName = channelName.Trim();
      username = username.Trim();
      adminPassword = !string.IsNullOrWhiteSpace(adminPassword) ? Encryption.EncryptText(adminPassword) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.FirstOrDefaultAsync(c => c.Name == channelName && c.AdminPassword == adminPassword);
        if (channel != null) {
          if (!await dbContext.Users!.AnyAsync(u => u.ChannelId == channel.Id && u.Username == username)) {
            dbContext.Add(new User() {
              ChannelId = channel.Id,
              Username = username,
              DecryptedPassword = password,
              Permissions = permissions
            });
            rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
            if (!rtnVal.Success) {
              rtnVal.Info = localizer["No entries written"];
            }
          } else {
            rtnVal.Info = localizer["User already exists"];
          }
        } else {
          rtnVal.Info = localizer["Access denied"];
        }
      } catch (Exception ex) {
        rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
      }
    } else {
      rtnVal.Info = localizer["No username was given"];
    }
    return rtnVal;
  }

  public async Task<SuccessReply> UpdateUser(string channelName, string adminPassword, int userId, string? newPassword, ChannelPermissions permissions) {
    SuccessReply rtnVal = new();
    if (!string.IsNullOrWhiteSpace(channelName)) {
      channelName = channelName.Trim();
      adminPassword = !string.IsNullOrWhiteSpace(adminPassword) ? Encryption.EncryptText(adminPassword) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.FirstOrDefaultAsync(c => c.Name == channelName && c.AdminPassword == adminPassword);
        if (channel != null) {
          User? user = await dbContext.Users!.FirstOrDefaultAsync(u => u.Id == userId && u.ChannelId == channel.Id);
          if (user != null) {
            if (!string.IsNullOrWhiteSpace(newPassword)) {
              user.DecryptedPassword = newPassword;
            }
            user.Permissions = permissions;
            dbContext.Update(user);
            rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
            if (!rtnVal.Success) {
              rtnVal.Info = localizer["No entries written"];
            }
          } else {
            rtnVal.Info = localizer["User not found"];
          }
        } else {
          rtnVal.Info = localizer["Access denied"];
        }
      } catch (Exception ex) {
        rtnVal.Info = $"{localizer["Exception"]}: {ex.Message}, {localizer["Inner Exception"]}: {ex.InnerException?.Message ?? localizer["Empty"]}";
      }
    } else {
      rtnVal.Info = localizer["No channel name was given"];
    }
    return rtnVal;
  }

  public async Task<SuccessReply> RemoveUser(string channelName, string adminPassword, int userId) {
    SuccessReply rtnVal = new();
    if (!string.IsNullOrWhiteSpace(channelName)) {
      channelName = channelName.Trim();
      adminPassword = !string.IsNullOrWhiteSpace(adminPassword) ? Encryption.EncryptText(adminPassword) : string.Empty;
      try {
        Channel? channel = await dbContext.Channels!.FirstOrDefaultAsync(c => c.Name == channelName && c.AdminPassword == adminPassword);
        if (channel != null) {
          User? user = await dbContext.Users!.FirstOrDefaultAsync(u => u.Id == userId && u.ChannelId == channel.Id);
          if (user != null) {
            dbContext.Remove(user);
            rtnVal.Success = await dbContext.SaveChangesAsync() > 0;
            if (!rtnVal.Success) {
              rtnVal.Info = localizer["No entries written"];
            }
          } else {
            rtnVal.Info = localizer["User not found"];
          }
        } else {
          rtnVal.Info = localizer["Access denied"];
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
    if (webhookRelationInfo.OldRelation != webhookRelationInfo.NewRelation || webhookRelationInfo.OldComment != webhookRelationInfo.NewComment || webhookRelationInfo.TagAdded.HasValue) {
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
      if (webhookRelationInfo.TagAdded.HasValue && !string.IsNullOrWhiteSpace(webhookRelationInfo.TagValue)) {
        fields.Add(new() {
          name = "TAG",
          value = webhookRelationInfo.TagAdded.Value ? webhookRelationInfo.TagValue : $"~~{webhookRelationInfo.TagValue}~~"
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
          fields = fields,
          footer = new() {
            text = !string.IsNullOrWhiteSpace(webhookRelationInfo.Username) ? $"Benutzer: {webhookRelationInfo.Username}" : null
          }
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
