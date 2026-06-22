using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;

namespace DataAccessObjects;

public partial class QuizManagementDbContext : DbContext
{
    public QuizManagementDbContext()
    {
    }

    public QuizManagementDbContext(DbContextOptions<QuizManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Answer> Answers { get; set; }

    public virtual DbSet<Deck> Decks { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<TestHistory> TestHistories { get; set; }

    public virtual DbSet<TestResultDetail> TestResultDetails { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Answers__3214EC0718E562F7");

            entity.HasOne(d => d.Question).WithMany(p => p.Answers)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK_Answers_Questions");
        });

        modelBuilder.Entity<Deck>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Decks__3214EC07F540EDFA");

            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);

            entity.HasOne(d => d.Subject).WithMany(p => p.Decks)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK_Decks_Subjects");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Question__3214EC079D8AB6B4");

            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.QuestionType).HasDefaultValue(1);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);

            entity.HasOne(d => d.Deck).WithMany(p => p.Questions)
                .HasForeignKey(d => d.DeckId)
                .HasConstraintName("FK_Questions_Decks");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Subjects__3214EC073667D612");

            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasIndex(e => new { e.Name, e.UserId }, "IX_Subjects_Name_UserId")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);

            entity.HasOne(d => d.User).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subjects_Users");
        });

        modelBuilder.Entity<TestHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TestHist__3214EC07D6370CED");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.Deck).WithMany(p => p.TestHistories)
                .HasForeignKey(d => d.DeckId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TestHistories_Decks");

            entity.HasOne(d => d.User).WithMany(p => p.TestHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TestHistories_Users");
        });

        modelBuilder.Entity<TestResultDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TestResu__3214EC07BD567949");

            entity.HasOne(d => d.Question).WithMany(p => p.TestResultDetails)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Details_Question");

            entity.HasOne(d => d.SelectedAnswer).WithMany(p => p.TestResultDetails)
                .HasForeignKey(d => d.SelectedAnswerId)
                .HasConstraintName("FK_Details_Answer");

            entity.HasOne(d => d.TestHistory).WithMany(p => p.TestResultDetails)
                .HasForeignKey(d => d.TestHistoryId)
                .HasConstraintName("FK_Details_History");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC077068E03B");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(256);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.SecurityStamp).HasMaxLength(450);
            entity.Property(e => e.IsDisabled).HasDefaultValue(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
