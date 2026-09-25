using System;
using System.IO;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppImageUploadResult = InventorySystemCloud.Application.DTOs.Images.ImageUploadResult;

namespace InventorySystemCloud.Infrastructure.Services
{
    public class CloudinaryImageStorageService : IImageStorageService
    {
        private readonly Cloudinary? _cloudinary;
        private readonly ILogger<CloudinaryImageStorageService> _logger;

        public CloudinaryImageStorageService(
            IOptions<CloudinarySettings> options,
            ILogger<CloudinaryImageStorageService> logger)
        {
            _logger = logger;
            var settings = options.Value;

            if (!string.IsNullOrWhiteSpace(settings.CloudName) &&
                !string.IsNullOrWhiteSpace(settings.ApiKey) &&
                !string.IsNullOrWhiteSpace(settings.ApiSecret))
            {
                var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
                _cloudinary = new Cloudinary(account);
                _cloudinary.Api.Secure = true;
            }
            else
            {
                _logger.LogWarning("Cloudinary is not configured. Image uploads will not be processed.");
            }
        }

        public async Task<AppImageUploadResult> UploadImageAsync(Stream fileStream, string fileName, string folder)
        {
            if (_cloudinary == null)
            {
                _logger.LogError("Cloudinary client is not initialized due to missing configuration.");
                return AppImageUploadResult.Failed("Cloudinary credentials are not configured in appsettings.");
            }

            try
            {
                if (fileStream.Position != 0 && fileStream.CanSeek)
                {
                    fileStream.Position = 0;
                }

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    Folder = folder,
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto"),
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    _logger.LogError("Cloudinary upload failed: {Message}", uploadResult.Error.Message);
                    return AppImageUploadResult.Failed(uploadResult.Error.Message);
                }

                return AppImageUploadResult.Succeeded(
                    uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty,
                    uploadResult.PublicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during Cloudinary image upload: {Message}", ex.Message);
                return AppImageUploadResult.Failed($"Error uploading image: {ex.Message}");
            }
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (_cloudinary == null || string.IsNullOrWhiteSpace(publicId))
                return false;

            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image from Cloudinary: {PublicId}", publicId);
                return false;
            }
        }
    }
}
