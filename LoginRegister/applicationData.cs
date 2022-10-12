namespace ApiEndPoints
{
    public class applicationData
    {
        public string name { get; set; }
        public Guid guid { get; set; }
        public string createdBy { get; set; }
        public string category { get; set; }
        public bool reward { get; set; }
        public float? stake { get; set; }
        public bool? finished { get; set; }
        public string? description { get; set; }

    }
}
