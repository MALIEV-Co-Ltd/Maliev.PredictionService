using Microsoft.EntityFrameworkCore;
using Maliev.PredictionService.Data.Entities;

namespace Maliev.PredictionService.Data.Contexts
{
    public class PredictionServiceContext : DbContext
    {
        public PredictionServiceContext(DbContextOptions<PredictionServiceContext> options)
            : base(options)
        {
        }

        public DbSet<FdmPrintData> FdmPrintData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FdmPrintData>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Material)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            // Add configurations for MjfPrintData and SlaPrintData when their entities are created
            // modelBuilder.Entity<MjfPrintData>(entity =>
            // {
            //     entity.Property(e => e.Id).HasColumnName("ID");
            // });

            // modelBuilder.Entity<SlaPrintData>(entity =>
            // {
            //     entity.Property(e => e.Id).HasColumnName("ID");
            //     entity.Property(e => e.Material)
            //         .IsRequired()
            //         .HasMaxLength(50);
            // });
        }
    }
}
