using IntellAccount.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Read configuration from appsettings
var configuration = new ConfigurationBuilder()
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json")
    .Build();


// Bind AzureDocumentConfig section from appsettings
builder.Services.Configure<AzureDocumentConfig>(configuration.GetSection("AzureDocument"));
builder.Services.Configure<AzureStorageConfig>(configuration.GetSection("AzureStorage"));
builder.Services.Configure<OpenAiConfig>(configuration.GetSection("AzureOpenAi"));
builder.Services.Configure<SearchApiConfig>(configuration.GetSection("SearchApiConfig"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
