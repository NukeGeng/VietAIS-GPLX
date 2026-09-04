using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Marten.Services;
using Marten.Subscriptions;

namespace Gplx.Modules.Learning;

public sealed class QuestionPerformanceDocument
{
    public Guid Id { get; set; }
    public string LicenseClassSlug { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public int CorrectAnswers { get; set; }
    public int IncorrectAnswers { get; set; }
    public int CriticalMistakes { get; set; }
    public DateTimeOffset LastScoredAt { get; set; }
}

public sealed class QuestionPerformanceSubscription : SubscriptionBase
{
    public override async Task<IChangeListener> ProcessEventsAsync(
        EventRange page,
        ISubscriptionController controller,
        IDocumentOperations operations,
        CancellationToken cancellationToken)
    {
        foreach (var item in page.Events)
        {
            if (item.Data is not Gplx.Modules.Exams.QuestionScored scored)
            {
                continue;
            }

            var performance = await operations.LoadAsync<QuestionPerformanceDocument>(scored.QuestionId, cancellationToken)
                ?? new QuestionPerformanceDocument
                {
                    Id = scored.QuestionId,
                    LicenseClassSlug = scored.LicenseClassSlug
                };
            performance.Attempts++;
            if (scored.Correct) performance.CorrectAnswers++;
            else performance.IncorrectAnswers++;
            if (scored.CriticalMistake) performance.CriticalMistakes++;
            performance.LastScoredAt = scored.ScoredAt;
            operations.Store(performance);
        }

        return new NoopChangeListener();
    }

    private sealed class NoopChangeListener : IChangeListener
    {
        public Task BeforeCommitAsync(IDocumentSession session, IChangeSet commit, CancellationToken token) => Task.CompletedTask;

        public Task AfterCommitAsync(IDocumentSession session, IChangeSet commit, CancellationToken token) => Task.CompletedTask;
    }
}
