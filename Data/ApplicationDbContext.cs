using AiChatbotApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AiChatbotApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations { get; set; }

    public DbSet<ChatMessage> ChatMessages { get; set; }

    public DbSet<UploadedImage> UploadedImages { get; set; }

    public DbSet<GeneratedImage> GeneratedImages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Conversation>()
            .HasMany(x => x.ChatMessages)
            .WithOne(x => x.Conversation)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Conversation>()
            .Property(x => x.Title)
            .HasMaxLength(120);
    }
}
