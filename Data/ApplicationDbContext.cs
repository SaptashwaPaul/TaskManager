using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using TaskManager.API.Entities;

namespace TaskManager.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<TaskItem> Tasks => Set<TaskItem>();
        public DbSet<TaskState> TaskStates => Set<TaskState>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.CreatedByUser)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Role>().HasData(
                    new Role { Id = 1, Name = "Admin" },
                    new Role { Id = 2, Name = "Manager" },
                    new Role { Id = 3, Name = "User" });

            modelBuilder.Entity<TaskState>().HasData(
                    new TaskState { Id = 1, Name = "Open" },
                    new TaskState { Id = 2, Name = "In Progress" },
                    new TaskState { Id = 3, Name = "Blocked" },
                    new TaskState { Id = 4, Name = "Done" } );
        }
    }
}
