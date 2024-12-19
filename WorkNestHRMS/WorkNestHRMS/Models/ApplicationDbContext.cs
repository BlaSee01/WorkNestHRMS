using Microsoft.EntityFrameworkCore;

namespace WorkNestHRMS.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Workplace> Workplaces { get; set; }
        public DbSet<UserWorkplace> UserWorkplaces { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Konfiguracja relacji wiele-do-wielu
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
        }


    }

}
