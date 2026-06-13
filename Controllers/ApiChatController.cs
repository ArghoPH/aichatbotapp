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
                x.Id,
                x.UserMessage,
                x.AiResponse,
                x.UploadedImagePath,
                x.CreatedAt
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

                // Image analysis still uses Gemini Vision directly
                aiResponse = await _geminiService.AnalyzeImageAsync(
                    imagePrompt,
                    fullImagePath);
            }
            else
            {
                var promptWithMemory = BuildMemoryPrompt(message!);

                // Text chat now uses fallback router:
                // Gemini -> Groq -> OpenRouter
                var routerResult = await _aiRouter.GenerateTextAsync(promptWithMemory);

                aiResponse = $"[{routerResult.ProviderName}]\n{routerResult.Text}";
            }
        }
        catch (Exception ex)
        {
            aiResponse = GetFriendlyAiError(ex);
        }

        var chatMessage = new ChatMessage
        {
            UserMessage = message ?? "[Image uploaded]",
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
            chatMessage.CreatedAt
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
            .TakeLast(10)
            .ToList();

        var memoryBuilder = new StringBuilder();

        memoryBuilder.AppendLine("You are a helpful AI assistant.");
        memoryBuilder.AppendLine("Use the previous conversation context only when it is relevant.");
        memoryBuilder.AppendLine("Do not repeat previous API quota, provider failure, or fallback error messages.");
        memoryBuilder.AppendLine("Do not claim that providers are unavailable based on previous conversation history.");
        memoryBuilder.AppendLine("Answer the user's current message clearly and professionally.");
        memoryBuilder.AppendLine();

        foreach (var chat in previousMessages)
        {
            memoryBuilder.AppendLine($"User: {chat.UserMessage}");
            memoryBuilder.AppendLine($"AI: {chat.AiResponse}");
            memoryBuilder.AppendLine();
        }

        memoryBuilder.AppendLine($"Current User Message: {currentMessage}");

        return memoryBuilder.ToString();
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
               text.Contains("provider") && text.Contains("unavailable") ||
               text.Contains("try again later") ||
               text.Contains("fallback");
    }

    private string GetFriendlyAiError(Exception ex)
    {
        if (ex.Message.Contains("RESOURCE_EXHAUSTED") || ex.Message.Contains("quota"))
        {
            return "Gemini image analysis quota limit reached. Please wait a little and try again later.";
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