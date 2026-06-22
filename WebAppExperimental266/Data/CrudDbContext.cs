using Microsoft.EntityFrameworkCore;
using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Data
{
    public class CrudDbContext : DbContext
    {
        private readonly CrudDataSettings _settings;

        public CrudDbContext(
            DbContextOptions<CrudDbContext> options,
            CrudDataSettings settings) : base(options)
        {
            _settings = settings;
        }

        public DbSet<CrudRecord> CrudRecords => Set<CrudRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var entity = modelBuilder.Entity<CrudRecord>();
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Title).HasMaxLength(120).IsRequired();
            entity.Property(record => record.Description).HasMaxLength(4000);
            entity.Property(record => record.UploadedFileName).HasMaxLength(260);
            entity.Property(record => record.UploadedContentType).HasMaxLength(120);
            entity.Property(record => record.OwnerId).HasMaxLength(256).IsRequired();
            entity.Property(record => record.OwnerDisplayName).HasMaxLength(256);

            if (_settings.UseCosmos)
            {
                entity.ToContainer(_settings.CosmosContainerName);
                entity.HasPartitionKey(record => record.OwnerId);
            }
            else
            {
                entity.ToTable("CrudRecords");
            }
        }
    }
}
