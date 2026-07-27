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
}