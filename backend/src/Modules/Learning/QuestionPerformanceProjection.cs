using Gplx.Modules.Exams;
using Marten.Events.Projections;

namespace Gplx.Modules.Learning;

public sealed class QuestionPerformanceReadModel
{
    public Guid Id { get; set; }
    public string LicenseClassSlug { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public int CorrectAnswers { get; set; }
    public int IncorrectAnswers { get; set; }
    public int CriticalMistakes { get; set; }
    public DateTimeOffset LastScoredAt { get; set; }
}

public sealed class QuestionPerformanceProjection : MultiStreamProjection<QuestionPerformanceReadModel, Guid>
{
    public QuestionPerformanceProjection()
    {
        Identity<QuestionScored>(scored => scored.QuestionId);
    }

    public QuestionPerformanceReadModel Create(QuestionScored scored) => new()
    {
        Id = scored.QuestionId,
        LicenseClassSlug = scored.LicenseClassSlug,
        Attempts = 1,
        CorrectAnswers = scored.Correct ? 1 : 0,
        IncorrectAnswers = scored.Correct ? 0 : 1,
        CriticalMistakes = scored.CriticalMistake ? 1 : 0,
        LastScoredAt = scored.ScoredAt
    };

    public void Apply(QuestionScored scored, QuestionPerformanceReadModel current)
    {
        current.Attempts++;
        if (scored.Correct) current.CorrectAnswers++;
        else current.IncorrectAnswers++;
        if (scored.CriticalMistake) current.CriticalMistakes++;
        if (scored.ScoredAt > current.LastScoredAt) current.LastScoredAt = scored.ScoredAt;
    }
}
