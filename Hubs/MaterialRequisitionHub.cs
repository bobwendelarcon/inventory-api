
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace inventory_api.Hubs
{
    [Authorize]
    public class MaterialRequisitionHub : Hub
    {
        private readonly ILogger<MaterialRequisitionHub> _logger;

        public MaterialRequisitionHub(
            ILogger<MaterialRequisitionHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                Context.Abort();
                return;
            }

            _logger.LogInformation(
                "MRS SignalR connected. User: {UserId}, Connection: {ConnectionId}",
                userId,
                Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(
            Exception? exception)
        {
            var userId = Context.User?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "MRS SignalR disconnected. User: {UserId}, Connection: {ConnectionId}",
                userId,
                Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}
