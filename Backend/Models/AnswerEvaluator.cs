using Backend.Models;

namespace Backend.Service;

public static class AnswerEvaluator
{
    public const int PointsPerCorrectAnswer = 10;


    public static bool IsQuestion(LessonBlock block) => block.QuestionText != null;
    public static string NormalizeTextAnswer(string? text) =>
        string.Join(' ', (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Czy wpisana odpowiedź pasuje do akceptowanej (z rozróżnianiem wielkości liter).</summary>
    public static bool TextMatches(string? acceptedAnswer, string? textAnswer)
    {
        var answer = NormalizeTextAnswer(textAnswer);
        return answer.Length > 0 && string.Equals(NormalizeTextAnswer(acceptedAnswer), answer, StringComparison.Ordinal);
    }

    public static bool Evaluate(LessonBlock block, IReadOnlyCollection<int>? optionIds, string? textAnswer)
    {
        switch (block.QuestionType)
        {
            case QuestionType.MultipleChoice:
            {
                var selected = optionIds?.ToHashSet() ?? new HashSet<int>();
                var correct = block.Options.Where(option => option.IsCorrect).Select(option => option.Id).ToHashSet();
                return correct.Count > 0 && selected.SetEquals(correct);
            }
            case QuestionType.TrueFalse:
            {
                if (optionIds is not { Count: 1 }) return false;
                var selectedId = optionIds.First();
                return block.Options.Any(option => option.Id == selectedId && option.IsCorrect);
            }
            case QuestionType.TextAnswer:
            {
                return block.Options.Any(option => option.IsCorrect && TextMatches(option.OptionText, textAnswer));
            }
            default:
                return false;
        }
    }
}