using System.Text;
using System.Text.RegularExpressions;
using TaskPilot.Domain.AI;
using TaskPilot.Domain.Entities;
using TaskPilot.Domain.Policies;

namespace TaskPilot.Application.Features.Ai;

public sealed partial class AiInputGuard : IAiInputGuard
{
    public const int MaximumInputCharacters = 4_000;

    public AiGuardResult Validate(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length > MaximumInputCharacters)
            return AiGuardResult.Deny("AI input must be between 1 and 4000 characters.");

        var normalized = input.Normalize(NormalizationForm.FormKC);
        if (normalized.Any(character => char.IsControl(character) && character is not '\r' and not '\n' and not '\t'))
            return AiGuardResult.Deny("AI input contains unsupported control characters.");
        if (PromptInjectionPattern().IsMatch(normalized))
            return AiGuardResult.Deny("AI input contains an instruction-override pattern.");
        if (PiiPattern().IsMatch(normalized))
            return AiGuardResult.Deny("AI input must not contain email addresses, phone numbers, or payment card numbers.");
        return AiGuardResult.Allow();
    }

    internal static AiGuardResult ValidateGeneratedText(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC);
        if (normalized.Any(char.IsControl) || PromptInjectionPattern().IsMatch(normalized))
            return AiGuardResult.Deny("Generated content failed the safety policy.");
        if (PiiPattern().IsMatch(normalized))
            return AiGuardResult.Deny("Generated content failed the privacy policy.");
        return AiGuardResult.Allow();
    }

    [GeneratedRegex(@"\b(?:ignore|disregard|override|forget)\s+(?:all\s+)?(?:previous|prior|system|developer)\s+(?:instructions?|prompts?|messages?)\b|\byou\s+are\s+now\b|\b(?:reveal|print|repeat|expose)\s+(?:the\s+)?(?:system|developer|hidden)\s+(?:prompt|instructions?|message)\b|\b(?:jailbreak|do\s+anything\s+now)\b|<\s*/?\s*system\b|\[\s*inst\s*\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex PromptInjectionPattern();

    [GeneratedRegex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b|\b(?:\+?\d[\s().-]?){10,15}\b|\b(?:\d[ -]?){13,19}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex PiiPattern();
}

public sealed class AiOutputGuard : IAiOutputGuard
{
    public const int MaximumTitleCharacters = TaskCreationPolicy.MaximumTitleCharacters;
    public const int MaximumLabels = 10;
    public const int MaximumLabelCharacters = 50;
    public const int MaximumSubtasks = 20;
    public const int MaximumSubtaskCharacters = 200;
    public const int MaximumTotalCharacters = 4_000;

    public AiGuardResult Validate(AiTaskSuggestion suggestion)
    {
        if (suggestion is null || string.IsNullOrWhiteSpace(suggestion.Title) || suggestion.Title.Length > MaximumTitleCharacters)
            return AiGuardResult.Deny("Generated title exceeds the allowed size.");
        if (suggestion.Labels is null || suggestion.Labels.Count > MaximumLabels ||
            suggestion.Labels.Any(label => string.IsNullOrWhiteSpace(label) || label.Length > MaximumLabelCharacters))
            return AiGuardResult.Deny("Generated labels exceed the allowed size.");
        if (suggestion.Subtasks is null || suggestion.Subtasks.Count > MaximumSubtasks ||
            suggestion.Subtasks.Any(subtask => string.IsNullOrWhiteSpace(subtask) || subtask.Length > MaximumSubtaskCharacters))
            return AiGuardResult.Deny("Generated subtasks exceed the allowed size.");
        if (suggestion.Priority is not null && !Enum.TryParse<TaskItemPriority>(suggestion.Priority, true, out _))
            return AiGuardResult.Deny("Generated priority is invalid.");

        var fields = new[] { suggestion.Title }
            .Concat(suggestion.Labels)
            .Concat(suggestion.Subtasks);
        var totalCharacters = fields.Sum(field => field.Length);
        if (totalCharacters > MaximumTotalCharacters)
            return AiGuardResult.Deny("Generated content exceeds the allowed size.");

        foreach (var field in fields)
        {
            var result = AiInputGuard.ValidateGeneratedText(field);
            if (!result.IsAllowed) return result;
        }

        return AiGuardResult.Allow();
    }
}
