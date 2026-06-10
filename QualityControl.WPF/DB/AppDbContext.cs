using Microsoft.EntityFrameworkCore;
using QualityControl.WPF.DB.Models;

namespace QualityControl.WPF.DB
{
    public class AppDbContext : DbContext
    {
        // DbSets for your schema
        public DbSet<Project> Projects { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<VIN> VINs { get; set; }
        public DbSet<License> Licenses { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<ReportType> ReportTypes { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<OperationCode> OperationCodes { get; set; }
        public DbSet<ResultType> ResultTypes { get; set; }
        public DbSet<DataSet> DataSets { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Design-time constructor for EF Core tools
        public AppDbContext() : base()
        {
        }

        override public void Dispose()
        {
            base.Dispose();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Only configure if not already configured (for design-time support)
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=qualitycontrol.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Project unique index
            modelBuilder.Entity<Project>()
                .HasIndex(p => p.Name)
                .IsUnique();

            // Configure File relationship with Project and composite unique index
            modelBuilder.Entity<File>()
                .HasOne(f => f.Project)
                .WithMany(p => p.Files)
                .HasForeignKey(f => f.Project_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<File>()
                .HasIndex(f => new { f.Name, f.Hash })
                .IsUnique();

            // Configure unique indexes for lookup tables
            modelBuilder.Entity<VIN>()
                .HasIndex(v => v.Code)
                .IsUnique();

            modelBuilder.Entity<License>()
                .HasIndex(l => l.Name)
                .IsUnique();

            modelBuilder.Entity<Model>()
                .HasIndex(m => m.Name)
                .IsUnique();

            modelBuilder.Entity<ReportType>()
                .HasIndex(r => r.Name)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<OperationCode>()
                .HasIndex(o => o.Name)
                .IsUnique();

            modelBuilder.Entity<ResultType>()
                .HasIndex(r => r.Name)
                .IsUnique();

            // Configure relationships for DataSet
            modelBuilder.Entity<DataSet>()
                .HasOne(d => d.License)
                .WithMany(l => l.DataSets)
                .HasForeignKey(d => d.License_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DataSet>()
                .HasOne(d => d.File)
                .WithMany(f => f.DataSets)
                .HasForeignKey(d => d.File_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DataSet>()
                .HasOne(d => d.VIN)
                .WithMany(v => v.DataSets)
                .HasForeignKey(d => d.VIN_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DataSet>()
                .HasOne(d => d.Model)
                .WithMany(m => m.DataSets)
                .HasForeignKey(d => d.Model_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DataSet>()
                .HasOne(d => d.ReportType)
                .WithMany(r => r.DataSets)
                .HasForeignKey(d => d.ReportType_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DataSet>()
                .HasOne(d => d.Category)
                .WithMany(c => c.DataSets)
                .HasForeignKey(d => d.Category_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DataSet>()
                .HasOne(d => d.OperationCode)
                .WithMany(o => o.DataSets)
                .HasForeignKey(d => d.OperationCode_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DataSet>()
                .HasOne(d => d.ResultType)
                .WithMany(r => r.DataSets)
                .HasForeignKey(d => d.ResultType_Id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
