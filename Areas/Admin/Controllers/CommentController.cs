using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Area("Admin")]
[Authorize("Admin")]
public class CommentController : Controller
{
    private readonly AppDataContext _context;

    public CommentController(AppDataContext context)
    {
        _context = context;
    }

    [HttpPost("Reply")]
    public async Task<IActionResult> Reply(long commentId, string content)
    {
        var adminId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var admin = await _context.Admins.FindAsync(adminId);

        var parentComment = await _context.Comments
            .Include(c => c.News)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (parentComment == null) return NotFound();

        var reply = new Comment
        {
            Content = content,
            NewsId = parentComment.NewsId,
            ParentId = commentId,
            AdminId = adminId,
            Admin = admin, 
            IsAdminReply = true, 
            CreatedAt = DateTime.Now
        };

        _context.Comments.Add(reply);
        await _context.SaveChangesAsync();

        return RedirectToAction("NewsComments", "ManageNews", new { id = parentComment.NewsId });
    }
}