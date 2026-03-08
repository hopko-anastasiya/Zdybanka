using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using System.Text.Json;
using Zdybanka.Models;

namespace Zdybanka.Models;

public partial class Lab1Context : DbContext
{
    public Lab1Context()
    {
    }

    public Lab1Context(DbContextOptions<Lab1Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Changeshistory> Changeshistories { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<Eventcategory> Eventcategories { get; set; }

    public virtual DbSet<Eventstatus> Eventstatuses { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<Organizationstatus> Organizationstatuses { get; set; }

    public virtual DbSet<Registration> Registrations { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Userfavorite> Userfavorites { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Database=Lab1;Username=postgres;Password=1234");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Changeshistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("changeshistory_pkey");

            entity.ToTable("changeshistory");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Changedat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("changedat");
            entity.Property(e => e.Changedata)
                .HasColumnType("json")
                .HasColumnName("changedata");
            entity.Property(e => e.Eventid).HasColumnName("eventid");

            entity.HasOne(d => d.Event).WithMany(p => p.Changeshistories)
                .HasForeignKey(d => d.Eventid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("changeshistory_eventid_fkey");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("event_pkey");

            entity.ToTable("event");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Eventdate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("eventdate");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasColumnName("location");
            entity.Property(e => e.Organizationid).HasColumnName("organizationid");
            entity.Property(e => e.Statusid).HasColumnName("statusid");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Updatedat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updatedat");

            entity.HasOne(d => d.Category).WithMany(p => p.Events)
                .HasForeignKey(d => d.Categoryid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("event_categoryid_fkey");

            entity.HasOne(d => d.Organization).WithMany(p => p.Events)
                .HasForeignKey(d => d.Organizationid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("event_organizationid_fkey");

            entity.HasOne(d => d.Status).WithMany(p => p.Events)
                .HasForeignKey(d => d.Statusid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("event_statusid_fkey");
        });

        modelBuilder.Entity<Eventcategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("eventcategory_pkey");

            entity.ToTable("eventcategory");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Categoryname)
                .HasMaxLength(255)
                .HasColumnName("categoryname");
        });

        modelBuilder.Entity<Eventstatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("eventstatus_pkey");

            entity.ToTable("eventstatus");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Statusname)
                .HasMaxLength(255)
                .HasColumnName("statusname");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("organization_pkey");

            entity.ToTable("organization");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Statusid).HasColumnName("statusid");
            entity.Property(e => e.Updatedat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updatedat");

            entity.HasOne(d => d.Status).WithMany(p => p.Organizations)
                .HasForeignKey(d => d.Statusid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("organization_statusid_fkey");
        });

        modelBuilder.Entity<Organizationstatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("organizationstatus_pkey");

            entity.ToTable("organizationstatus");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Statusname)
                .HasMaxLength(255)
                .HasColumnName("statusname");
        });

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("registration_pkey");

            entity.ToTable("registration");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Eventid).HasColumnName("eventid");
            entity.Property(e => e.Registrationdate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("registrationdate");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Event).WithMany(p => p.Registrations)
                .HasForeignKey(d => d.Eventid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("registration_eventid_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Registrations)
                .HasForeignKey(d => d.Userid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("registration_userid_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("User_pkey");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "User_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Fullname)
                .HasMaxLength(255)
                .HasColumnName("fullname");
        });

        modelBuilder.Entity<Userfavorite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("userfavorite_pkey");

            entity.ToTable("userfavorite");

            entity.HasIndex(e => new { e.Userid, e.Eventid }, "userfavorite_userid_eventid_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Eventid).HasColumnName("eventid");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Event).WithMany(p => p.Userfavorites)
                .HasForeignKey(d => d.Eventid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("userfavorite_eventid_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Userfavorites)
                .HasForeignKey(d => d.Userid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("userfavorite_userid_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var dateEntries = ChangeTracker.Entries()
         .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in dateEntries)
        {
            foreach (var prop in entry.Properties.Where(p => p.Metadata.ClrType == typeof(DateTime) || p.Metadata.ClrType == typeof(DateTime?)))
            {
                if (prop.CurrentValue is DateTime dt)
                {
                    prop.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                }
            }
        }

        // 2. ЛОГУВАННЯ ЗМІН (Тільки для редагування подій - EntityState.Modified)
        var modifiedEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is Event && e.State == EntityState.Modified)
            .ToList();

        var historyRecords = new List<Changeshistory>();

        foreach (var entry in modifiedEntries)
        {
            var ev = (Event)entry.Entity;
            var history = new Changeshistory
            {
                Changedat = DateTime.Now,
                Eventid = ev.Id
            };

            var changes = new Dictionary<string, object>();

            // Додаємо інформацію про те, що це саме оновлення
            changes["_Action"] = "Update";
            changes["_Entity"] = entry.Entity.GetType().Name;

            foreach (var property in entry.Properties)
            {
                // Для логу редагування записуємо ТІЛЬКИ ті поля, які реально змінилися
                if (property.IsModified && property.Metadata.Name != "Updatedat")
                {
                    changes[property.Metadata.Name] = new
                    {
                        Old = property.OriginalValue,
                        New = property.CurrentValue
                    };
                }
            }

            // Якщо реально змінених полів немає (наприклад, натиснули "Зберегти" без змін), не створюємо лог
            if (changes.Count > 2)
            {
                history.Changedata = JsonSerializer.Serialize(changes,
                    new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                historyRecords.Add(history);
            }
        }

        if (historyRecords.Any())
        {
            this.AddRange(historyRecords);
        }

        // 3. Збереження
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    }
