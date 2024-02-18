using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MovieAnalyticsWeb.Data;
using MovieAnalyticsWeb.Models;
using System.Reflection;
using System.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Config
/*
((IConfigurationBuilder)builder.Configuration).Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", false)
    .AddUserSecrets(Assembly.GetEntryAssembly()!)
    .AddEnvironmentVariables();
*/

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient();

builder.Services.AddFluentEmail("kolbiesch10@gmail.com")
    .AddRazorRenderer()
    .AddSmtpSender("smtp.gmail.com", 587, "kolbiesch10@gmail.com", builder.Configuration.GetValue<string>("SMTP-Password"));

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration.GetValue<string>("AuthID");
        options.ClientSecret = builder.Configuration.GetValue<string>("AuthSecret");
    });

builder.Services.AddScoped<IService, Service>();
builder.Services.AddScoped<ITMDBApiClient, TMDBApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
