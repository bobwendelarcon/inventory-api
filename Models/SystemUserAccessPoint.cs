namespace inventory_api.Models
{
    public class SystemUserAccessPoint
    {
        public int user_access_id { get; set; }

        public string user_id { get; set; } =
            string.Empty;

        public int access_point_id { get; set; }

        public SystemAccessPoint? AccessPoint
        {
            get;
            set;
        }
    }
}