using GymFitApplication.Areas.Identity.Data;
using GymFitApplication.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymFitApplication.Data;

public class GymFitApplicationContext : IdentityDbContext<GymFitApplicationUser>
{
    public GymFitApplicationContext(DbContextOptions<GymFitApplicationContext> options)
        : base(options)
    {
    }

    public DbSet<GymFitApplication.Models.Booking> BookingTable { get; set; }

    public DbSet<GymFitApplication.Models.Feedback> FeedbackTable { get; set; }

    public DbSet<Package> PackageTable { get; set; }
    public DbSet<Tutor> TutorTable { get; set; }
    public DbSet<Product> ProductTable { get; set; }

    public DbSet<GymFitApplicationUser> GymFitApplicationUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<GymFitApplicationUser>().ToTable("AspNetUsers");
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
    }
}
