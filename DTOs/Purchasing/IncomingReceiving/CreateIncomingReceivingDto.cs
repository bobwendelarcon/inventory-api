using System;
using System.Collections.Generic;

namespace inventory_api.DTOs.Purchasing.IncomingReceiving
{
    public class CreateIncomingReceivingDto
    {
        public int ScheduleId { get; set; }

        public string? BranchId { get; set; }

        public DateTime DeliveryDate { get; set; }

        public string? SiDrNo { get; set; }

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }

        public List<IncomingReceivingLineDto> Lines { get; set; }
            = new();

        public IncomingReceivingInspectionDto Inspection { get; set; }
            = new();
    }
}