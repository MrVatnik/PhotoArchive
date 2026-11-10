using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PhotoArchive.Data;
using PhotoArchive.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Machine-specific overrides (e.g. Storage:OriginalsRoot), not committed - see appsettings.Local.example.json.
// Environment variables and command-line args are re-added after it so they still take precedence.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(
    options =>
    {
        // Uploads of a whole roll can be large: allow 1 GB in total. The body is not buffered in memory;
        // ASP.NET spools big files to temp files on disk and the upload action streams them to storage.
        options.ValueLengthLimit = 134217728;
        options.MultipartBodyLengthLimit = 1073741824;
        options.MultipartHeadersCountLimit = 64;
    }
);

builder.Services.AddDbContext<PhotoContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        // LocalDB / SQL Server 2019. Raise when the server is upgraded.
        sql => sql.UseCompatibilityLevel(150)));

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<IPhotoFileService, PhotoFileService>();
builder.Services.AddSingleton<ThumbnailQueue>();
builder.Services.AddSingleton<IThumbnailQueue>(sp => sp.GetRequiredService<ThumbnailQueue>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ThumbnailQueue>());


var app = builder.Build();

await DatabaseInitializer.MigrateAsync(app.Services);


app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(new CultureInfo("en-US")),
    SupportedCultures = new List<CultureInfo> { new CultureInfo("en-US") },
    SupportedUICultures = new List<CultureInfo> { new CultureInfo("en") }
});


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Originals live outside wwwroot (see "Storage" in appsettings.json).
// Thumbnails and previews are served by MediaController, which renders them on demand.
var files = app.Services.GetRequiredService<IPhotoFileService>();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(files.OriginalsRoot),
    RequestPath = StorageOptions.OriginalsRequestPath
});

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Films}/{action=Index}/{id?}");

app.Run();
