using Microsoft.EntityFrameworkCore;

namespace WorkNestHRMS.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Workplace> Workplaces { get; set; } = null!;
        public DbSet<UserWorkplace> UserWorkplaces { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<WorkGroup> WorkGroups { get; set; } = null!;
        public DbSet<UserWorkGroup> UserWorkGroups { get; set; } = null!;
        public DbSet<Task> Tasks { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Workplace wiele do wielu
            modelBuilder.Entity<UserWorkplace>()
                .HasKey(uw => new { uw.UserId, uw.WorkplaceId });

            modelBuilder.Entity<UserWorkplace>()
                .HasOne(uw => uw.User)
                .WithMany(u => u.UserWorkplaces)
                .HasForeignKey(uw => uw.UserId);

            modelBuilder.Entity<UserWorkplace>()
                .HasOne(uw => uw.Workplace)
                .WithMany(w => w.UserWorkplaces)
                .HasForeignKey(uw => uw.WorkplaceId);

            // User - Employee wiele do wielu
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithOne(u => u.Employee)
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - Workgroups  wiele do wielu
            modelBuilder.Entity<UserWorkGroup>()
                .HasKey(uwg => new { uwg.UserId, uwg.WorkGroupId });

            modelBuilder.Entity<UserWorkGroup>()
                .HasOne(uwg => uwg.User)
                .WithMany(u => u.UserWorkGroups)
                .HasForeignKey(uwg => uwg.UserId);

            modelBuilder.Entity<UserWorkGroup>()
                .HasOne(uwg => uwg.WorkGroup)
                .WithMany(wg => wg.UserWorkGroups)
                .HasForeignKey(uwg => uwg.WorkGroupId);

            // relacje TASK
            modelBuilder.Entity<Task>()
                .HasOne(t => t.AssignedUser)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.AssignedUserId);

            modelBuilder.Entity<Task>()
                .HasOne(t => t.AssignedWorkGroup)
                .WithMany(wg => wg.Tasks)
                .HasForeignKey(t => t.AssignedWorkGroupId);

            modelBuilder.Entity<Task>()
                .HasOne(t => t.CreatedByUser)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(t => t.CreatedByUserId);

            modelBuilder.Entity<Task>()
                .HasOne(t => t.Workplace)
                .WithMany(w => w.Tasks)
                .HasForeignKey(t => t.WorkplaceId);
        }
    }
}