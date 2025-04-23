namespace CP2496H07Group1.Models
{
    public class ChatMessage
    {
        public long Id { get; set; }

        public required string Message { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public long AdminId { get; set; }

        public Admin Admin { get; set; }
    }
}
