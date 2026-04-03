using Microsoft.EntityFrameworkCore;
using IWX_CloudZen.Authentication.Models.Entities;
using IWX_CloudZen.CloudAccounts.Entities;
using IWX_CloudZen.CloudStorage.Entities;

namespace IWX_CloudZen.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<CloudAccount> CloudAccounts { get; set; }
        public DbSet<CloudFile> CloudFiles { get; set; }

        // 'DbSet<User> Users;' This is a table that stores User objects

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CloudAccount>()
                .HasIndex(x => new { x.UserEmail, x.Provider, x.AccountName })
                .IsUnique();
        }
    }
}
