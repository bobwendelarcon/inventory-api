
namespace inventory_api.DTOs.Inventory.MaterialRequisitions
{
    public class MaterialRequisitionNotificationDto
    {
        public string NotificationId { get; set; } =
            Guid.NewGuid().ToString("N");

        public string NotificationType { get; set; } = "";

        public int RequisitionId { get; set; }

        public string RequisitionNo { get; set; } = "";

        public string BranchId { get; set; } = "";

        public string BranchName { get; set; } = "";

        public string Title { get; set; } = "";

        public string Message { get; set; } = "";

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;
    }
}
