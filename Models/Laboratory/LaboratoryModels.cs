
namespace inventory_api.Models.Laboratory
{
    public class LaboratoryCategory
    {
        public long category_id { get; set; }
        public string category_name { get; set; } = "";
        public bool is_active { get; set; } = true;
    }

    public class LaboratoryBrand
    {
        public long brand_id { get; set; }
        public string brand_name { get; set; } = "";
        public bool is_active { get; set; } = true;
    }

    public class LaboratoryLocation
    {
        public long location_id { get; set; }
        public string location_name { get; set; } = "";
        public string? description { get; set; }
        public bool is_active { get; set; } = true;
    }

    public class LaboratoryItem
    {
        public long item_id { get; set; }
        public string item_code { get; set; } = "";
        public string item_description { get; set; } = "";
        public long category_id { get; set; }
        public long? brand_id { get; set; }
        public string uom { get; set; } = "Unit";
        public decimal minimum_stock { get; set; }
        public string tracking_type { get; set; } = "NONE";
        public bool is_active { get; set; } = true;
        public DateTime created_at { get; set; }
    }

    public class LaboratoryInventory
    {
        public long inventory_id { get; set; }
        public long item_id { get; set; }
        public long location_id { get; set; }
        public string? serial_lot_no { get; set; }
        public decimal quantity { get; set; }
        public string item_status { get; set; } = "Functional";
        public DateTime? date_acquired { get; set; }
        public DateTime? expiry_date { get; set; }
        public string? supplier_name { get; set; }
        public string? remarks { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class LaboratoryInventoryTransaction
    {
        public long transaction_id { get; set; }
        public long inventory_id { get; set; }
        public long item_id { get; set; }
        public long location_id { get; set; }
        public string transaction_type { get; set; } = "";
        public string reference_type { get; set; } = "";
        public string reference_no { get; set; } = "";
        public decimal quantity { get; set; }
        public decimal previous_quantity { get; set; }
        public decimal new_quantity { get; set; }
        public string? reason { get; set; }
        public string? remarks { get; set; }
        public string performed_by { get; set; } = "";
        public DateTime created_at { get; set; }
    }
}
