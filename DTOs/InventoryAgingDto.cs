namespace inventory_api.DTOs
{
    public class InventoryAgingDto
    {
        public string product_id { get; set; } = "";
        public string branch_id { get; set; } = "";

        public string product_name { get; set; } = "";
        public string product_description { get; set; } = "";

        public string lot_no { get; set; } = "";
        public string warehouse { get; set; } = "";

        public decimal qty { get; set; }
        public string uom { get; set; } = "";

        public DateTime? date_in { get; set; }
        public DateTime? last_out_date { get; set; }

        public DateTime? manufacturing_date { get; set; }
        public DateTime? expiration_date { get; set; }

        public int days_in_inventory { get; set; }
        public int? days_since_last_out { get; set; }

        public int? days_to_expiry { get; set; }
        public DateTime? last_verified_at { get; set; }

        public string aging_status { get; set; } = "";
        public bool needs_verification { get; set; }
    }
}