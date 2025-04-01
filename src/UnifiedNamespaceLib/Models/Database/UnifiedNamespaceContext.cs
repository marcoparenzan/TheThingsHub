using Microsoft.EntityFrameworkCore;

namespace UnifiedNamespaceLib.Models.Database;

public partial class UnifiedNamespaceContext : DbContext
{
    public UnifiedNamespaceContext(DbContextOptions<UnifiedNamespaceContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<TopicValue> TopicValues { get; set; }

    public virtual DbSet<ValueType> ValueTypes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.\\sqlexpress;Initial Catalog=UnifiedNamespace2025;Trusted_Connection=True;Integrated Security=SSPI;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Topic>(entity =>
        {
            entity.ToTable("Topics", "uns");

            entity.Property(e => e.Value).HasMaxLength(256);
        });

        modelBuilder.Entity<TopicValue>(entity =>
        {
            entity.ToTable("TopicValues", "uns");

            entity.Property(e => e.Value).HasMaxLength(2048);

            entity.HasOne(d => d.Topic).WithMany(p => p.TopicValues)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TopicValues_Topics");

            entity.HasOne(d => d.ValueType).WithMany(p => p.TopicValues)
                .HasForeignKey(d => d.ValueTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TopicValues_ValueTypes");
        });

        modelBuilder.Entity<ValueType>(entity =>
        {
            entity.ToTable("ValueTypes", "uns");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name)
                .HasMaxLength(10)
                .IsFixedLength();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
