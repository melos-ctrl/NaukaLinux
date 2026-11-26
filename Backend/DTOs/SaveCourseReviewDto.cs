namespace Backend.DTOs;

public class SaveCourseReviewDto
{
    // Walidacja jest w kontrolerze, żeby błędy wracały w tym samym formacie { message } co reszta API
    public int Rating { get; set; }
    public string? Comment { get; set; }
}