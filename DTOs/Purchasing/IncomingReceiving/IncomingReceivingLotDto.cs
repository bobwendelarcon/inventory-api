using System;

namespace inventory_api.DTOs.Purchasing.IncomingReceiving
{
    public class IncomingReceivingLotDto
    {
        public int? ManufacturerId { get; set; }

        public string? LotNo { get; set; }

        public DateTime? ManufacturingDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        // Bags / drums / boxes / containers
        public decimal? ItemCount { get; set; }

        // Actual weight stated on Material Receiving Form
        public decimal? Weight { get; set; }

        public string? Remarks { get; set; }
    }
}