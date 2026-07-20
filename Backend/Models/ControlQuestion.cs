namespace Backend.Models;

public enum QuestionType
{
    Flashcard,
    TrueFalse,
    TextAnwser,
    MultipleChoice
}

public class ControlQuestion
{
    public int Id { get; set; }
    public QuestionType Type { get; set; }
    public string Content { get; set; } = string.Empty; 
    public int Order { get; set; }
    
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    
    public List<QuestionOption> Options { get; set; } = new();
}

public class QuestionOption
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    
    public int ControlQuestionId { get; set; }
    public ControlQuestion? ControlQuestion { get; set; }
}