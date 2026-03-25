using GDG_DashBoard.BLL.Services.Admin;
using GDG_DashBoard.BLL.Services.Auth;
using GDG_DashBoard.BLL.Services.Email;
using GDG_DashBoard.BLL.Services.Role;
using GDG_DashBoard.DAL.Models;
using GDG_DashBoard.DAL.Repositores.GenericRepository;
using GDGDashBoard.DAL.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GDG_DashBoard.BLL.Services.Group;
using GDG_DashBoard.BLL.Services.RoadmapServices;
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    options.User.RequireUniqueEmail = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddRoles<IdentityRole<Guid>>()            // ← CRITICAL: registers RoleClaimsPrincipalFactory
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ── Generic Repository ─────────────────────────────────────────────────────
builder.Services.AddScoped(typeof(IGenericRepositoryAsync<>), typeof(GenericRepositoryAsync<>));

// ── BLL Services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IRoadmapService, RoadmapService>();

var app = builder.Build();

// ── Seed Roles on Startup ─────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    string[] roles = { "Admin", "Member", "Speaker", "Organizer", "Mentor" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }

    // Seed Admin User
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = builder.Configuration["AdminSettings:AdminEmail"];
    var adminPassword = builder.Configuration["AdminSettings:AdminPassword"];

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser 
        { 
            UserName = adminEmail, 
            Email = adminEmail,
            EmailConfirmed = true 
        };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRolesAsync(adminUser, new[] { "Admin", "Speaker", "Mentor" });
        }
        else
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"[Seed Error]: {error.Description}");
            }
        }
    }
    else
    {
        // Ensure roles are assigned if user already exists
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            await userManager.AddToRoleAsync(adminUser, "Admin");
        if (!await userManager.IsInRoleAsync(adminUser, "Speaker"))
            await userManager.AddToRoleAsync(adminUser, "Speaker");
        if (!await userManager.IsInRoleAsync(adminUser, "Mentor"))
            await userManager.AddToRoleAsync(adminUser, "Mentor");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
