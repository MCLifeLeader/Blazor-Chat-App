using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Blazor.Chat.App.Data.Sql;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>
    /// Chat sessions/rooms
    /// </summary>
    public DbSet<ChatSession> ChatSessions { get; set; } = null!;

    /// <summary>
    /// Chat session participants
    /// </summary>
    public DbSet<ChatParticipant> ChatParticipants { get; set; } = null!;

    /// <summary>
    /// Chat messages (metadata only, full content in Cosmos DB)
    /// </summary>
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

    /// <summary>
    /// Outbox entries for reliable Cosmos DB delivery
    /// </summary>
    public DbSet<ChatOutbox> ChatOutbox { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Chat Session
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.LastActivityAt);
            entity.HasIndex(e => new { e.TenantId, e.State });

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Chat Participant
        modelBuilder.Entity<ChatParticipant>(entity =>
        {
            entity.HasIndex(e => new { e.SessionId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LastReadAt);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Participants)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Restrict); // changed from Cascade to Restrict to avoid multiple cascade paths

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.LastReadMessage)
                .WithMany()
                .HasForeignKey(e => e.LastReadMessageId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Chat Message
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasIndex(e => new { e.SessionId, e.SentAt });
            entity.HasIndex(e => e.SenderUserId);
            entity.HasIndex(e => e.OutboxId).IsUnique();
            entity.HasIndex(e => e.ReplyToMessageId);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SenderUser)
                .WithMany()
                .HasForeignKey(e => e.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.OutboxEntry)
                .WithMany(o => o.RelatedMessages)
                .HasForeignKey(e => e.OutboxId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReplyToMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(e => e.ReplyToMessageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Chat Outbox
        modelBuilder.Entity<ChatOutbox>(entity =>
        {
            entity.HasIndex(e => new { e.Status, e.NextRetryAt });
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.MessageId);
        });
    }
}