using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Models;
using nia_api.Services;
using File = nia_api.Models.File;

namespace nia_api.Controllers;

[ApiController]
[Route("file")]
public class FileController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IMongoCollection<File> _files;

    public FileController(NiaDbContext context, IWebHostEnvironment environment)
    {
        _environment = environment;
        _files = context.Files;
    }

    [HttpPost("uploadFile")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
    public async Task<IActionResult> UploadFile([FromForm] NewResource resource)
    {
        try
        {
            var file = resource.Files;
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded!");

            const long maxUploadBytes = 5 * 1024 * 1024;
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName);
            if (file.Length > maxUploadBytes || !allowedExtensions.Contains(extension))
                return BadRequest("Unsupported file type or size.");
            if (!await HasExpectedSignature(file, extension))
                return BadRequest("Unsupported file content.");

            var uploadFileFolder = Path.Combine(_environment.WebRootPath, "Files");
            Directory.CreateDirectory(uploadFileFolder);

            var safeName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            await using (var fileStream = System.IO.File.Create(Path.Combine(uploadFileFolder, safeName)))
            {
                await file.CopyToAsync(fileStream);
            }

            var newFile = new File()
            {
                Id = Guid.NewGuid(),
                Name = safeName,
                Path = "/Files/" + safeName,
                CreatedAt = LocalTimeService.LocalTime()
            };
            
            await _files.InsertOneAsync(newFile);

            return Ok(new { id = newFile.Id, path = newFile.Path });
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpDelete("removeFile/{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
    public async Task<IActionResult> RemoveFile(Guid id)
    {
        try
        {
            var file = await _files.Find(g => g.Id == id).FirstOrDefaultAsync();

            if (file == null)
                return NotFound("File not found!");
            
            var filePath = Path.Combine(_environment.WebRootPath, "Files", Path.GetFileName(file.Path));

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            await _files.DeleteOneAsync(g => g.Id == id);

            return Ok(new { message = "File deleted successfully!" });
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpDelete("move")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
    public async Task<IActionResult> RemoveTags([FromBody] List<File> files)
    {
        if (files == null || !files.Any())
            return BadRequest(new { message = "No files provided for deletion." });
        
        var filesId = files.Select(ds => ds.Id).ToList();

        var result = await _files.DeleteManyAsync(d => filesId.Contains(d.Id));
        
        return Ok(new { message = $"{result.DeletedCount} designs were successfully deleted!"});
    }

    [HttpGet("byId/{id}")]
    public async Task<IActionResult> GetFileById(Guid id)
    {
        return Ok(await _files.Find(g => g.Id == id).FirstOrDefaultAsync());
    }
    
    [HttpGet("getAll")]
    public async Task<IActionResult> GetAllFiles()
    {
        return Ok(await _files.Find(_ => true).ToListAsync());
    }
    
    public record NewResource(IFormFile Files);

    private static async Task<bool> HasExpectedSignature(IFormFile file, string extension)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header.AsMemory());
        if (bytesRead < 12) return false;

        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".webp" => header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                         header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }
}
