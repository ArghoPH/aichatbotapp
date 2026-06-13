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

        try
        {
            if (model.ImageFile != null)
            {
                uploadedImagePath = await SaveUploadedImageAsync(model.ImageFile);

                aiResponse = await _geminiService.GenerateResponseAsync(
                    $"{model.Message ?? "Analyze this image."}\n\nNote: Image uploaded at {uploadedImagePath}.");
            }
            else
            {
                aiResponse = await _geminiService.GenerateResponseAsync(model.Message!);
            }
        }
        catch (Exception ex)
        {
            aiResponse = "AI service is temporarily unavailable. Please try again later.\n\nTechnical details: " + ex.Message;
        }

        var chatMessage = new ChatMessage
        {
            UserMessage = model.Message ?? "[Image uploaded]",
            AiResponse = aiResponse,
            UploadedImagePath = uploadedImagePath,
            CreatedAt = DateTime.Now
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
            GeneratedAt = DateTime.Now
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