using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/reviews")]
[Authorize]
public class CourseReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CourseReviewsController(AppDbContext context)
    {
        _context = context;
    }

    private int? GetUserIdOrNull()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : null;
    }

    private Task<bool> IsEnrolledAsync(int courseId, int userId, CancellationToken cancellationToken) =>
        _context.CourseEnrollments.AnyAsync(
            enrollment => enrollment.CourseId == courseId && enrollment.UserId == userId,
            cancellationToken);

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(int courseId, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
        if (course == null) return NotFound(new { message = "Nie znaleziono kursu." });

        // Przy [AllowAnonymous] token i tak jest odczytywany, jeśli przyszedł - wtedy znamy użytkownika
        var userId = GetUserIdOrNull();
        var isOwner = userId.HasValue && course.MentorId == userId.Value;
        var isAdmin = User.IsInRole("Admin");

        if (!course.isPublished && !isOwner && !isAdmin)
            return NotFound(new { message = "Nie znaleziono kursu." });

        var reviews = await _context.CourseReviews
            .AsNoTracking()
            .Where(review => review.CourseId == courseId)
            .OrderByDescending(review => review.UpdatedAt)
            .Select(review => new
            {
                review.Id,
                review.UserId,
                Username = review.User != null ? review.User.Username : "Usunięty użytkownik",
                AvatarUrl = review.User != null ? review.User.AvatarUrl : null,
                review.Rating,
                review.Comment,
                review.CreatedAt,
                review.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        string? reviewBlockedReason = null;
        if (!userId.HasValue) reviewBlockedReason = "notLoggedIn";
        else if (isOwner) reviewBlockedReason = "owner";
        else if (!course.isPublished) reviewBlockedReason = "notPublished";
        else if (!await IsEnrolledAsync(courseId, userId.Value, cancellationToken)) reviewBlockedReason = "notEnrolled";

        var myReview = userId.HasValue ? reviews.FirstOrDefault(review => review.UserId == userId.Value) : null;

        return Ok(new
        {
            AverageRating = reviews.Count == 0 ? (double?)null : Math.Round(reviews.Average(review => review.Rating), 1),
            ReviewCount = reviews.Count,
            // Distribution[0] = liczba ocen 1★, ..., Distribution[4] = liczba ocen 5★
            Distribution = Enumerable.Range(CourseReview.MinRating, CourseReview.MaxRating - CourseReview.MinRating + 1)
                .Select(stars => reviews.Count(review => review.Rating == stars))
                .ToList(),
            CanReview = reviewBlockedReason == null,
            ReviewBlockedReason = reviewBlockedReason,
            MyReview = myReview == null
                ? null
                : new { myReview.Id, myReview.Rating, myReview.Comment, myReview.UpdatedAt },
            Reviews = reviews.Select(review => new
            {
                review.Id,
                review.Username,
                review.AvatarUrl,
                review.Rating,
                review.Comment,
                review.CreatedAt,
                review.UpdatedAt,
                IsMine = userId.HasValue && review.UserId == userId.Value
            })
        });
    }

    // Dodanie albo edycja własnej oceny (jedna na kurs)
    [HttpPut("mine")]
    public async Task<IActionResult> SaveMyReview(
        int courseId,
        [FromBody] SaveCourseReviewDto dto,
        CancellationToken cancellationToken)
    {
        if (GetUserIdOrNull() is not int userId) return Unauthorized();

        if (dto.Rating < CourseReview.MinRating || dto.Rating > CourseReview.MaxRating)
            return BadRequest(new { message = "Wybierz ocenę od 1 do 5 gwiazdek." });

        var comment = dto.Comment?.Trim() ?? string.Empty;
        if (comment.Length > CourseReview.MaxCommentLength)
            return BadRequest(new { message = $"Komentarz może mieć maksymalnie {CourseReview.MaxCommentLength} znaków." });

        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
        if (course == null || !course.isPublished)
            return NotFound(new { message = "Nie znaleziono kursu." });

        if (course.MentorId == userId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Nie możesz ocenić własnego kursu." });

        if (!await IsEnrolledAsync(courseId, userId, cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Zapisz się na kurs, aby go ocenić." });

        var now = DateTime.UtcNow;
        var review = await _context.CourseReviews
            .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId, cancellationToken);

        var isNew = review == null;
        if (review == null)
        {
            review = new CourseReview { CourseId = courseId, UserId = userId, CreatedAt = now };
            _context.CourseReviews.Add(review);
        }

        review.Rating = dto.Rating;
        review.Comment = comment;
        review.UpdatedAt = now;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (isNew)
        {
            // Dwa równoległe zapisy tej samej osoby - drugi trafia na unikalny indeks
            return Conflict(new { message = "Twoja ocena została już zapisana. Odśwież stronę." });
        }

        return Ok(new
        {
            message = isNew ? "Dziękujemy za ocenę!" : "Zaktualizowano Twoją ocenę.",
            review.Id,
            review.Rating,
            review.Comment,
            review.UpdatedAt
        });
    }

    [HttpDelete("mine")]
    public async Task<IActionResult> DeleteMyReview(int courseId, CancellationToken cancellationToken)
    {
        if (GetUserIdOrNull() is not int userId) return Unauthorized();

        var review = await _context.CourseReviews
            .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId, cancellationToken);

        if (review != null)
        {
            _context.CourseReviews.Remove(review);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    // Moderacja: admin może usunąć dowolną ocenę (np. spam, wulgaryzmy).
    // Mentor celowo nie ma tej możliwości - nie powinien móc kasować krytycznych opinii o swoim kursie.
    [HttpDelete("{reviewId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteReview(int courseId, int reviewId, CancellationToken cancellationToken)
    {
        var review = await _context.CourseReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.CourseId == courseId, cancellationToken);

        if (review == null) return NotFound(new { message = "Nie znaleziono oceny." });

        _context.CourseReviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}