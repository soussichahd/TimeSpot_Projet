using Microsoft.EntityFrameworkCore;
using MyAmazonstore3.Data;
using MyAmazonstore3.Pages.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages
builder.Services.AddRazorPages();

// 🔥 API Controllers
builder.Services.AddControllers();

// DbContext
builder.Services.AddDbContext<MyAmazonstore3Context>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MyAmazonstore3Context")
        ?? throw new InvalidOperationException("Connection string not found")
    )
);

// Cache
builder.Services.AddMemoryCache();

// 🔥 RAG Services
builder.Services.AddScoped<Vectorizer>();
builder.Services.AddScoped<RechercheVectorielle>();
builder.Services.AddScoped<LLMService>();
builder.Services.AddScoped<RagService>();

// 🔥 HttpContext (cookies)
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// 🔥 IMPORTANT
app.MapRazorPages();
app.MapControllers();

app.Run();
