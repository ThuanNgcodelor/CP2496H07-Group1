﻿using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

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
                string[] bannedWords = new string[]
                {
                    "địt", "lồn", "cặc", "bú lol", "bú cu", "đụ", "bú m*", "bú zú", "xếp hình", "hiếp dâm",
                    "dâm đãng", "dâm loạn", "thằng chó", "con đĩ", "con mẹ mày", "đồ khốn", "thằng khốn", "má mày",
                    "thằng ngu", "con ngu", "chó má", "bạo lực", "giết người", "máu me", "vkl", "vkvl", "md", "vl", "vl qlq", "qlq",
                    "fuck", "shit", "bitch", "asshole", "bastard", "slut", "dick", "pussy", "fucker",
                    "motherfucker", "rape", "kill", "violence", "bloody", "suck my dick", "blowjob",
                    "porn", "porno", "sex", "orgy", "nude", "cum", "ejaculate"
                };

                bool containsBannedWord = bannedWords.Any(word =>
                    Regex.IsMatch(Content, @"\b" + Regex.Escape(word) + @"\b", RegexOptions.IgnoreCase));

                if (containsBannedWord)
                {
                    return BadRequest("Comment contains invalid words.");
                }

                var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // Limit to 2 top-level comments per user per news
                int userCommentCount = await _context.Comments
                    .CountAsync(c => c.NewsId == NewsId && c.UserId == userId && c.ParentId == null);
                if (userCommentCount >= 2)
                {
                    return BadRequest("You have reached the maximum number of comments for this news.");
                }

                var comment = new Comment
                {
                    NewsId = NewsId,
                    Content = Content,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch
            {
                return StatusCode(500, "An error occurred while submitting the comment.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(long NewsId, long ParentId, string Content)
        {
            try
            {
                string[] bannedWords = new string[]
                {
                    "địt", "lồn", "cặc", "bú lol", "bú cu", "đụ", "bú m*", "bú zú", "xếp hình", "hiếp dâm",
                    "dâm đãng", "dâm loạn", "thằng chó", "con đĩ", "con mẹ mày", "má mày", "đồ khốn", "thằng khốn",
                    "thằng ngu", "con ngu", "chó má", "bạo lực", "giết người", "máu me","vkl", "vkvl", "md", "vl", "vl qlq", "qlq",
                    "fuck", "shit", "bitch", "asshole", "bastard", "slut", "dick", "pussy", "fucker",
                    "motherfucker", "rape", "kill", "violence", "bloody", "suck my dick", "blowjob",
                    "porn", "porno", "sex", "orgy", "nude", "cum", "ejaculate"
                };

                bool containsBannedWord = bannedWords.Any(word =>
                    Regex.IsMatch(Content, @"\b" + Regex.Escape(word) + @"\b", RegexOptions.IgnoreCase));

                if (containsBannedWord)
                {
                    return BadRequest("Comment contains invalid words.");
                }

                var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var parentComment = await _context.Comments.FindAsync(ParentId);
                if (parentComment == null)
                {
                    return NotFound("Parent comment not found.");
                }

                if (parentComment.UserId == userId)
                {
                    return BadRequest("You cannot reply to your own comments.");
                }

                // Check if user already replied to this comment
                bool hasReplied = await _context.Comments.AnyAsync(c =>
                    c.ParentId == ParentId && c.UserId == userId);

                if (hasReplied)
                {
                    return BadRequest("You have already replied to this comment.");
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

                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while sending the response.");
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