using GoSchool.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoSchool.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(GoSchoolDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await db.Notifications
            .Where(n => n.UserId == User.UserId())
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new { n.Id, n.Message, n.CreatedAt, n.IsRead })
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == User.UserId());
        if (n is null) return NotFound();
        n.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
