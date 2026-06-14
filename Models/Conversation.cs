namespace AiChatbotApp.Models;

public class Conversation
{
    public int Id { get; set; }

    public string Title { get; set; } = "New Chat";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ChatMessage> ChatMessages { get; set; } = new();
}
