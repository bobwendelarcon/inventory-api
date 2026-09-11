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


        // ============================================================
        // MATERIAL RECEIVING FORM
        // ============================================================

       

        

        public string? VerifiedBy { get; set; }


        // ============================================================
        // GENERAL
        // ============================================================

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }


        // ============================================================
        // MATERIALS / LOTS
        // ============================================================

        public List<IncomingReceivingLineDto> Lines { get; set; }
            = new();


        // ============================================================
        // RMW RECEIVING CHECKLIST
        // ============================================================

        public IncomingReceivingInspectionDto Inspection { get; set; }
            = new();
    }
}