using Gplx.BuildingBlocks;
using Gplx.Modules.QuestionBank;

namespace Gplx.Modules.Exams;

public enum ExamAttemptStatus
{
    InProgress,
    Submitted,
    Scored
}

public sealed class ExamAttempt
{
    private readonly Dictionary<Guid, string> _answers = [];
    private readonly HashSet<Guid> _flaggedQuestionIds = [];

    public Guid Id { get; private set; }
    public string LicenseClassSlug { get; private set; } = string.Empty;
    public Guid QuestionBankVersionId { get; private set; }
    public Guid ExamBlueprintVersionId { get; private set; }
    public Guid RegulationVersionId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public ExamAttemptStatus Status { get; private set; }
    public IReadOnlyList<Guid> QuestionIds { get; private set; } = [];
    public IReadOnlyDictionary<Guid, string> Answers => _answers;
    public IReadOnlySet<Guid> FlaggedQuestionIds => _flaggedQuestionIds;
    public int? Score { get; private set; }
    public int? CorrectCount { get; private set; }
    public int? CriticalMistakes { get; private set; }
    public bool? Passed { get; private set; }

    public static ExamAttempt Rehydrate(IEnumerable<object> events)
    {
        var attempt = new ExamAttempt();
        foreach (var @event in events)
        {
            attempt.Apply(@event);
        }

        return attempt;
    }

    public static ExamAttempt FromSnapshot(ExamAttemptSnapshot snapshot)
    {
        var attempt = new ExamAttempt
        {
            Id = snapshot.Id,
            LicenseClassSlug = snapshot.LicenseClassSlug,
            QuestionBankVersionId = snapshot.QuestionBankVersionId,
            ExamBlueprintVersionId = snapshot.ExamBlueprintVersionId,
            RegulationVersionId = snapshot.RegulationVersionId,
            StartedAt = snapshot.StartedAt,
            ExpiresAt = snapshot.ExpiresAt,
            Status = Enum.Parse<ExamAttemptStatus>(snapshot.Status, ignoreCase: false),
            QuestionIds = snapshot.QuestionIds,
            Score = snapshot.Score,
            Passed = snapshot.Passed,
            CorrectCount = snapshot.CorrectCount,
            CriticalMistakes = snapshot.CriticalMistakes
        };
        foreach (var answer in snapshot.Answers)
        {
            attempt._answers[answer.Key] = answer.Value;
        }
        foreach (var questionId in snapshot.FlaggedQuestionIds)
        {
            attempt._flaggedQuestionIds.Add(questionId);
        }

        return attempt;
    }

    public void Apply(object @event)
    {
        switch (@event)
        {
            case ExamStarted started:
                Id = started.AttemptId;
                LicenseClassSlug = started.LicenseClassSlug;
                QuestionBankVersionId = started.QuestionBankVersionId;
                ExamBlueprintVersionId = started.ExamBlueprintVersionId;
                RegulationVersionId = started.RegulationVersionId;
                StartedAt = started.StartedAt;
                ExpiresAt = started.ExpiresAt;
                QuestionIds = started.QuestionIds;
                Status = ExamAttemptStatus.InProgress;
                break;
            case QuestionAnswered answered:
                _answers[answered.QuestionId] = answered.OptionId;
                break;
            case AnswerChanged changed:
                _answers[changed.QuestionId] = changed.OptionId;
                break;
            case QuestionFlagged flagged:
                _flaggedQuestionIds.Add(flagged.QuestionId);
                break;
            case QuestionUnflagged unflagged:
                _flaggedQuestionIds.Remove(unflagged.QuestionId);
                break;
            case ExamSubmitted:
                Status = ExamAttemptStatus.Submitted;
                break;
            case ExamScored scored:
                Status = ExamAttemptStatus.Scored;
                Score = scored.Score;
                CorrectCount = scored.CorrectCount;
                CriticalMistakes = scored.CriticalMistakes;
                Passed = scored.Passed;
                break;
            default:
                throw new InvalidOperationException($"Unsupported exam event: {@event.GetType().Name}");
        }
    }

    public object? AnswerQuestion(Guid questionId, string optionId, DateTimeOffset now)
    {
        EnsureInProgress(now);
        EnsureQuestion(questionId);
        if (_answers.TryGetValue(questionId, out var currentOptionId))
        {
            return currentOptionId == optionId ? null : new AnswerChanged(questionId, optionId, now);
        }

        return new QuestionAnswered(questionId, optionId, now);
    }

    public object FlagQuestion(Guid questionId, DateTimeOffset now)
    {
        EnsureInProgress(now);
        EnsureQuestion(questionId);
        return _flaggedQuestionIds.Contains(questionId)
            ? throw new DomainRuleViolationException("Question is already flagged.")
            : new QuestionFlagged(questionId, now);
    }

    public object UnflagQuestion(Guid questionId, DateTimeOffset now)
    {
        EnsureInProgress(now);
        EnsureQuestion(questionId);
        return !_flaggedQuestionIds.Contains(questionId)
            ? throw new DomainRuleViolationException("Question is not flagged.")
            : new QuestionUnflagged(questionId, now);
    }

    public ExamSubmitted Submit(DateTimeOffset now)
    {
        EnsureInProgress(now);
        return new ExamSubmitted(now);
    }

    public ExamScored ScoreExam(
        IReadOnlyDictionary<Guid, string> correctOptionByQuestion,
        IReadOnlySet<Guid> criticalQuestionIds,
        int passingScore,
        int maxCriticalMistakes,
        DateTimeOffset now)
    {
        if (Status != ExamAttemptStatus.Submitted)
        {
            throw new DomainRuleViolationException("Only a submitted exam can be scored.");
        }

        var correctCount = _answers.Count(pair => correctOptionByQuestion.TryGetValue(pair.Key, out var correct) && correct == pair.Value);
        var criticalMistakes = criticalQuestionIds.Count(questionId =>
            !_answers.TryGetValue(questionId, out var answer)
            || !correctOptionByQuestion.TryGetValue(questionId, out var correct)
            || correct != answer);
        var passed = correctCount >= passingScore && criticalMistakes <= maxCriticalMistakes;
        return new ExamScored(correctCount, correctCount, criticalMistakes, passed, now);
    }

    public void EnsureInProgress(DateTimeOffset now)
    {
        if (Status != ExamAttemptStatus.InProgress)
        {
            throw new DomainRuleViolationException("Exam is no longer in progress.");
        }

        if (now >= ExpiresAt)
        {
            throw new DomainRuleViolationException("Exam time has expired.");
        }
    }

    private void EnsureQuestion(Guid questionId)
    {
        if (!QuestionIds.Contains(questionId))
        {
            throw new DomainRuleViolationException("Question does not belong to this exam.");
        }
    }
}
