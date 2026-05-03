using System.Collections.Concurrent;

namespace EMenu.Web.Services
{
    public class CheckoutRequestTracker
    {
        private readonly ConcurrentDictionary<int, CheckoutRequestState> _requestsByTableId = new();

        public CheckoutRequestState Upsert(int sessionId, int tableId, string tableName)
        {
            var normalizedTableName = string.IsNullOrWhiteSpace(tableName)
                ? $"Table {tableId}"
                : tableName.Trim();

            return _requestsByTableId.AddOrUpdate(
                tableId,
                _ => CreateState(sessionId, tableId, normalizedTableName),
                (_, existing) => new CheckoutRequestState
                {
                    SessionId = sessionId,
                    TableId = tableId,
                    TableName = normalizedTableName,
                    RequestedAt = existing.RequestedAt
                });
        }

        public bool HasRequestForSession(int sessionId)
        {
            return _requestsByTableId.Values.Any(x => x.SessionId == sessionId);
        }

        public IReadOnlyCollection<CheckoutRequestState> GetAll()
        {
            return _requestsByTableId.Values
                .OrderBy(x => x.TableId)
                .ToList();
        }

        public CheckoutRequestState? ClearByTable(int tableId)
        {
            return _requestsByTableId.TryRemove(tableId, out var removed)
                ? removed
                : null;
        }

        private static CheckoutRequestState CreateState(int sessionId, int tableId, string tableName)
        {
            return new CheckoutRequestState
            {
                SessionId = sessionId,
                TableId = tableId,
                TableName = tableName,
                RequestedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public class CheckoutRequestState
    {
        public int SessionId { get; set; }

        public int TableId { get; set; }

        public string TableName { get; set; } = string.Empty;

        public DateTimeOffset RequestedAt { get; set; }
    }
}
