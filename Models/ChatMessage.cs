namespace AiChatbotApp.Models;

public class ChatMessage
{
    public int Id { get; set; }

    public string UserMessage { get; set; } = string.Empty;

    public string AiResponse { get; set; } = string.Empty;

    public string? UploadedImagePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}