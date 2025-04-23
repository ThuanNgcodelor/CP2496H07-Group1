using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CP2496H07Group1.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDataContext _context;

        public ChatHub(AppDataContext context)
        {
            _context = context;
        }

        // Phương thức gửi tin nhắn
        public async Task SendMessage(string message)
        {
            var adminIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminIdString) || !long.TryParse(adminIdString, out var adminId))
            {
                return;
            }

            var admin = await _context.Admins.FindAsync(adminId);
            if (admin == null) return;

            var chatMessage = new ChatMessage
            {
                AdminId = adminId,
                Admin = admin,
                Message = message,
                Timestamp = DateTime.Now
            };

            // Lưu tin nhắn vào cơ sở dữ liệu
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Phát tin nhắn tới tất cả các client (Admin)
            await Clients.All.SendAsync("ReceiveMessage", admin.Username, message);
        }

        // Phương thức tải tất cả tin nhắn
        public async Task LoadMessages()
        {
            // Lấy tin nhắn từ cơ sở dữ liệu
            var messages = await _context.ChatMessages
                .Include(m => m.Admin)  // Lấy thông tin Admin nếu có
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    message = m.Message,
                    timestamp = m.Timestamp.ToString("dd/MM/yyyy HH:mm"),
                    admin = m.Admin.Username, // Chỉ lấy thông tin Admin
                    isAdmin = m.Admin != null
                })
                .ToListAsync();

            // Trả về dữ liệu dưới dạng JSON
            await Clients.Caller.SendAsync("LoadMessages", messages);
        }
    }
}
