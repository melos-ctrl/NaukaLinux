namespace Backend.DTOs;

public class CreateQuizOptionDto
{
    public int? Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}