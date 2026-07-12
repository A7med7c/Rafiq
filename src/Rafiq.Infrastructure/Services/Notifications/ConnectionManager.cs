using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Rafiq.Infrastructure.Services.Notifications
{
    public interface IConnectionManager
    {
        void AddConnection(string userId, string connectionId);
        void RemoveConnection(string userId, string connectionId);
        IEnumerable<string> GetConnections(string userId);

        /// <summary>Diagnostics: which user ids currently hold connections.</summary>
        IEnumerable<string> GetTrackedUserIds();
    }

    public class ConnectionManager : IConnectionManager
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _connections = new();

        public void AddConnection(string userId, string connectionId)
        {
            _connections.AddOrUpdate(
                userId,
                _ => new HashSet<string> { connectionId },
                (_, set) =>
                {
                    lock (set)
                    {
                        set.Add(connectionId);
                    }
                    return set;
                });
        }

        public void RemoveConnection(string userId, string connectionId)
        {
            if (_connections.TryGetValue(userId, out var set))
            {
                lock (set)
                {
                    set.Remove(connectionId);
                    if (set.Count == 0)
                    {
                        _connections.TryRemove(userId, out _);
                    }
                }
            }
        }

        public IEnumerable<string> GetConnections(string userId)
        {
            if (_connections.TryGetValue(userId, out var set))
            {
                lock (set)
                {
                    return [.. set];
                }
            }
            return [];
        }

        public IEnumerable<string> GetTrackedUserIds() => [.. _connections.Keys];
    }
}
