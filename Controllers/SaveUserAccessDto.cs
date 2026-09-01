namespace inventory_api.DTOs
{
    public class SaveUserAccessDto
    {
        public List<int> access_point_ids { get; set; }
            = new List<int>();
    }
}