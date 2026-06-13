using AiChatbotApp.Data;
using AiChatbotApp.Services;
using AiChatbotApp.Services.Providers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add MVC + API controller services
builder.Services.AddControllersWithViews();

// Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpClient for external AI API calls
builder.Services.AddHttpClient();

// Core AI services
builder.Services.AddScoped<GeminiService>();

// AI Provider fallback services
builder.Services.AddScoped<IAiTextProvider, GeminiTextProvider>();
builder.Services.AddScoped<IAiTextProvider, GroqTextProvider>();
builder.Services.AddScoped<IAiTextProvider, OpenRouterTextProvider>();

builder.Services.AddScoped<AiProviderRouterService>();

// Add Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();

    // In production, force HTTPS
    app.UseHttpsRedirection();
}

// Important for wwwroot/uploads and wwwroot/generated
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

// Important for attribute routed API controllers:
// /api/chat/history
// /api/chat/send
app.MapControllers();

// Old MVC route. Not our main frontend anymore, but keeping it does not hurt.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chat}/{action=Index}/{id?}");

app.Run();