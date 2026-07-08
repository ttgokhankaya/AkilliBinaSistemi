using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Adle.Api.Data;

public partial class AdleDbContext : DbContext
{
    public AdleDbContext(DbContextOptions<AdleDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Actor> Actors { get; set; }

    public virtual DbSet<Area> Areas { get; set; }

    public virtual DbSet<AreaBasis> AreaBases { get; set; }

    public virtual DbSet<AreaType> AreaTypes { get; set; }

    public virtual DbSet<DeviceBasis> DeviceBases { get; set; }

    public virtual DbSet<GraphNodeDeviceMapping> GraphNodeDeviceMappings { get; set; }

    public virtual DbSet<GraphObject> GraphObjects { get; set; }

    public virtual DbSet<Habit> Habits { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<Memory> Memories { get; set; }

    public virtual DbSet<MigrationHistory> MigrationHistories { get; set; }

    public virtual DbSet<Operation> Operations { get; set; }

    public virtual DbSet<OperationDevice> OperationDevices { get; set; }

    public virtual DbSet<OperationHabitMapping> OperationHabitMappings { get; set; }

    public virtual DbSet<Scenario> Scenarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Actor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Actor_pkey");

            entity.ToTable("Actor");

            entity.Property(e => e.Id).HasColumnName("ID");
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Areas_pkey");

            entity.HasIndex(e => e.Name, "IX_Areas_Name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AreaId).HasColumnName("AreaID");
            entity.Property(e => e.AreaTypeId).HasColumnName("AreaTypeID");
            entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.DeletedDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.ModifiedDate).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.AreaNavigation).WithMany(p => p.InverseAreaNavigation)
                .HasForeignKey(d => d.AreaId)
                .HasConstraintName("Areas_AreaID_fkey");

            entity.HasOne(d => d.AreaType).WithMany(p => p.Areas)
                .HasForeignKey(d => d.AreaTypeId)
                .HasConstraintName("Areas_AreaTypeID_fkey");
        });

        modelBuilder.Entity<AreaBasis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AreaBases_pkey");

            entity.Property(e => e.Id).HasColumnName("ID");
        });

        modelBuilder.Entity<AreaType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AreaTypes_pkey");

            entity.HasIndex(e => e.Name, "IX_AreaTypes_Name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.DeletedDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.ModifiedDate).HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<DeviceBasis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("DeviceBases_pkey");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AreaId).HasColumnName("AreaID");
            entity.Property(e => e.Discriminator).HasMaxLength(128);
            entity.Property(e => e.Ip).HasColumnName("ip");
            entity.Property(e => e.State).HasColumnName("state");

            entity.HasOne(d => d.Area).WithMany(p => p.DeviceBases)
                .HasForeignKey(d => d.AreaId)
                .HasConstraintName("DeviceBases_AreaID_fkey");
        });

        modelBuilder.Entity<GraphNodeDeviceMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("GraphNodeDeviceMappings_pkey");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.GraphId).HasColumnName("GraphID");

            entity.HasOne(d => d.Device).WithMany(p => p.GraphNodeDeviceMappings)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("GraphNodeDeviceMappings_DeviceID_fkey");

            entity.HasOne(d => d.Graph).WithMany(p => p.GraphNodeDeviceMappings)
                .HasForeignKey(d => d.GraphId)
                .HasConstraintName("GraphNodeDeviceMappings_GraphID_fkey");
        });

        modelBuilder.Entity<GraphObject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("GraphObjects_pkey");

            entity.Property(e => e.Id).HasColumnName("ID");
        });

        modelBuilder.Entity<Habit>(entity =>
        {
            entity.HasKey(e => e.HabitId).HasName("Habits_pkey");

            entity.Property(e => e.HabitId).HasColumnName("HabitID");

            entity.HasMany(d => d.Actors).WithMany(p => p.HabitHabits)
                .UsingEntity<Dictionary<string, object>>(
                    "HabitActor",
                    r => r.HasOne<Actor>().WithMany()
                        .HasForeignKey("ActorId")
                        .HasConstraintName("HabitActors_Actor_ID_fkey"),
                    l => l.HasOne<Habit>().WithMany()
                        .HasForeignKey("HabitHabitId")
                        .HasConstraintName("HabitActors_Habit_HabitID_fkey"),
                    j =>
                    {
                        j.HasKey("HabitHabitId", "ActorId").HasName("HabitActors_pkey");
                        j.ToTable("HabitActors");
                        j.IndexerProperty<int>("HabitHabitId").HasColumnName("Habit_HabitID");
                        j.IndexerProperty<int>("ActorId").HasColumnName("Actor_ID");
                    });
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Items_pkey");

            entity.HasIndex(e => e.Name, "IX_Items_Name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AreaOfItemId).HasColumnName("AreaOfItemID");
            entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.DeletedDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.ModifiedDate).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.AreaOfItem).WithMany(p => p.Items)
                .HasForeignKey(d => d.AreaOfItemId)
                .HasConstraintName("Items_AreaOfItemID_fkey");
        });

        modelBuilder.Entity<Memory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Memories_pkey");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AreaId).HasColumnName("AreaID");
            entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Date).HasColumnType("timestamp without time zone");
            entity.Property(e => e.DeletedDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.ModifiedDate).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Area).WithMany(p => p.Memories)
                .HasForeignKey(d => d.AreaId)
                .HasConstraintName("Memories_AreaID_fkey");

            entity.HasOne(d => d.Item).WithMany(p => p.Memories)
                .HasForeignKey(d => d.ItemId)
                .HasConstraintName("Memories_ItemID_fkey");
        });

        modelBuilder.Entity<MigrationHistory>(entity =>
        {
            entity.HasKey(e => new { e.MigrationId, e.ContextKey }).HasName("PK_dbo.__MigrationHistory");

            entity.ToTable("__MigrationHistory", "dbo");

            entity.Property(e => e.MigrationId).HasMaxLength(150);
            entity.Property(e => e.ContextKey).HasMaxLength(300);
            entity.Property(e => e.ProductVersion).HasMaxLength(32);
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Operations_pkey");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.StartTime).HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<OperationDevice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("OperationDevices_pkey");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AreaId).HasColumnName("AreaID");
            entity.Property(e => e.DeviceBaseId).HasColumnName("DeviceBaseID");
            entity.Property(e => e.OperationId).HasColumnName("OperationID");

            entity.HasOne(d => d.Area).WithMany(p => p.OperationDevices)
                .HasForeignKey(d => d.AreaId)
                .HasConstraintName("OperationDevices_AreaID_fkey");

            entity.HasOne(d => d.DeviceBase).WithMany(p => p.OperationDevices)
                .HasForeignKey(d => d.DeviceBaseId)
                .HasConstraintName("OperationDevices_DeviceBaseID_fkey");

            entity.HasOne(d => d.Operation).WithMany(p => p.OperationDevices)
                .HasForeignKey(d => d.OperationId)
                .HasConstraintName("OperationDevices_OperationID_fkey");
        });

        modelBuilder.Entity<OperationHabitMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("OperationHabitMappings_pkey");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.HabitId).HasColumnName("HabitID");
            entity.Property(e => e.OperationId).HasColumnName("OperationID");

            entity.HasOne(d => d.Habit).WithMany(p => p.OperationHabitMappings)
                .HasForeignKey(d => d.HabitId)
                .HasConstraintName("OperationHabitMappings_HabitID_fkey");

            entity.HasOne(d => d.Operation).WithMany(p => p.OperationHabitMappings)
                .HasForeignKey(d => d.OperationId)
                .HasConstraintName("OperationHabitMappings_OperationID_fkey");
        });

        modelBuilder.Entity<Scenario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Scenarios_pkey");

            entity.Property(e => e.Id).HasColumnName("ID");

            entity.HasMany(d => d.Actors).WithMany(p => p.Scenarios)
                .UsingEntity<Dictionary<string, object>>(
                    "ScenarioActor",
                    r => r.HasOne<Actor>().WithMany()
                        .HasForeignKey("ActorId")
                        .HasConstraintName("ScenarioActors_Actor_ID_fkey"),
                    l => l.HasOne<Scenario>().WithMany()
                        .HasForeignKey("ScenarioId")
                        .HasConstraintName("ScenarioActors_Scenario_ID_fkey"),
                    j =>
                    {
                        j.HasKey("ScenarioId", "ActorId").HasName("ScenarioActors_pkey");
                        j.ToTable("ScenarioActors");
                        j.IndexerProperty<int>("ScenarioId").HasColumnName("Scenario_ID");
                        j.IndexerProperty<int>("ActorId").HasColumnName("Actor_ID");
                    });

            entity.HasMany(d => d.AreaBases).WithMany(p => p.Scenarios)
                .UsingEntity<Dictionary<string, object>>(
                    "ScenarioAreaBasis",
                    r => r.HasOne<AreaBasis>().WithMany()
                        .HasForeignKey("AreaBaseId")
                        .HasConstraintName("ScenarioAreaBases_AreaBase_ID_fkey"),
                    l => l.HasOne<Scenario>().WithMany()
                        .HasForeignKey("ScenarioId")
                        .HasConstraintName("ScenarioAreaBases_Scenario_ID_fkey"),
                    j =>
                    {
                        j.HasKey("ScenarioId", "AreaBaseId").HasName("ScenarioAreaBases_pkey");
                        j.ToTable("ScenarioAreaBases");
                        j.IndexerProperty<int>("ScenarioId").HasColumnName("Scenario_ID");
                        j.IndexerProperty<int>("AreaBaseId").HasColumnName("AreaBase_ID");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
