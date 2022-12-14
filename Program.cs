using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PhotoArchive.Data;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddDbContext<PhotoContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();




app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(new CultureInfo("en-US"))
            ,
    SupportedCultures = new List<CultureInfo>
            {
                new CultureInfo("en-US")
            }
            ,
    SupportedUICultures = new List<CultureInfo>
            {
                new CultureInfo("en")
            }
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

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Films}/{action=Index}/{id?}");

app.Run();


