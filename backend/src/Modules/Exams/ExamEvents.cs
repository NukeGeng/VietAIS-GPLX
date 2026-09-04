namespace Gplx.Modules.Exams;

public sealed record ExamStarted(
    Guid AttemptId,
    string LicenseClassSlug,
    Guid QuestionBankVersionId,
    Guid ExamBlueprintVersionId,
    Guid RegulationVersionId,
    DateTimeOffset StartedAt,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<Guid> QuestionIds);

public sealed record QuestionAnswered(Guid QuestionId, string OptionId, DateTimeOffset OccurredAt);

public sealed record AnswerChanged(Guid QuestionId, string OptionId, DateTimeOffset OccurredAt);

public sealed record QuestionFlagged(Guid QuestionId, DateTimeOffset OccurredAt);

public sealed record QuestionUnflagged(Guid QuestionId, DateTimeOffset OccurredAt);

public sealed record ExamSubmitted(DateTimeOffset SubmittedAt);

public sealed record ExamScored(
    int Score,
    int CorrectCount,
    int CriticalMistakes,
    bool Passed,
    DateTimeOffset ScoredAt);

public sealed record QuestionScored(
    Guid QuestionId,
    string LicenseClassSlug,
    bool Correct,
    bool CriticalMistake,
    DateTimeOffset ScoredAt);
