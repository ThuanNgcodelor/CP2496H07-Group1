using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CP2496H07Group1.Controllers
{
    [Authorize]
    public class CommentController : Controller
    {
        private readonly AppDataContext _context;

        public CommentController(AppDataContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(long NewsId, string Content)
        {
            try
            {
                var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var comment = new Comment
                {
                    NewsId = NewsId,
                    Content = Content,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                return RedirectToAction("NewsDetail", "News", new { id = NewsId });
            }
            catch (Exception)
            {
                return RedirectToAction("NewsDetail", "News", new { id = NewsId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(long NewsId, long ParentId, string Content)
        {
            try
            {
                var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // Prevent self-reply
                var parentComment = await _context.Comments.FindAsync(ParentId);
                if (parentComment != null && parentComment.UserId == userId)
                {
                    TempData["ErrorMessage"] = "You cannot reply to your own comments";
                    return RedirectToAction("NewsDetail", "News", new { id = NewsId });
                }

                var reply = new Comment
                {
                    NewsId = NewsId,
                    ParentId = ParentId,
                    Content = Content,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };

                _context.Comments.Add(reply);
                await _context.SaveChangesAsync();

                return RedirectToAction("NewsDetail", "News", new { id = NewsId });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while sending feedback.";
                return RedirectToAction("NewsDetail", "News", new { id = NewsId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var comment = await _context.Comments
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (comment.UserId != currentUserId)
            {
                return Forbid();
            }

            _context.Comments.RemoveRange(comment.Replies);
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("NewsDetail", "News", new { id = comment.NewsId });
        }
    }
}