using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseModule> CourseModules { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<CourseEnrollment> CourseEnrollments { get; set; }

    public DbSet<UserProgress> UserProgresses { get; set; }
    public DbSet<StudentAnswer> StudentAnswers { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<LessonBlock> LessonBlocks { get; set; }
    public DbSet<QuizOption> QuizOptions { get; set; }
    public DbSet<MentorApplication> MentorApplications { get; set; }
    public DbSet<CourseReview> CourseReviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LessonBlock>()
            .Property(block => block.QuestionType)
            .HasDefaultValue(QuestionType.MultipleChoice)
            .HasSentinel(QuestionType.MultipleChoice);

        modelBuilder.Entity<StudentAnswer>()
            .HasIndex(answer => new { answer.UserId, answer.LessonBlockId })
            .IsUnique();

        modelBuilder.Entity<StudentAnswer>()
            .HasOne(answer => answer.User)
            .WithMany()
            .HasForeignKey(answer => answer.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentAnswer>()
            .HasOne(answer => answer.LessonBlock)
            .WithMany()
            .HasForeignKey(answer => answer.LessonBlockId)
            .OnDelete(DeleteBehavior.Cascade);

        // Jeden zapis postępu i jedno zapisanie na kurs na użytkownika
        modelBuilder.Entity<UserProgress>()
            .HasIndex(progress => new { progress.UserId, progress.CourseId })
            .IsUnique();

        modelBuilder.Entity<CourseEnrollment>()
            .HasIndex(enrollment => new { enrollment.UserId, enrollment.CourseId })
            .IsUnique();

        // Usunięcie bloku lekcji nie kasuje zgłoszenia - zostaje ono jako ogólna opinia o kursie
        modelBuilder.Entity<Feedback>()
            .HasOne(feedback => feedback.LessonBlock)
            .WithMany()
            .HasForeignKey(feedback => feedback.LessonBlockId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CourseReview>(review =>
        {
            // Jeden kursant może mieć tylko jedną ocenę danego kursu - pilnuje tego sama baza
            review.HasIndex(r => new { r.CourseId, r.UserId }).IsUnique();

            review.Property(r => r.Comment).HasMaxLength(CourseReview.MaxCommentLength);

            review.ToTable(table => table.HasCheckConstraint(
                "CK_CourseReviews_Rating",
                $"\"Rating\" BETWEEN {CourseReview.MinRating} AND {CourseReview.MaxRating}"));

            // Usunięcie konta albo kursu usuwa też jego oceny
            review.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            review.HasOne(r => r.Course)
                .WithMany()
                .HasForeignKey(r => r.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}