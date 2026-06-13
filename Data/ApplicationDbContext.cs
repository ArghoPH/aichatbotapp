using Microsoft.EntityFrameworkCore;
using AiChatbotApp.Models;

namespace AiChatbotApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ChatMessage> ChatMessages { get; set; }

    public DbSet<UploadedImage> UploadedImages { get; set; }

    public DbSet<GeneratedImage> GeneratedImages { get; set; }
}