namespace inventory_api.Models
{
    public class SystemAccessPoint
    {
        public int access_point_id { get; set; }

        public string access_code { get; set; } =
            string.Empty;

        public string access_name { get; set; } =
            string.Empty;

        public string module_name { get; set; } =
            string.Empty;

        public int sort_order { get; set; }

        public bool is_active { get; set; }
    }
}