using Microsoft.EntityFrameworkCore;
using MovieDemo.Data;
using MovieDemo.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Add the Database Service (Bridge to SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- SEEDER LOGIC START (Safe Mode) ---
// We use Console.WriteLine so you can see progress in the black console window
Console.WriteLine(">>> DEBUG: App started. Beginning database check...");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        Console.WriteLine(">>> DEBUG: Attempting to connect to SQL Server...");

        // This is usually where the 'hang' happens if the connection string is wrong
        context.Database.EnsureCreated();

        Console.WriteLine(">>> DEBUG: Connection Successful! Database is ready.");

        // This line calls your DbSeeder class
        DbSeeder.Seed(context);

        Console.WriteLine(">>> DEBUG: Seeding sequence complete.");
    }
    catch (Exception ex)
    {
        Console.WriteLine(">>> CRITICAL ERROR DURING STARTUP: " + ex.Message);
        if (ex.InnerException != null)
            Console.WriteLine(">>> INNER EXCEPTION: " + ex.InnerException.Message);

        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
Console.WriteLine(">>> DEBUG: Moving to Web Server configuration...");
// --- SEEDER LOGIC END ---

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// 2. Default Route (Points to your Movies/IndexM)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Movies}/{action=IndexM}/{id?}");

app.Run();