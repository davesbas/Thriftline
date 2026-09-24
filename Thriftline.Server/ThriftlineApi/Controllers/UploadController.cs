using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThriftlineApi.DTOs.Uploads;
using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] VideoExtensions = { ".mp4", ".webm", ".mov" };
    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const long MaxVideoSizeBytes = 50 * 1024 * 1024; // 50 MB

    public UploadController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost("image")]
    public async Task<ActionResult<UploadImageResponse>> UploadImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("File tidak boleh kosong.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var isVideo = VideoExtensions.Contains(extension);
        var isImage = ImageExtensions.Contains(extension);

        if (!isImage && !isVideo)
        {
            return BadRequest("Format file harus JPG, PNG, WEBP, MP4, WEBM, atau MOV.");
        }

        var maxSize = isVideo ? MaxVideoSizeBytes : MaxImageSizeBytes;
        if (file.Length > maxSize)
        {
            return BadRequest(isVideo ? "Ukuran video maksimal 50MB." : "Ukuran gambar maksimal 5MB.");
        }

        var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new UploadImageResponse
        {
            Url = $"/uploads/{fileName}",
            MediaType = isVideo ? MediaType.Video : MediaType.Image
        });
    }
}
