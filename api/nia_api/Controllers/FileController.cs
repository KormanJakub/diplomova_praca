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
    public async Task<IActionResult> UploadFile([FromForm] NewResource resource)
    {
        try
        {
            var file = resource.Files;
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded!");

            var uploadFileFolder = Path.Combine(_environment.WebRootPath + "/Files/");
            Directory.CreateDirectory(uploadFileFolder);

            using FileStream fileStream = System.IO.File.Create(uploadFileFolder + file.FileName);
            file.CopyToAsync(fileStream);
            fileStream.Flush();

            var newFile = new File()
            {
                Id = Guid.NewGuid(),
                Name = file.FileName,
                Path = "/Files/" + file.FileName,
                CreatedAt = LocalTimeService.LocalTime()
            };
            
            await _files.InsertOneAsync(newFile);

            return Ok(new { id = newFile.Id, path = newFile.Path });
        }
        catch (Exception e)
        {
            return StatusCode(500, "Internal server error: " + e);
        }
    }

    [HttpDelete("removeFile/{id}")]
    public async Task<IActionResult> RemoveFile(Guid id)
    {
        try
        {
            var file = await _files.Find(g => g.Id == id).FirstOrDefaultAsync();

            if (file == null)
                return NotFound("File not found!");
            
            var filePath = Path.Combine(_environment.WebRootPath, file.Path.TrimStart('/'));

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            await _files.DeleteOneAsync(g => g.Id == id);

            return Ok(new { message = "File deleted successfully!" });
        }
        catch (Exception e)
        {
            return StatusCode(500, "Internal server error: " + e);
        }
    }
    
    [HttpDelete("move")]
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
}