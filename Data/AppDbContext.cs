using DotNetBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DotNetBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserFollow> UserFollows { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Reply> Replies { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //many-to-many (mean one user can follow many users, and one user can be followed by many users)
            modelBuilder.Entity<UserFollow>() // Configure the UserFollow entity.
                .HasOne(f => f.Follower) // UserFollow has one Follower
                .WithMany(u => u.Followers) // UserFollow has many Followers
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict); 
                
            // same as above but for the following user
            modelBuilder.Entity<UserFollow>()
                .HasOne(f => f.Following)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserFollow → FollowerId, FollowingId: unique constraint (mean one user can follow another user only once)
            modelBuilder.Entity<UserFollow>()
                .HasIndex(f => new { f.FollowerId, f.FollowingId })
                .IsUnique();

            // Reply → User: one-to-many (mean one user can have many replies, but one reply belongs to one user)
            modelBuilder.Entity<Reply>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent extra cascade

            // Reply → Post: one-to-many (mean one post can have many replies, but one reply belongs to one post)
            modelBuilder.Entity<Reply>()
                .HasOne(r => r.Post)
                .WithMany(p => p.Replies)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Post → User (optional if needed) one-to-many (mean one user can have many posts, but one post belongs to one user)
            modelBuilder.Entity<Post>()
                .HasOne(p => p.User) // Post has one User
                .WithMany() // User has many Posts
                .HasForeignKey(p => p.PostedById) 
                .OnDelete(DeleteBehavior.Cascade); // Or Restrict if you'd prefer to keep posts

            // PostLike → Post: one-to-many (mean one post can have many likes, but one like belongs to one post)
            modelBuilder.Entity<PostLike>()
                .HasOne(pl => pl.Post)  // PostLike has one Post
                .WithMany(p => p.Likes)  // Post has many Likes
                .HasForeignKey(pl => pl.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            // PostLike → User: one-to-many (mean one user can have many likes, but one like belongs to one user)
            modelBuilder.Entity<PostLike>()
                .HasOne(pl => pl.User)  // PostLike has one User
                .WithMany()   // User has many PostLikes
                .HasForeignKey(pl => pl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // PostLike → PostId, UserId: unique constraint (mean one user can like one post only once)
            modelBuilder.Entity<PostLike>() 
                .HasIndex(pl => new { pl.PostId, pl.UserId })
                .IsUnique();



            ////////////////////////////////////////////////////////////////////////////////////////
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<User>()
                .Property(u => u.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            ////////////////////////////////////////////////////////////////////////////////////////
            modelBuilder.Entity<Post>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Post>()
                .Property(p => p.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            ////////////////////////////////////////////////////////////////////////////////////////
            modelBuilder.Entity<Reply>()
                .Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Reply>()
                .Property(r => r.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // var entries = ChangeTracker
            //     .Entries()
            //     .Where(e => e.Entity is User && e.State == EntityState.Modified);

            // foreach (var entry in entries)
            // {
            //     ((User)entry.Entity).UpdatedAt = DateTime.UtcNow;
            // }

            // return await base.SaveChangesAsync(cancellationToken);
            var now = DateTime.UtcNow;

            var updatedUsers = ChangeTracker.Entries<User>()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in updatedUsers)
            {
                entry.Entity.UpdatedAt = now;
            }

            var updatedPosts = ChangeTracker.Entries<Post>()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in updatedPosts)
            {
                entry.Entity.UpdatedAt = now;
            }

            var updatedReplies = ChangeTracker.Entries<Reply>()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in updatedReplies)
            {
                entry.Entity.UpdatedAt = now;
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}