using Backend.DTOs;
using Backend.Models;

namespace Backend.Validation;

public static class CourseValidator
{
    public const int LessonBlockTypeQuestion = 1;

    public static bool IsQuestion(CreateLessonBlockDto block) =>
        block.Type == LessonBlockTypeQuestion || block.QuestionText != null;

    public static string? ValidateForPublish(IReadOnlyList<CreateModuleDto> modules)
    {
        var lessonCount = modules.Sum(module => module.Lessons.Count);
        if (lessonCount == 0)
            return "Dodaj co najmniej jedną lekcję, zanim opublikujesz kurs.";

        for (var moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++)
        {
            var module = modules[moduleIndex];
            if (string.IsNullOrWhiteSpace(module.Title))
                return $"Moduł {moduleIndex + 1} nie ma tytułu.";

            foreach (var lesson in module.Lessons)
            {
                if (string.IsNullOrWhiteSpace(lesson.Title))
                    return $"W module „{module.Title}” jest lekcja bez tytułu.";

                var questionNumber = 0;
                foreach (var block in lesson.Blocks.Where(IsQuestion))
                {
                    questionNumber++;
                    var error = ValidateQuestion(block);
                    if (error != null)
                        return $"Lekcja „{lesson.Title}”, pytanie {questionNumber}: {error}";
                }
            }
        }

        return null;
    }

    private static string? ValidateQuestion(CreateLessonBlockDto block)
    {
        if (string.IsNullOrWhiteSpace(block.QuestionText))
            return "wpisz treść pytania.";

        var options = block.Options ?? new List<CreateQuizOptionDto>();
        var correctCount = options.Count(option => option.IsCorrect);

        return block.QuestionType switch
        {
            QuestionType.MultipleChoice when options.Count < 2 => "dodaj co najmniej dwie odpowiedzi.",
            QuestionType.MultipleChoice when correctCount == 0 => "zaznacz co najmniej jedną poprawną odpowiedź.",
            QuestionType.TrueFalse when options.Count != 2 => "pytanie prawda/fałsz musi mieć dokładnie dwie odpowiedzi.",
            QuestionType.TrueFalse when correctCount != 1 => "zaznacz dokładnie jedną poprawną odpowiedź.",
            QuestionType.TextAnswer when options.Count == 0 => "dodaj co najmniej jedną akceptowaną odpowiedź.",
            _ => null
        };
    }
}