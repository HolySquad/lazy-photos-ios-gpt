using LazyPhotos.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LazyPhotos.Infrastructure.Data;

public class LazyPhotosDbContext : DbContext
{
    public LazyPhotosDbContext(DbContextOptions<LazyPhotosDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Photo> Photos { get; set; } = null!;
    public DbSet<Album> Albums { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<SharedLink> SharedLinks { get; set; } = null!;
    public DbSet<PhotoAlbum> PhotoAlbums { get; set; } = null!;
    public DbSet<PhotoTag> PhotoTags { get; set; } = null!;
    public DbSet<UploadSession> UploadSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // Photo configuration
        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Sha256Hash);
            entity.HasIndex(e => e.TakenAt);
            entity.HasIndex(e => new { e.UserId, e.TakenAt });

            entity.Property(e => e.Sha256Hash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(1024).IsRequired();
            entity.Property(e => e.ThumbnailPath).HasMaxLength(1024);
            entity.Property(e => e.OriginalFilename).HasMaxLength(512).IsRequired();
            entity.Property(e => e.MimeType).HasMaxLength(100);
            entity.Property(e => e.FileSize).IsRequired();
            entity.Property(e => e.Width).IsRequired();
            entity.Property(e => e.Height).IsRequired();
            entity.Property(e => e.TakenAt).IsRequired();
            entity.Property(e => e.UploadedAt).IsRequired();

            // EXIF metadata fields
            entity.Property(e => e.CameraModel).HasMaxLength(256);
            entity.Property(e => e.ExposureTime).HasMaxLength(50);
            entity.Property(e => e.FNumber).HasMaxLength(50);
            entity.Property(e => e.FocalLength).HasMaxLength(50);

            // Foreign key relationship
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Photos)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Album configuration
        modelBuilder.Entity<Album>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Name });

            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();

            // Foreign key relationships
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Albums)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CoverPhoto)
                  .WithMany()
                  .HasForeignKey(e => e.CoverPhotoId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // PhotoAlbum (many-to-many) configuration
        modelBuilder.Entity<PhotoAlbum>(entity =>
        {
            entity.HasKey(e => new { e.PhotoId, e.AlbumId });
            entity.Property(e => e.AddedAt).IsRequired();

            entity.HasOne(e => e.Photo)
                  .WithMany(p => p.PhotoAlbums)
                  .HasForeignKey(e => e.PhotoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Album)
                  .WithMany(a => a.PhotoAlbums)
                  .HasForeignKey(e => e.AlbumId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PhotoTag (many-to-many) configuration
        modelBuilder.Entity<PhotoTag>(entity =>
        {
            entity.HasKey(e => new { e.PhotoId, e.TagId });
            entity.Property(e => e.AddedAt).IsRequired();

            entity.HasOne(e => e.Photo)
                  .WithMany(p => p.PhotoTags)
                  .HasForeignKey(e => e.PhotoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                  .WithMany(t => t.PhotoTags)
                  .HasForeignKey(e => e.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Device configuration
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.DeviceId }).IsUnique();

            entity.Property(e => e.DeviceId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.DeviceName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Platform).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RegisteredAt).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Devices)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SharedLink configuration
        modelBuilder.Entity<SharedLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();

            entity.Property(e => e.Token).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Password).HasMaxLength(256);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.AccessCount).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Album)
                  .WithMany()
                  .HasForeignKey(e => e.AlbumId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Photo)
                  .WithMany()
                  .HasForeignKey(e => e.PhotoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UploadSession configuration
        modelBuilder.Entity<UploadSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => e.Hash);

            entity.Property(e => e.Hash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.MimeType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StorageKey).HasMaxLength(1024);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
