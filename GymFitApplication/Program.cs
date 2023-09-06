using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GymFitApplication.Data;
using GymFitApplication.Areas.Identity.Data;
using System.Globalization;
using Amazon.XRay.Recorder.Handlers.AwsSdk;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("GymFitApplicationContextConnection") ?? throw new InvalidOperationException("Connection string 'GymFitApplicationContextConnection' not found.");

builder.Services.AddDbContext<GymFitApplicationContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<GymFitApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<GymFitApplicationContext>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
AWSSDKHandler.RegisterXRayForAllServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseXRay("GymFity Web Application");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();;

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

//Seeding role data initial
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    var roles = new[] { "Admin", "Tutor", "Customer" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<GymFitApplicationUser>>();

    string username = "Admin";
    string email = "lingken2001@gmail.com";
    string password = "Admin@123";

    if (await userManager.FindByEmailAsync(email) == null)
    {
        var user = new GymFitApplicationUser();
        user.UserName = username;
        user.Rolename = "Admin";
        user.Email = email;
        user.PhoneNumber = "012-8888802";

        var dateOfBirth = DateTime.ParseExact("01/01/2001", "MM/dd/yyyy", CultureInfo.InvariantCulture);
        user.Dateofbirth = dateOfBirth;
        user.Gender = "Male";
        user.EmailConfirmed = true;

        await userManager.CreateAsync(user, password);

        await userManager.AddToRoleAsync(user, "Admin");
    }
}

app.Run();
