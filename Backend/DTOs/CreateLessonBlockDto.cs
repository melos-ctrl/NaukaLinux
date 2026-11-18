using System.Text.Json.Serialization;
using Backend.Models;

namespace Backend.DTOs;

public class CreateLessonBlockDto
{
    public int? Id { get; set; }
    public int Type { get; set; }
    public int OrderIndex { get; set; }
    public string? TextContent { get; set; }
    public string? QuestionText { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;
    public string? FrontText { get; set; }
    public string? BackText { get; set; }

    public string? VideoUrl { get; set; }

    public List<CreateQuizOptionDto>? Options { get; set; }

    public string? Hint {get; set;}
}