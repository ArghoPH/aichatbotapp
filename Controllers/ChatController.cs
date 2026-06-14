using AiChatbotApp.Data;
using AiChatbotApp.Models;
using AiChatbotApp.Services;
using AiChatbotApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiChatbotApp.Controllers;

public class ChatController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly GeminiService _geminiService;
    private readonly IWebHostEnvironment _environment;

    public ChatController(
        ApplicationDbContext context,
        GeminiService geminiService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _geminiService = geminiService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var viewModel = new ChatViewModel
        {
            ChatHistory = await _context.ChatMessages
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .ToListAsync(),

            GeneratedImages = await _context.GeneratedImages
                .OrderByDescending(x => x.GeneratedAt)
                .Take(10)
                .ToListAsync()
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(ChatViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Message) && model.ImageFile == null)
        {
            TempData["Error"] = "Please write a message or upload an image.";
            return RedirectToAction(nameof(Index));
        }

        string? uploadedImagePath = null;
        string aiResponse;

        var previousMessages = await _context.ChatMessages
    .OrderByDescending(x => x.CreatedAt)
    .Take(10)
    .OrderBy(x => x.CreatedAt)
    .ToListAsync();

        var memoryBuilder = new System.Text.StringBuilder();

        memoryBuilder.AppendLine("You are a helpful AI assistant.");
        memoryBuilder.AppendLine("Use the previous conversation context if relevant.");
        memoryBuilder.AppendLine();

        foreach (var chat in previousMessages)
        {
            memoryBuilder.AppendLine($"User: {chat.UserMessage}");
            memoryBuilder.AppendLine($"AI: {chat.AiResponse}");
        }

        memoryBuilder.AppendLine();
        memoryBuilder.AppendLine($"Current User Message: {model.Message}");

        try
        {
            if (model.ImageFile != null)
            {
                uploadedImagePath = await SaveUploadedImageAsync(model.ImageFile);

                var fullImagePath = Path.Combine(
                    _environment.WebRootPath,
                    uploadedImagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

                aiResponse = await _geminiService.AnalyzeImageAsync(
                    model.Message ?? "Analyze this image clearly.",
                    fullImagePath);
            }
            else
            {
                aiResponse = await _geminiService.GenerateResponseAsync(memoryBuilder.ToString());
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("RESOURCE_EXHAUSTED") || ex.Message.Contains("quota"))
            {
                aiResponse = "Gemini free API quota limit reached. Please wait a little and try again later.";
            }
            else if (ex.Message.Contains("UNAVAILABLE") || ex.Message.Contains("503"))
            {
                aiResponse = "Gemini AI service is busy right now. Please try again after a moment.";
            }
            else
            {
                aiResponse = "AI service is temporarily unavailable. Please try again later.";
            }
        }

        var chatMessage = new ChatMessage
        {
            UserMessage = model.Message ?? "[Image uploaded]",
            AiResponse = aiResponse,
            UploadedImagePath = uploadedImagePath,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> GenerateImage(ChatViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ImagePrompt))
        {
            TempData["Error"] = "Please write an image prompt.";
            return RedirectToAction(nameof(Index));
        }

        var imageUrl = "https://placehold.co/1024x1024/png?text=AI+Generated+Image+Demo";

        var generatedImage = new GeneratedImage
        {
            Prompt = model.ImagePrompt,
            ImageUrl = imageUrl,
            GeneratedAt = DateTime.UtcNow
        };

        _context.GeneratedImages.Add(generatedImage);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
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
