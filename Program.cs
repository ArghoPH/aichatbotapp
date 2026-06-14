using AiChatbotApp.Data;
using AiChatbotApp.Services;
using AiChatbotApp.Services.Providers;
using Microsoft.EntityFrameworkCore;
// Add services to the container.
var builder = WebApplication.CreateBuilder(args);

// Set port
var port = Environment.GetEnvironmentVariable("PORT") ?? "5077";

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// MVC + API Controllers
builder.Services.AddControllersWithViews();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://YOUR-VERCEL-APP.vercel.app")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// HttpClient for external AI API calls
builder.Services.AddHttpClient();

// Core AI service for Gemini image analysis
builder.Services.AddScoped<GeminiService>();

// Image generation service
builder.Services.AddScoped<CloudflareImageGenerationService>();

// AI Text Provider fallback chain
builder.Services.AddScoped<IAiTextProvider, GeminiTextProvider>();
builder.Services.AddScoped<IAiTextProvider, GroqTextProvider>();
builder.Services.AddScoped<IAiTextProvider, OpenRouterTextProvider>();
builder.Services.AddScoped<IAiTextProvider, MistralTextProvider>();
builder.Services.AddScoped<IAiTextProvider, CohereTextProvider>();
builder.Services.AddScoped<IAiTextProvider, HuggingFaceTextProvider>();
builder.Services.AddScoped<IAiTextProvider, CerebrasTextProvider>();

// AI Text Provider Router
builder.Services.AddScoped<AiProviderRouterService>();

// AI Vision Provider fallback chain
builder.Services.AddScoped<IAiVisionProvider, GeminiVisionProvider>();
builder.Services.AddScoped<IAiVisionProvider, GitHubModelsVisionProvider>();

// AI Vision Provider Router
builder.Services.AddScoped<AiVisionRouterService>();

// Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Migrate database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();

    // In production, force HTTPS
    app.UseHttpsRedirection();
}

// Static files for uploads/generated images
app.UseStaticFiles();

app.UseRouting();

app.UseCors("FrontendPolicy");

app.UseSession();

app.UseAuthorization();

// Attribute-routed API controllers:
// /api/chat/history
// /api/chat/send
// /api/chat/providers/status
app.MapControllers();

app.MapGet("/health", () => Results.Ok("Healthy"));

// Old MVC route, not main frontend anymore
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chat}/{action=Index}/{id?}");

app.Run();