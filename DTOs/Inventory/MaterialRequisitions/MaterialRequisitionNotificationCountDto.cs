
namespace inventory_api.DTOs.Inventory.MaterialRequisitions
{
    public class MaterialRequisitionNotificationCountDto
    {
        public int PendingAllocation { get; set; }

        public int PendingStaffApproval { get; set; }

        public int PendingHeadApproval { get; set; }

        public int TotalPending =>
            PendingAllocation +
            PendingStaffApproval +
            PendingHeadApproval;
    }
}
