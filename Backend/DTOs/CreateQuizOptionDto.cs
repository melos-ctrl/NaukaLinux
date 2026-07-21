namespace Backend.DTOs;

public class CreateQuizOptionDto
{
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}