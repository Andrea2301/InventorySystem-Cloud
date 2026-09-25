using System.IO;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Images;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IImageStorageService
    {
        Task<ImageUploadResult> UploadImageAsync(Stream fileStream, string fileName, string folder);
        Task<bool> DeleteImageAsync(string publicId);
    }
}
