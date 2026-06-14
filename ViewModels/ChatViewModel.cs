using AiChatbotApp.Models;
using Microsoft.AspNetCore.Http;

namespace AiChatbotApp.ViewModels;

public class ChatViewModel
{
    public string? Message { get; set; }

    public IFormFile? ImageFile { get; set; }

    public string? ImagePrompt { get; set; }

    public List<ChatMessage> ChatHistory { get; set; } = new();

    public List<GeneratedImage> GeneratedImages { get; set; } = new();
}
