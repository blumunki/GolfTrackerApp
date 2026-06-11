namespace GolfTrackerApp.Web.Models
{
    public class AiProviderConfig
    {
        public string Name { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
