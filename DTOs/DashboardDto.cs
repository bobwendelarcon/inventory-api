namespace inventory_api.DTOs
{
    public class DashboardDto
    {
        public int DailyOrders { get; set; }
        public int ReadyForChecklist { get; set; }
        public int ChecklistQueue { get; set; }
        public int ReleasedToday { get; set; }
        public int PartialDispatch { get; set; }
        public int LowStock { get; set; }
        public int CompletedOrders { get; set; }

        public List<DashboardChecklistDto> Checklist { get; set; } = new();
        public List<DashboardPartialOrderDto> PartialOrders { get; set; } = new();
        public List<DashboardInventoryAlertDto> InventoryAlerts { get; set; } = new();
        public List<DashboardTransactionDto> RecentTransactions { get; set; } = new();
        public List<DashboardReturnDto> RecentReturns { get; set; } = new();
    }
}
