using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Auth;
using InventorySystemCloud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InventorySystemCloud.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5MB

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var ipAddress = GetIpAddress();
            var result = await _authService.RegisterAsync(request, ipAddress);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var ipAddress = GetIpAddress();
            var result = await _authService.LoginAsync(request, ipAddress);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var ipAddress = GetIpAddress();
            var result = await _authService.RefreshTokenAsync(request, ipAddress);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto request)
        {
            var ipAddress = GetIpAddress();
            var result = await _authService.RevokeTokenAsync(request, ipAddress);
            return StatusCode(result.StatusCode, result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var publicIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(publicIdClaim, out var publicId))
                return Unauthorized();

            var result = await _authService.LogoutAsync(publicId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Uploads and sets the avatar image for the authenticated user.
        /// </summary>
        [Authorize]
        [HttpPost("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var publicIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(publicIdClaim, out var publicId))
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Debe proporcionar un archivo de imagen válido." });

            if (file.Length > MaxFileSizeInBytes)
                return BadRequest(new { success = false, message = "El tamaño máximo permitido para la imagen es de 5 MB." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
                return BadRequest(new { success = false, message = "Formato de archivo no soportado. Formatos válidos: JPG, PNG, WEBP." });

            await using var stream = file.OpenReadStream();
            var result = await _authService.UploadAvatarAsync(publicId, stream, file.FileName);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Deletes the avatar image for the authenticated user.
        /// </summary>
        [Authorize]
        [HttpDelete("avatar")]
        public async Task<IActionResult> DeleteAvatar()
        {
            var publicIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(publicIdClaim, out var publicId))
                return Unauthorized();

            var result = await _authService.DeleteAvatarAsync(publicId);
            return StatusCode(result.StatusCode, result);
        }

        private string? GetIpAddress() =>
            Request.Headers.ContainsKey("X-Forwarded-For")
                ? Request.Headers["X-Forwarded-For"].ToString()
                : HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
