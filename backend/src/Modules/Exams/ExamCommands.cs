using Gplx.BuildingBlocks;
using Gplx.Modules.QuestionBank;
using Marten;
using Wolverine.Attributes;

namespace Gplx.Modules.Exams;

public sealed record StartExamCommand(string LicenseClassSlug);

public sealed record StartExamResult(ExamAttemptView View);

public sealed record AnswerQuestionCommand(Guid AttemptId, Guid QuestionId, string OptionId);

public sealed record AnswerQuestionResult(ExamAttemptView View, bool Changed);

public sealed record FlagQuestionCommand(Guid AttemptId, Guid QuestionId, bool Flagged);

public sealed record FlagQuestionResult(ExamAttemptView View);

public sealed record SubmitExamCommand(Guid AttemptId);

public sealed record SubmitExamResult(ExamAttemptView View);

[WolverineHandler]
public sealed class ExamCommandHandlers
{
    public static async Task<StartExamResult> Handle(
        StartExamCommand command,
        IDocumentSession session,
        IQuerySession query,
        CancellationToken cancellationToken)
    {
        var licenseClass = await query.Query<LicenseClassDocument>()
            .Where(item => item.Slug == command.LicenseClassSlug)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainRuleViolationException("License class was not found.");

        var questionBank = await query.Query<QuestionBankVersionDocument>()
            .Where(item => item.Status == "Published" && item.LicenseClassSlugs.Contains(command.LicenseClassSlug))
            .OrderByDescending(item => item.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainRuleViolationException("No published question bank is available.");

        var blueprint = await query.Query<ExamBlueprintVersionDocument>()
            .Where(item => item.Status == "Published")
            .OrderByDescending(item => item.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainRuleViolationException("No published exam blueprint is available.");

        var rule = blueprint.Blueprints.SingleOrDefault(item => item.LicenseClassSlug == licenseClass.Slug)
            ?? throw new DomainRuleViolationException("No exam blueprint exists for this license class.");

        if (rule.QuestionBankVersion != questionBank.Version)
        {
            throw new DomainRuleViolationException("Exam blueprint and question bank versions do not match.");
        }

        var candidateQuestions = await query.Query<QuestionDocument>()
            .Where(item => item.LicenseClassSlugs.Contains(licenseClass.Slug) && item.QuestionBankVersion == questionBank.Version)
            .OrderBy(item => item.Slug)
            .ToListAsync(cancellationToken);
        var questions = SelectQuestions(candidateQuestions, rule);

        if (questions.Count < rule.QuestionCount)
        {
            throw new DomainRuleViolationException("The published question bank has fewer questions than the blueprint requires.");
        }

        var regulation = await query.Query<RegulationVersionDocument>()
            .Where(item => item.Status == "Published")
            .OrderByDescending(item => item.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainRuleViolationException("No published regulation is available.");

        var now = DateTimeOffset.UtcNow;
        var attemptId = Guid.NewGuid();
        var started = new ExamStarted(
            attemptId,
            licenseClass.Slug,
            questionBank.Id,
            blueprint.Id,
            regulation.Id,
            now,
            now.AddSeconds(rule.DurationSeconds),
            questions.Select(item => item.Id).ToList());

        session.Events.StartStream<ExamAttempt>(attemptId, started);
        var attempt = ExamAttempt.Rehydrate([started]);
        StoreInlineReadModels(session, attempt, version: 1);
        await session.SaveChangesAsync(cancellationToken);
        return new StartExamResult(ToView(attempt));
    }

    public static async Task<AnswerQuestionResult> Handle(
        AnswerQuestionCommand command,
        IDocumentSession session,
        IQuerySession query,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadAttemptForWriting(session, command.AttemptId, cancellationToken);
        var attempt = loaded.Attempt;
        var question = await query.LoadAsync<QuestionDocument>(command.QuestionId, cancellationToken)
            ?? throw new DomainRuleViolationException("Question was not found.");
        if (!question.Options.Any(option => option.Id == command.OptionId))
        {
            throw new DomainRuleViolationException("Option does not belong to the question.");
        }

        var @event = attempt.AnswerQuestion(command.QuestionId, command.OptionId, DateTimeOffset.UtcNow);
        if (@event is null)
        {
            return new AnswerQuestionResult(ToView(attempt), false);
        }

        await session.Events.AppendOptimistic(command.AttemptId, @event);
        attempt.Apply(@event);
        StoreInlineReadModels(session, attempt, version: loaded.Version + 1);
        await session.SaveChangesAsync(cancellationToken);
        return new AnswerQuestionResult(ToView(attempt), true);
    }

    public static async Task<FlagQuestionResult> Handle(
        FlagQuestionCommand command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadAttemptForWriting(session, command.AttemptId, cancellationToken);
        var attempt = loaded.Attempt;
        var @event = command.Flagged
            ? attempt.FlagQuestion(command.QuestionId, DateTimeOffset.UtcNow)
            : attempt.UnflagQuestion(command.QuestionId, DateTimeOffset.UtcNow);

        await session.Events.AppendOptimistic(command.AttemptId, @event);
        attempt.Apply(@event);
        StoreInlineReadModels(session, attempt, version: loaded.Version + 1);
        await session.SaveChangesAsync(cancellationToken);
        return new FlagQuestionResult(ToView(attempt));
    }

    public static async Task<SubmitExamResult> Handle(
        SubmitExamCommand command,
        IDocumentSession session,
        IQuerySession query,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadAttemptForWriting(session, command.AttemptId, cancellationToken);
        var attempt = loaded.Attempt;
        if (attempt.Status == ExamAttemptStatus.Scored)
        {
            return new SubmitExamResult(ToView(attempt));
        }

        var blueprint = await query.LoadAsync<ExamBlueprintVersionDocument>(attempt.ExamBlueprintVersionId, cancellationToken)
            ?? throw new DomainRuleViolationException("Pinned exam blueprint was not found.");
        var rule = blueprint.Blueprints.SingleOrDefault(item => item.LicenseClassSlug == attempt.LicenseClassSlug)
            ?? throw new DomainRuleViolationException("Pinned exam rule was not found.");
        var questions = await query.Query<QuestionDocument>()
            .Where(item => attempt.QuestionIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        var submitted = attempt.Submit(DateTimeOffset.UtcNow);
        attempt.Apply(submitted);

        var scored = attempt.ScoreExam(
            questions.ToDictionary(item => item.Id, item => item.CorrectOptionId),
            questions.Where(item => item.IsCritical).Select(item => item.Id).ToHashSet(),
            rule.PassingScore,
            rule.MaxCriticalMistakes,
            DateTimeOffset.UtcNow);
        attempt.Apply(scored);
        var events = new List<object> { submitted, scored };
        foreach (var question in questions)
        {
            var answered = attempt.Answers.TryGetValue(question.Id, out var answer);
            var correct = answered && answer == question.CorrectOptionId;
            events.Add(new QuestionScored(
                question.Id,
                attempt.LicenseClassSlug,
                correct,
                question.IsCritical && !correct,
                scored.ScoredAt));
        }
        await session.Events.AppendOptimistic(command.AttemptId, events.ToArray());
        StoreInlineReadModels(session, attempt, version: loaded.Version + events.Count);
        await session.SaveChangesAsync(cancellationToken);
        return new SubmitExamResult(ToView(attempt));
    }

    private static async Task<(ExamAttempt Attempt, long Version)> LoadAttemptForWriting(IDocumentSession session, Guid attemptId, CancellationToken cancellationToken)
    {
        var snapshot = await session.LoadAsync<ExamAttemptSnapshot>(attemptId, cancellationToken);
        if (snapshot is not null && snapshot.Version > 0 && snapshot.QuestionIds.Count > 0)
        {
            var changes = await session.Events.FetchStreamAsync(
                attemptId,
                fromVersion: snapshot.Version + 1,
                token: cancellationToken);
            var attempt = ExamAttempt.FromSnapshot(snapshot);
            foreach (var change in changes)
            {
                attempt.Apply(change.Data);
            }

            return (attempt, snapshot.Version + changes.Count);
        }

        var events = await session.Events.FetchStreamAsync(attemptId, token: cancellationToken);
        return events.Count == 0
            ? throw new DomainRuleViolationException("Exam attempt was not found.")
            : (ExamAttempt.Rehydrate(events.Select(item => item.Data)), events[^1].Version);
    }

    private static void StoreInlineReadModels(IDocumentSession session, ExamAttempt attempt, long version)
    {
        var view = ToView(attempt);
        session.Store(view);
        session.Store(new ExamAttemptSnapshot
        {
            Id = attempt.Id,
            LicenseClassSlug = attempt.LicenseClassSlug,
            QuestionBankVersionId = attempt.QuestionBankVersionId,
            ExamBlueprintVersionId = attempt.ExamBlueprintVersionId,
            RegulationVersionId = attempt.RegulationVersionId,
            StartedAt = attempt.StartedAt,
            ExpiresAt = attempt.ExpiresAt,
            Status = attempt.Status.ToString(),
            AnsweredCount = attempt.Answers.Count,
            FlaggedCount = attempt.FlaggedQuestionIds.Count,
            Score = attempt.Score,
            CorrectCount = attempt.CorrectCount,
            CriticalMistakes = attempt.CriticalMistakes,
            Passed = attempt.Passed,
            Version = version,
            QuestionIds = attempt.QuestionIds,
            Answers = new Dictionary<Guid, string>(attempt.Answers),
            FlaggedQuestionIds = new HashSet<Guid>(attempt.FlaggedQuestionIds)
        });
    }

    public static ExamAttemptView ToView(ExamAttempt attempt) => new()
    {
        Id = attempt.Id,
        LicenseClassSlug = attempt.LicenseClassSlug,
        QuestionBankVersionId = attempt.QuestionBankVersionId,
        ExamBlueprintVersionId = attempt.ExamBlueprintVersionId,
        RegulationVersionId = attempt.RegulationVersionId,
        StartedAt = attempt.StartedAt,
        ExpiresAt = attempt.ExpiresAt,
        Status = attempt.Status.ToString(),
        QuestionIds = attempt.QuestionIds,
        Answers = new Dictionary<Guid, string>(attempt.Answers),
        FlaggedQuestionIds = new HashSet<Guid>(attempt.FlaggedQuestionIds),
        Score = attempt.Score,
        CorrectCount = attempt.CorrectCount,
        CriticalMistakes = attempt.CriticalMistakes,
        Passed = attempt.Passed
    };

    private static List<QuestionDocument> SelectQuestions(
        IReadOnlyList<QuestionDocument> candidates,
        ExamBlueprintRule rule)
    {
        var selected = new List<QuestionDocument>(rule.QuestionCount);
        var selectedIds = new HashSet<Guid>();

        void Add(IEnumerable<QuestionDocument> items, int count)
        {
            foreach (var item in items
                .OrderBy(_ => Random.Shared.Next())
                .Where(item => selectedIds.Add(item.Id))
                .Take(Math.Max(0, count)))
            {
                selected.Add(item);
            }
        }

        Add(candidates.Where(item => item.IsCritical), rule.CriticalQuestionCount);

        foreach (var topicRule in rule.TopicQuestionCounts)
        {
            var alreadySelected = selected.Count(item => item.Topic == topicRule.Key && !item.IsCritical);
            Add(
                candidates.Where(item => item.Topic == topicRule.Key && !item.IsCritical),
                topicRule.Value - alreadySelected);
        }

        Add(candidates, rule.QuestionCount - selected.Count);
        return selected;
    }
}
