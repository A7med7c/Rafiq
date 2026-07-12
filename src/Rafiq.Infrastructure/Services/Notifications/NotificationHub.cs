using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Rafiq.Infrastructure.Services.Notifications
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(IConnectionManager connectionManager, ILogger<NotificationHub> logger)
        {
            _connectionManager = connectionManager;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _connectionManager.AddConnection(userId, Context.ConnectionId);

                _logger.LogInformation(
                    "SignalR connection established for userId='{UserId}', connectionId={ConnectionId}. Registered in ConnectionManager.",
                    userId, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning(
                    "SignalR connection {ConnectionId} authenticated but carries no user identifier claim.",
                    Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _connectionManager.RemoveConnection(userId, Context.ConnectionId);
            }

            _logger.LogInformation(
                "SignalR connection closed for userId={UserId}, connectionId={ConnectionId}. Reason={Reason}",
                userId, Context.ConnectionId, exception?.Message ?? "client disconnect");

            await base.OnDisconnectedAsync(exception);
        }
    }
}
