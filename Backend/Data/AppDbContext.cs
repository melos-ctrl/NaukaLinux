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
    public DbSet<ControlQuestion> ControlQuestions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<CourseEnrollment> CourseEnrollments { get; set; }

    public DbSet<UserProgress> UserProgresses { get; set; }
    public DbSet<StudentAnswer> StudentAnswers { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<LessonBlock> LessonBlocks { get; set; }
    public DbSet<QuizOption> QuizOptions { get; set; }
    public DbSet<MentorApplication> MentorApplications { get; set; }

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
    }
}