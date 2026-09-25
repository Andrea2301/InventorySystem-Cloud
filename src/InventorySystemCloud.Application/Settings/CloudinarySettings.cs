namespace InventorySystemCloud.Application.Settings
{
    public class CloudinarySettings
    {
        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string ProductFolder { get; set; } = "inventory/products";
        public string AvatarFolder { get; set; } = "inventory/avatars";
    }
}
