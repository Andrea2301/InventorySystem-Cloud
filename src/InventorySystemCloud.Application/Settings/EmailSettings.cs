namespace InventorySystemCloud.Application.Settings
{
    public class EmailSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = "onboarding@resend.dev";
        public string SenderName { get; set; } = "InventorySystem Cloud";
        public bool IsSimulationMode { get; set; } = true;
    }
}
