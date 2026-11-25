
using inventory_api.Data;
using inventory_api.DTOs.Inventory.MaterialRequisitions;
using inventory_api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Inventory
{
    public class MaterialRequisitionNotificationService
    {
        private readonly AppDbContext _context;

        private readonly UserService _userService;

        private readonly IHubContext<MaterialRequisitionHub>
            _hubContext;

        private readonly ILogger<MaterialRequisitionNotificationService>
            _logger;

        public const string NotificationEvent =
            "ReceiveMrsNotification";

        public MaterialRequisitionNotificationService(
            AppDbContext context,
            UserService userService,
            IHubContext<MaterialRequisitionHub> hubContext,
            ILogger<MaterialRequisitionNotificationService> logger)
        {
            _context = context;
            _userService = userService;
            _hubContext = hubContext;
            _logger = logger;
        }

        // ==========================================
        // GET AUTHORIZED MRS RECIPIENTS
        // ==========================================

        private async Task<List<string>>
            GetAuthorizedRecipientsAsync()
        {
            var activeUserIds = await _context.Users
                .AsNoTracking()
                .Where(x => !x.is_deleted)
                .Select(x => x.user_id)
                .ToListAsync();

            var recipients = new List<string>();

            foreach (var userId in activeUserIds)
            {
                var accessCodes =
                    await _userService
                        .GetUserAccessCodesAsync(userId);

                var canViewMrs = accessCodes.Contains(
                    "INV_MATERIAL_REQUISITION",
                    StringComparer.OrdinalIgnoreCase);

                var canReleaseMrs = accessCodes.Contains(
                    "MRS_RELEASE",
                    StringComparer.OrdinalIgnoreCase);

                if (canViewMrs && canReleaseMrs)
                {
                    recipients.Add(userId);
                }
            }

            return recipients;
        }

        // ==========================================
        // NOTIFY AUTHORIZED MRS USERS
        // ==========================================

        public async Task NotifyWarehouseUsersAsync(
            MaterialRequisitionNotificationDto notification)
        {
            try
            {
                var recipients =
                    await GetAuthorizedRecipientsAsync();

                await SendToUsersAsync(
                    recipients,
                    notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to notify warehouse users for MRS {RequisitionNo}",
                    notification.RequisitionNo);
            }
        }

        // ==========================================
        // SEND SIGNALR NOTIFICATION
        // ==========================================

        private async Task SendToUsersAsync(
            IEnumerable<string> userIds,
            MaterialRequisitionNotificationDto notification)
        {
            var recipients = userIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (recipients.Count == 0)
            {
                _logger.LogInformation(
                    "No notification recipients for MRS {RequisitionNo}",
                    notification.RequisitionNo);

                return;
            }

            await _hubContext.Clients
                .Users(recipients)
                .SendAsync(
                    NotificationEvent,
                    notification);

            _logger.LogInformation(
                "MRS notification dispatched. Type: {NotificationType}, MRS: {RequisitionNo}, Target users: {RecipientCount}",
                notification.NotificationType,
                notification.RequisitionNo,
                recipients.Count);
        }
    }
}
