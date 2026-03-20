using Microsoft.EntityFrameworkCore;
using IWX_CloudZen.Models.Entities;

namespace IWX_CloudZen.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }

        // 'DbSet<User> Users;' This is a table that stores User objects
    }
}
