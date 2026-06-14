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
    private readonly AiProviderRouterService _aiRouter;
    private readonly AiVisionRouterService _visionRouter;
    private readonly IWebHostEnvironment _environment;
    private readonly CloudflareImageGenerationService _imageGenerationService;

    public ApiChatController(
    ApplicationDbContext context,
    AiProviderRouterService aiRouter,
    AiVisionRouterService visionRouter,
    CloudflareImageGenerationService imageGenerationService,
    IWebHostEnvironment environment)
    {
        _context = context;
        _aiRouter = aiRouter;
        _visionRouter = visionRouter;
        _imageGenerationService = imageGenerationService;
        _environment = environment;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var conversations = await _context.Conversations
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new
            {
                id = x.Id,
                title = x.Title,
                createdAt = x.CreatedAt,
                updatedAt = x.UpdatedAt,
                messageCount = x.ChatMessages.Count,
                lastMessage = x.ChatMessages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.UserMessage)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(conversations);
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation()
    {
        var conversation = new Conversation
        {
            Title = "New Chat",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = conversation.Id,
            title = conversation.Title,
            createdAt = conversation.CreatedAt,
            updatedAt = conversation.UpdatedAt
        });
    }

    [HttpDelete("conversations/{id:int}")]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        var conversation = await _context.Conversations
            .Include(x => x.ChatMessages)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (conversation == null)
        {
            return NotFound(new
            {
                error = "Conversation not found."
            });
        }

        foreach (var message in conversation.ChatMessages)
        {
            DeleteUploadedImageFile(message.UploadedImagePath);
        }

        _context.Conversations.Remove(conversation);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Conversation deleted successfully."
        });
    }

    [HttpPatch("conversations/{id:int}/title")]
    public async Task<IActionResult> RenameConversation(
        int id,
        [FromBody] RenameConversationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                error = "Conversation title cannot be empty."
            });
        }

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(x => x.Id == id);

        if (conversation == null)
        {
            return NotFound(new
            {
                error = "Conversation not found."
            });
        }

        conversation.Title = TrimTitle(request.Title);
        conversation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = conversation.Id,
            title = conversation.Title,
            updatedAt = conversation.UpdatedAt
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int conversationId)
    {
        if (conversationId <= 0)
        {
            return BadRequest(new
            {
                error = "Conversation id is required."
            });
        }

        var conversationExists = await _context.Conversations
            .AnyAsync(x => x.Id == conversationId);

        if (!conversationExists)
        {
            return NotFound(new
            {
                error = "Conversation not found."
            });
        }

        var chats = await _context.ChatMessages
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id,
                conversationId = x.ConversationId,
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
    public async Task<IActionResult> ClearAllChatHistory()
    {
        var messages = await _context.ChatMessages.ToListAsync();

        foreach (var message in messages)
        {
            DeleteUploadedImageFile(message.UploadedImagePath);
        }

        var conversations = await _context.Conversations.ToListAsync();

        _context.ChatMessages.RemoveRange(messages);
        _context.Conversations.RemoveRange(conversations);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "All chat history cleared successfully."
        });
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(
        [FromForm] int? conversationId,
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

        var conversation = await GetOrCreateConversationAsync(
            conversationId,
            message,
            imageFile != null);

        if (conversation == null)
        {
            return NotFound(new
            {
                error = "Conversation not found."
            });
        }

        string? uploadedImagePath = null;
        string? providerName = null;
        string? generatedImagePath = null;
        string aiResponse;


        try
        {
            if (imageFile != null)
            {
                uploadedImagePath = await SaveUploadedImageAsync(imageFile);

                var fullImagePath = Path.Combine(
                    _environment.WebRootPath,
                    uploadedImagePath
                        .TrimStart('/')
                        .Replace("/", Path.DirectorySeparatorChar.ToString()));

                var imagePrompt = BuildMemoryPrompt(
                    conversation.Id,
                    string.IsNullOrWhiteSpace(message)
                        ? "Analyze this image clearly."
                        : message);

                var visionResult = await _visionRouter.AnalyzeImageAsync(
                    imagePrompt,
                    fullImagePath);

                providerName = visionResult.ProviderName;
                aiResponse = visionResult.Text;
            }
            else
            {
                if (IsImageGenerationRequest(message!))
                {
                    var imagePrompt = ExtractImageGenerationPrompt(message!);

                    var generatedImage = await _imageGenerationService.GenerateImageAsync(
                    imagePrompt);
                    providerName = "Cloudflare Workers AI";
                    generatedImagePath = generatedImage.ImagePath;

                    aiResponse =
                        $"### Generated Image\n\n" +
                        $"![Generated Image]({generatedImage.ImagePath})\n\n" +
                        $"**Prompt:** {generatedImage.Prompt}\n\n" +
                        $"**Model:** `{generatedImage.Model}`";
                }
                else
                {
                    var promptWithMemory = BuildMemoryPrompt(
                        conversation.Id,
                        message!);

                    var routerResult = await _aiRouter.GenerateTextAsync(promptWithMemory);

                    providerName = routerResult.ProviderName;
                    aiResponse = routerResult.Text;
                }
            }
        }
        catch (Exception ex)
        {
            aiResponse = GetFriendlyAiError(ex);
        }

        if (conversation.Title == "New Chat")
        {
            conversation.Title = GenerateConversationTitle(
                message,
                imageFile != null);
        }

        conversation.UpdatedAt = DateTime.UtcNow;

        var chatMessage = new ChatMessage
        {
            ConversationId = conversation.Id,

            UserMessage = string.IsNullOrWhiteSpace(message)
                ? "[Image uploaded]"
                : message,

            AiResponse = aiResponse,
            UploadedImagePath = uploadedImagePath,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            chatMessage.Id,
            chatMessage.ConversationId,
            ConversationTitle = conversation.Title,
            chatMessage.UserMessage,
            chatMessage.AiResponse,
            chatMessage.UploadedImagePath,
            chatMessage.CreatedAt,
            ProviderName = providerName,
            GeneratedImagePath = generatedImagePath
        });
    }

    private async Task<Conversation?> GetOrCreateConversationAsync(
        int? conversationId,
        string? message,
        bool hasImage)
    {
        if (conversationId.HasValue && conversationId.Value > 0)
        {
            return await _context.Conversations
                .FirstOrDefaultAsync(x => x.Id == conversationId.Value);
        }

        var conversation = new Conversation
        {
            Title = "New Chat",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        return conversation;
    }

    private string BuildMemoryPrompt(int conversationId, string currentMessage)
    {
        var previousMessages = _context.ChatMessages
            .Where(x => x.ConversationId == conversationId)
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
        memoryBuilder.AppendLine("Do not mention AI provider names such as Gemini, Groq, OpenRouter, Mistral, Cohere, HuggingFace, Cerebras, or GitHub Models.");
        memoryBuilder.AppendLine("Use previous conversation context only when it is relevant.");
        memoryBuilder.AppendLine("Only use messages from the current conversation.");
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
        if (ex.Message.Contains("RESOURCE_EXHAUSTED") ||
            ex.Message.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
        {
            return "AI quota or rate limit reached. Please wait a little and try again later.";
        }

        if (ex.Message.Contains("UNAVAILABLE") ||
            ex.Message.Contains("503"))
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

    private void DeleteUploadedImageFile(string? uploadedImagePath)
    {
        if (string.IsNullOrWhiteSpace(uploadedImagePath))
        {
            return;
        }

        var fullImagePath = Path.Combine(
            _environment.WebRootPath,
            uploadedImagePath
                .TrimStart('/')
                .Replace("/", Path.DirectorySeparatorChar.ToString()));

        if (System.IO.File.Exists(fullImagePath))
        {
            System.IO.File.Delete(fullImagePath);
        }
    }

    private bool IsImageGenerationRequest(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var text = message.Trim().ToLower();

        return text.StartsWith("/image ") ||
               text.StartsWith("/generate-image ") ||
               text.StartsWith("generate image:");
    }

    private string ExtractImageGenerationPrompt(string message)
    {
        var prompt = message.Trim();

        if (prompt.StartsWith("/image ", StringComparison.OrdinalIgnoreCase))
        {
            return prompt[7..].Trim();
        }

        if (prompt.StartsWith("/generate-image ", StringComparison.OrdinalIgnoreCase))
        {
            return prompt[16..].Trim();
        }

        if (prompt.StartsWith("generate image:", StringComparison.OrdinalIgnoreCase))
        {
            return prompt["generate image:".Length..].Trim();
        }

        return prompt;
    }

    private string GenerateConversationTitle(string? message, bool hasImage)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return hasImage ? "Image Chat" : "New Chat";
        }

        return TrimTitle(message);
    }

    private string TrimTitle(string title)
    {
        var cleanTitle = title
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        if (cleanTitle.Length > 45)
        {
            cleanTitle = cleanTitle[..45] + "...";
        }

        return cleanTitle;
    }
}

public class RenameConversationRequest
{
    public string Title { get; set; } = string.Empty;
}
