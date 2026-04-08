using SCHQ_Protos;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace SCHQ_Blazor.Services;

public class RelationChangedNotification {
  public required string ChannelName { get; init; }
  public RelationInfo? Relation { get; init; }
}

public class ChannelRelationsNotifier {
  private readonly ConcurrentDictionary<string, List<Channel<RelationChangedNotification>>> _subscribers = new(StringComparer.OrdinalIgnoreCase);
  private readonly Lock _lock = new();

  public ChannelReader<RelationChangedNotification> Subscribe(string channelName) {
    Channel<RelationChangedNotification> ch = Channel.CreateUnbounded<RelationChangedNotification>();
    lock (_lock) {
      _subscribers.AddOrUpdate(channelName, _ => [ch], (_, list) => { list.Add(ch); return list; });
    }
    return ch.Reader;
  }

  public void Unsubscribe(string channelName, ChannelReader<RelationChangedNotification> reader) {
    lock (_lock) {
      if (_subscribers.TryGetValue(channelName, out List<Channel<RelationChangedNotification>>? list)) {
        list.RemoveAll(c => c.Reader == reader);
      }
    }
  }

  public void Notify(string channelName, RelationChangedNotification notification) {
    List<Channel<RelationChangedNotification>>? snapshot;
    lock (_lock) {
      snapshot = _subscribers.TryGetValue(channelName, out List<Channel<RelationChangedNotification>>? list) ? [.. list] : null;
    }
    if (snapshot != null) {
      foreach (Channel<RelationChangedNotification> ch in snapshot) {
        ch.Writer.TryWrite(notification);
      }
    }
  }
}
