namespace InventorySystemCloud.Application.DTOs.Images
{
    public class ImageUploadResult
    {
        public bool Success { get; set; }
        public string? Url { get; set; }
        public string? PublicId { get; set; }
        public string? ErrorMessage { get; set; }

        public static ImageUploadResult Succeeded(string url, string publicId) =>
            new() { Success = true, Url = url, PublicId = publicId };

        public static ImageUploadResult Failed(string errorMessage) =>
            new() { Success = false, ErrorMessage = errorMessage };
    }
}
