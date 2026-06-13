using AiChatbotApp.Data;
using AiChatbotApp.Models;
using AiChatbotApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AiChatbotApp.Controllers;

[ApiController]
[Route("api/chat")]
public class ApiChatController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly GeminiService _geminiService;
    private readonly AiProviderRouterService _aiRouter;
    private readonly IWebHostEnvironment _environment;

    public ApiChatController(
        ApplicationDbContext context,
        GeminiService geminiService,
        AiProviderRouterService aiRouter,
        IWebHostEnvironment environment)
    {
        _context = context;
        _geminiService = geminiService;
        _aiRouter = aiRouter;
        _environment = environment;
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var chats = await _context.ChatMessages
            .OrderByDescending(x => x.CreatedAt)
            .Take(30)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id,
                userMessage = x.UserMessage,
                aiResponse = x.AiResponse,
                uploadedImagePath = x.UploadedImagePath,
                createdAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(chats);
    }

    [HttpGet("providers/status")]
    public IActionResult GetProviderStatus()
    {
        var statuses = _aiRouter.GetProviderStatuses();

        return Ok(statuses);
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearChatHistory()
    {
        var messages = await _context.ChatMessages.ToListAsync();

        foreach (var message in messages)
        {
            if (!string.IsNullOrWhiteSpace(message.UploadedImagePath))
            {
                var fullImagePath = Path.Combine(
                    _environment.WebRootPath,
                    message.UploadedImagePath
                        .TrimStart('/')
                        .Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (System.IO.File.Exists(fullImagePath))
                {
                    System.IO.File.Delete(fullImagePath);
                }
            }
        }

        _context.ChatMessages.RemoveRange(messages);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Chat history cleared successfully."
        });
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(
        [FromForm] string? message,
        [FromForm] IFormFile? imageFile)
    {
        if (string.IsNullOrWhiteSpace(message) && imageFile == null)
        {
            return BadRequest(new
            {
                error = "Please write a message or upload an image."
            });
        }

        string? uploadedImagePath = null;
        string? providerName = null;
        string aiResponse;

        try
        {
            if (imageFile != null)
            {
                uploadedImagePath = await SaveUploadedImageAsync(imageFile);

                var fullImagePath = Path.Combine(
                    _environment.WebRootPath,
                    uploadedImagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

                var imagePrompt = BuildMemoryPrompt(
                    message ?? "Analyze this image clearly.");

                providerName = "Gemini Vision";

                aiResponse = await _geminiService.AnalyzeImageAsync(
                    imagePrompt,
                    fullImagePath);
            }
            else
            {
                var promptWithMemory = BuildMemoryPrompt(message!);

                var routerResult = await _aiRouter.GenerateTextAsync(promptWithMemory);

                providerName = routerResult.ProviderName;
                aiResponse = routerResult.Text;
            }
        }
        catch (Exception ex)
        {
            aiResponse = GetFriendlyAiError(ex);
        }

        var chatMessage = new ChatMessage
        {
            UserMessage = string.IsNullOrWhiteSpace(message)
         ? "[Image uploaded]"
         : message,

            AiResponse = aiResponse,
            UploadedImagePath = uploadedImagePath,
            CreatedAt = DateTime.Now
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            chatMessage.Id,
            chatMessage.UserMessage,
            chatMessage.AiResponse,
            chatMessage.UploadedImagePath,
            chatMessage.CreatedAt,
            ProviderName = providerName
        });
    }

    private string BuildMemoryPrompt(string currentMessage)
    {
        var previousMessages = _context.ChatMessages
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .OrderBy(x => x.CreatedAt)
            .ToList()
            .Where(x => !IsSystemFailureMessage(x.AiResponse))
            .TakeLast(8)
            .ToList();

        var memoryBuilder = new StringBuilder();

        memoryBuilder.AppendLine("You are a helpful AI assistant for this web application.");
        memoryBuilder.AppendLine("Reply in the same language as the current user message.");
        memoryBuilder.AppendLine("Be clear, direct, and concise.");
        memoryBuilder.AppendLine("Do not mention AI provider names such as Gemini, Groq, OpenRouter, Mistral, Cohere, HuggingFace, or Cerebras.");
        memoryBuilder.AppendLine("Use previous conversation context only when it is relevant.");
        memoryBuilder.AppendLine("Do not repeat previous API quota, provider failure, or fallback error messages.");
        memoryBuilder.AppendLine("Do not invent personal information.");
        memoryBuilder.AppendLine("If the user asks for their name and the name is not available in the previous conversation, say that you do not know their name yet and ask them to tell you.");
        memoryBuilder.AppendLine();

        foreach (var chat in previousMessages)
        {
            memoryBuilder.AppendLine($"User: {chat.UserMessage}");
            memoryBuilder.AppendLine($"AI: {RemoveProviderTag(chat.AiResponse)}");
            memoryBuilder.AppendLine();
        }

        memoryBuilder.AppendLine($"Current User Message: {currentMessage}");

        return memoryBuilder.ToString();
    }

    private string RemoveProviderTag(string aiResponse)
    {
        if (string.IsNullOrWhiteSpace(aiResponse))
        {
            return string.Empty;
        }

        var text = aiResponse.Trim();

        if (text.StartsWith("[") && text.Contains("]"))
        {
            var closingIndex = text.IndexOf("]");

            if (closingIndex >= 0 && closingIndex < 30)
            {
                return text[(closingIndex + 1)..].Trim();
            }
        }

        return text;
    }

    private bool IsSystemFailureMessage(string aiResponse)
    {
        if (string.IsNullOrWhiteSpace(aiResponse))
        {
            return false;
        }

        var text = aiResponse.ToLower();

        return text.Contains("quota limit reached") ||
               text.Contains("quota-limited") ||
               text.Contains("temporarily unavailable") ||
               text.Contains("resource_exhausted") ||
               text.Contains("all free ai providers") ||
               (text.Contains("provider") && text.Contains("unavailable")) ||
               text.Contains("try again later") ||
               text.Contains("fallback");
    }

    private string GetFriendlyAiError(Exception ex)
    {
        if (ex.Message.Contains("RESOURCE_EXHAUSTED") || ex.Message.Contains("quota"))
        {
            return "AI quota limit reached. Please wait a little and try again later.";
        }

        if (ex.Message.Contains("UNAVAILABLE") || ex.Message.Contains("503"))
        {
            return "AI service is busy right now. Please try again after a moment.";
        }

        return "AI service is temporarily unavailable. Please try again later.";
    }

    private async Task<string> SaveUploadedImageAsync(IFormFile imageFile)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        var extension = Path.GetExtension(imageFile.FileName).ToLower();

        if (!allowedExtensions.Contains(extension))
        {
            throw new Exception("Only JPG, JPEG, PNG, and WEBP images are allowed.");
        }

        var fileName = $"upload_{Guid.NewGuid()}{extension}";
        var folderPath = Path.Combine(_environment.WebRootPath, "uploads");

        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await imageFile.CopyToAsync(stream);

        return $"/uploads/{fileName}";
    }
}