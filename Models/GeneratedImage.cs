namespace AiChatbotApp.Models;

public class GeneratedImage
{
    public int Id { get; set; }

    public string Prompt { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}