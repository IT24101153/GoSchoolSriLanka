using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoSchool.Api.Controllers;

// shortcut: files land on this server's disk under wwwroot/uploads; move to object storage before running more than one instance
[ApiController]
[Route("api/uploads")]
[Authorize]
public class UploadsController(IWebHostEnvironment env) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".pdf"))
            return BadRequest(new { message = "Only jpg, png, webp or pdf files are allowed" });

        var dir = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{ext}";
        await using (var stream = System.IO.File.Create(Path.Combine(dir, name)))
        {
            await file.CopyToAsync(stream);
        }
        return Ok(new { url = $"{Request.Scheme}://{Request.Host}/uploads/{name}" });
    }
}
