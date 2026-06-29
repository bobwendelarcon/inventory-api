namespace inventory_api.DTOs
{
    public class PartnerListDto
    {
        public string partner_id { get; set; } = "";
        public string partner_name { get; set; } = "";
        public string address { get; set; } = "";
        public string contact_no { get; set; } = "";
        public string partner_type { get; set; } = "";
        public string region { get; set; } = "";
        public string agent_id { get; set; } = "";
        public string agent_name { get; set; } = "";
        public bool is_deleted { get; set; }
        public string created_at { get; set; } = "";
        public string updated_at { get; set; } = "";
    }

    public class PagedResultDto<T>
    {
        public int page { get; set; }
        public int pageSize { get; set; }
        public int totalRecords { get; set; }
        public int totalPages { get; set; }
        public List<T> data { get; set; } = new();
    }
}