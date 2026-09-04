using Gplx.BuildingBlocks;
using Gplx.Modules.Exams;
using Xunit;

namespace Gplx.Api.Tests;

public sealed class ExamAttemptTests
{
    private static readonly Guid QuestionOne = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid QuestionTwo = Guid.Parse("20000000-0000-0000-0000-000000000002");

    [Fact]
    public void Answering_same_option_does_not_create_a_new_event()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var attempt = ExamAttempt.Rehydrate([new ExamStarted(
            Guid.NewGuid(), "b", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), startedAt, startedAt.AddMinutes(10), [QuestionOne])]);
        attempt.Apply(new QuestionAnswered(QuestionOne, "a", startedAt.AddSeconds(1)));

        var @event = attempt.AnswerQuestion(QuestionOne, "a", startedAt.AddSeconds(2));

        Assert.Null(@event);
    }

    [Fact]
    public void Cannot_answer_after_expiry()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var attempt = ExamAttempt.Rehydrate([new ExamStarted(
            Guid.NewGuid(), "b", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), startedAt, startedAt.AddMinutes(1), [QuestionOne])]);

        Assert.Throws<DomainRuleViolationException>(() => attempt.AnswerQuestion(QuestionOne, "a", startedAt.AddMinutes(1)));
    }

    [Fact]
    public void Unanswered_critical_question_makes_exam_fail()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var attempt = ExamAttempt.Rehydrate([new ExamStarted(
            Guid.NewGuid(), "b", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), startedAt, startedAt.AddMinutes(10), [QuestionOne, QuestionTwo])]);
        attempt.Apply(new QuestionAnswered(QuestionOne, "a", startedAt.AddSeconds(1)));
        attempt.Apply(attempt.Submit(startedAt.AddSeconds(2)));

        var scored = attempt.ScoreExam(
            new Dictionary<Guid, string> { [QuestionOne] = "a", [QuestionTwo] = "a" },
            new HashSet<Guid> { QuestionTwo },
            passingScore: 1,
            maxCriticalMistakes: 0,
            startedAt.AddSeconds(3));

        Assert.Equal(1, scored.CorrectCount);
        Assert.Equal(1, scored.CriticalMistakes);
        Assert.False(scored.Passed);
    }

    [Fact]
    public void Flag_and_unflag_are_represented_by_distinct_events()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var attempt = ExamAttempt.Rehydrate([new ExamStarted(
            Guid.NewGuid(), "b", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), startedAt, startedAt.AddMinutes(10), [QuestionOne])]);

        var flagged = attempt.FlagQuestion(QuestionOne, startedAt.AddSeconds(1));
        attempt.Apply(flagged);
        var unflagged = attempt.UnflagQuestion(QuestionOne, startedAt.AddSeconds(2));
        attempt.Apply(unflagged);

        Assert.Empty(attempt.FlaggedQuestionIds);
    }
}
