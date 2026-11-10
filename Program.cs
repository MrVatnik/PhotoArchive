using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PhotoArchive.Data;
using PhotoArchive.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Machine-specific overrides (e.g. Storage:OriginalsRoot), not committed - see appsettings.Local.example.json.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(
    options =>
    {
        // Set the limit to 1Gb
        options.BufferBody = true;
        options.BufferBodyLengthLimit = 1073741824;
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

// Photo files live outside wwwroot (see "Storage" in appsettings.json).
var files = app.Services.GetRequiredService<IPhotoFileService>();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(files.OriginalsRoot),
    RequestPath = StorageOptions.OriginalsRequestPath
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(files.ThumbnailsRoot),
    RequestPath = StorageOptions.ThumbnailsRequestPath
});

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Films}/{action=Index}/{id?}");

app.Run();
