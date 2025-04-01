using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CatalogLib.Database;

public partial class CatalogContext : DbContext
{
    public CatalogContext(DbContextOptions<CatalogContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Thing> Things { get; set; }

    public virtual DbSet<ThingsAttachment> ThingsAttachments { get; set; }

    public virtual DbSet<ThingsProperty> ThingsProperties { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Thing>(entity =>
        {
            entity.ToTable("Things", "catalog");

            entity.Property(e => e.Description).HasMaxLength(256);
            entity.Property(e => e.Name).HasMaxLength(64);
        });

        modelBuilder.Entity<ThingsAttachment>(entity =>
        {
            entity.ToTable("ThingsAttachments", "catalog");

            entity.Property(e => e.ContentType).HasMaxLength(64);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Type).HasMaxLength(32);

            entity.HasOne(d => d.Thing).WithMany(p => p.ThingsAttachments)
                .HasForeignKey(d => d.ThingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ThingsAttachments_Things");
        });

        modelBuilder.Entity<ThingsProperty>(entity =>
        {
            entity.ToTable("ThingsProperties", "catalog");

            entity.Property(e => e.ContentType).HasMaxLength(64);
            entity.Property(e => e.Name).HasMaxLength(64);
            entity.Property(e => e.Type).HasMaxLength(32);

            entity.HasOne(d => d.Thing).WithMany(p => p.ThingsProperties)
                .HasForeignKey(d => d.ThingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ThingsProperties_Things");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
