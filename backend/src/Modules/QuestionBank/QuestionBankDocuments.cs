namespace Gplx.Modules.QuestionBank;

public sealed class SourceProvenance
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? RetrievedFrom { get; set; }
    public DateOnly RetrievedAt { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public string? Sha256 { get; set; }
    public int? SourcePage { get; set; }
}

public sealed class LicenseClassDocument
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SourceProvenance Source { get; set; } = new();
}

public sealed class QuestionOptionDocument
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public sealed class QuestionDocument
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string LicenseClassSlug { get; set; } = string.Empty;
    public IReadOnlyList<string> LicenseClassSlugs { get; set; } = [];
    public string Topic { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public IReadOnlyList<QuestionOptionDocument> Options { get; set; } = [];
    public string CorrectOptionId { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string? MemoryTip { get; set; }
    public string QuestionBankVersion { get; set; } = string.Empty;
    public SourceProvenance Source { get; set; } = new();
}

public sealed class QuestionBankVersionDocument
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateOnly EffectiveFrom { get; set; }
    public IReadOnlyList<string> LicenseClassSlugs { get; set; } = [];
    public SourceProvenance Source { get; set; } = new();
}

public sealed class RegulationVersionDocument
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public SourceProvenance Source { get; set; } = new();
}

public sealed class ExamBlueprintRule
{
    public string LicenseClassSlug { get; set; } = string.Empty;
    public string QuestionBankVersion { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int DurationSeconds { get; set; }
    public int PassingScore { get; set; }
    public int MaxCriticalMistakes { get; set; }
    public int CriticalQuestionCount { get; set; }
    public IReadOnlyDictionary<string, int> TopicQuestionCounts { get; set; } =
        new Dictionary<string, int>();
}

public sealed class ExamBlueprintVersionDocument
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateOnly EffectiveFrom { get; set; }
    public IReadOnlyList<ExamBlueprintRule> Blueprints { get; set; } = [];
    public SourceProvenance Source { get; set; } = new();
}

public sealed class ExamAttemptSnapshot
{
    public Guid Id { get; set; }
    public string LicenseClassSlug { get; set; } = string.Empty;
    public Guid QuestionBankVersionId { get; set; }
    public Guid ExamBlueprintVersionId { get; set; }
    public Guid RegulationVersionId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AnsweredCount { get; set; }
    public int FlaggedCount { get; set; }
    public int? Score { get; set; }
    public int? CorrectCount { get; set; }
    public int? CriticalMistakes { get; set; }
    public bool? Passed { get; set; }
    public long Version { get; set; }
    public IReadOnlyList<Guid> QuestionIds { get; set; } = [];
    public IReadOnlyDictionary<Guid, string> Answers { get; set; } = new Dictionary<Guid, string>();
    public IReadOnlyCollection<Guid> FlaggedQuestionIds { get; set; } = [];
}

public sealed class ExamAttemptView
{
    public Guid Id { get; set; }
    public string LicenseClassSlug { get; set; } = string.Empty;
    public Guid QuestionBankVersionId { get; set; }
    public Guid ExamBlueprintVersionId { get; set; }
    public Guid RegulationVersionId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<Guid> QuestionIds { get; set; } = [];
    public IReadOnlyDictionary<Guid, string> Answers { get; set; } = new Dictionary<Guid, string>();
    public IReadOnlyCollection<Guid> FlaggedQuestionIds { get; set; } = [];
    public int? Score { get; set; }
    public int? CorrectCount { get; set; }
    public int? CriticalMistakes { get; set; }
    public bool? Passed { get; set; }
}
