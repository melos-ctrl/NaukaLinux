using System.Text.Json.Serialization;
using Backend.Models;

namespace Backend.DTOs;

public sealed record CourseContentDto(
    int Id,
    string Title,
    string Description,
    bool IsPublished,
    IReadOnlyList<LessonContentDto> Lessons,
    IReadOnlyList<CourseModuleContentDto> Modules)
{
    public static CourseContentDto FromCourse(Course course, bool includeAnswerKey)
    {
        LessonContentDto MapLesson(Lesson lesson) => new(
            lesson.Id,
            lesson.Title,
            lesson.OrderIndex,
            lesson.Blocks.OrderBy(block => block.OrderIndex).Select(block => new LessonBlockContentDto(
                block.Id,
                block.Type,
                block.OrderIndex,
                block.TextContent,
                block.QuestionText,
                block.QuestionType.ToString(),
                block.FrontText,
                block.BackText,
                block.VideoUrl,
                block.Hint,
                block.Options.Select(option => new QuizOptionContentDto(
                    option.Id,
                    option.OptionText,
                    includeAnswerKey ? option.IsCorrect : null)).ToList())).ToList());

        return new CourseContentDto(
            course.Id,
            course.Title,
            course.Description,
            course.isPublished,
            course.Lessons.OrderBy(lesson => lesson.OrderIndex).Select(MapLesson).ToList(),
            course.Modules.OrderBy(module => module.Order).Select(module => new CourseModuleContentDto(
                module.Id,
                module.Title,
                module.Order,
                module.Lessons.OrderBy(lesson => lesson.OrderIndex).Select(MapLesson).ToList())).ToList());
    }
}

public sealed record CourseModuleContentDto(
    int Id,
    string Title,
    int Order,
    IReadOnlyList<LessonContentDto> Lessons);

public sealed record LessonContentDto(
    int Id,
    string Title,
    int OrderIndex,
    IReadOnlyList<LessonBlockContentDto> Blocks);

public sealed record LessonBlockContentDto(
    int Id,
    int Type,
    int OrderIndex,
    string? TextContent,
    string? QuestionText,
    string QuestionType,
    string? FrontText,
    string? BackText,
    string? VideoUrl,
    string? Hint,
    IReadOnlyList<QuizOptionContentDto> Options);

public sealed record QuizOptionContentDto(
    int Id,
    string OptionText,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsCorrect);

public sealed record VerifyAnswerDto(IReadOnlyList<int>? OptionIds, string? TextAnswer);