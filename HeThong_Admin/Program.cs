using Microsoft.EntityFrameworkCore;
using HeThong_User.Models;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Thay dòng cũ bằng đoạn này
builder.Services.AddControllersWithViews()
    .ConfigureApplicationPartManager(manager =>
    {
        // Tìm và loại bỏ các Controller "ké" từ project HeThong_User
        var userPart = manager.ApplicationParts
            .FirstOrDefault(p => p.Name == "HeThong_User");
        if (userPart != null)
        {
            manager.ApplicationParts.Remove(userPart);
        }
    });
builder.Services.AddDbContext<HeThongChiaSeTaiLieu_V1>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
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
