namespace PmEngine.Telegram.Entities
{
    public class MessageQueueEntity
    {
        public long Id { get; set; }
        public long? ForUserTgId { get; set; }
        public long? ForChatTgId { get; set; }
        public string? Text { get; set; }
        public string? Actions { get; set; }
        public string? Media { get; set; }
        public string? Arguments { get; set; }
        public DateTime? SendedDate { get; set; }
        public string Status { get; set; } = "Waiting";
        public int? MessageId { get; set; }
    }
}