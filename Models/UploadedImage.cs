namespace AiChatbotApp.Models;

public class UploadedImage
{
    public int Id { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.Now;
}