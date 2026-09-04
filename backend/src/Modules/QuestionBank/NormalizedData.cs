using System.Text.Json;
using System.Text.Json.Serialization;
using Marten;

namespace Gplx.Modules.QuestionBank;

public sealed class NormalizedDataSeeder(IConfiguration configuration, IHostEnvironment environment)
{
    private static readonly Guid QuestionBankId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid BlueprintId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid RegulationId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid LicenseBId = Guid.Parse("10000000-0000-0000-0000-000000000101");
    private static readonly Guid LicenseC1Id = Guid.Parse("10000000-0000-0000-0000-000000000102");

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task SeedAsync(IDocumentSession session, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Seed:Enabled"))
        {
            return;
        }

        var root = ResolveDataRoot();
        var classes = await ReadAsync<NormalizedLicenseClasses>(Path.Combine(root, "license-classes.json"), cancellationToken);
        var regulation = await ReadAsync<NormalizedRegulation>(Path.Combine(root, "regulations/v1.json"), cancellationToken);
        var blueprint = await ReadAsync<NormalizedBlueprint>(Path.Combine(root, "exam-blueprints/v1.json"), cancellationToken);
        var bank = await ReadAsync<NormalizedQuestionBank>(Path.Combine(root, "question-banks/v1.json"), cancellationToken);

        foreach (var item in classes.LicenseClasses)
        {
            session.Store(new LicenseClassDocument
            {
                Id = item.Slug == "b" ? LicenseBId : LicenseC1Id,
                Slug = item.Slug,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                Source = item.Source
            });
        }

        session.Store(new QuestionBankVersionDocument
        {
            Id = QuestionBankId,
            Version = bank.Version,
            Status = "Published",
            EffectiveFrom = bank.EffectiveFrom,
            LicenseClassSlugs = bank.LicenseClassSlugs,
            Source = bank.Source
        });

        var validQuestionIds = bank.Questions.Select(item => Guid.Parse(item.Id)).ToHashSet();
        var existingQuestions = await session.Query<QuestionDocument>().ToListAsync(cancellationToken);
        foreach (var staleQuestion in existingQuestions.Where(item => !validQuestionIds.Contains(item.Id)))
        {
            session.Delete(staleQuestion);
        }

        foreach (var question in bank.Questions)
        {
            session.Store(new QuestionDocument
            {
                Id = Guid.Parse(question.Id),
                Slug = question.Slug,
                LicenseClassSlug = question.LicenseClassSlugs.FirstOrDefault() ?? bank.LicenseClassSlugs.FirstOrDefault() ?? "b",
                LicenseClassSlugs = question.LicenseClassSlugs.Count > 0 ? question.LicenseClassSlugs : bank.LicenseClassSlugs,
                Topic = question.Topic,
                Text = question.Text,
                Options = question.Options,
                CorrectOptionId = question.CorrectOptionId,
                IsCritical = question.IsCritical,
                Explanation = question.Explanation,
                MemoryTip = question.MemoryTip,
                QuestionBankVersion = bank.Version,
                Source = question.Source
            });
        }

        session.Store(new RegulationVersionDocument
        {
            Id = RegulationId,
            Version = regulation.Version,
            Status = "Published",
            Title = regulation.Title,
            Summary = regulation.Summary,
            EffectiveFrom = regulation.EffectiveFrom,
            Source = regulation.Source
        });

        session.Store(new ExamBlueprintVersionDocument
        {
            Id = BlueprintId,
            Version = blueprint.Version,
            Status = "Published",
            EffectiveFrom = blueprint.EffectiveFrom,
            Blueprints = blueprint.Blueprints,
            Source = blueprint.Source
        });

        await session.SaveChangesAsync(cancellationToken);
    }

    private string ResolveDataRoot()
    {
        var configured = configuration["Seed:DataRoot"];
        if (Path.IsPathRooted(configured))
        {
            return configured;
        }

        var contentRoot = environment.ContentRootPath;
        return Path.GetFullPath(Path.Combine(contentRoot, configured ?? "../../../../data/normalized"));
    }

    private async Task<T> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken)
            ?? throw new InvalidDataException($"Normalized data file is empty: {path}");
    }
}

public sealed class NormalizedSource
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? RetrievedFrom { get; set; }
    public DateOnly RetrievedAt { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public string? Sha256 { get; set; }
}

public sealed class NormalizedLicenseClass
{
    public string Slug { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SourceProvenance Source { get; set; } = new();
}

public sealed class NormalizedLicenseClasses
{
    public string SchemaVersion { get; set; } = string.Empty;
    public string BundleVersion { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<NormalizedLicenseClass> LicenseClasses { get; set; } = [];
}

public sealed class NormalizedQuestion
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public List<string> LicenseClassSlugs { get; set; } = [];
    public string Topic { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<QuestionOptionDocument> Options { get; set; } = [];
    public string CorrectOptionId { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string? MemoryTip { get; set; }
    public SourceProvenance Source { get; set; } = new();
}

public sealed class NormalizedQuestionBank
{
    public string SchemaVersion { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public List<string> LicenseClassSlugs { get; set; } = [];
    public List<NormalizedQuestion> Questions { get; set; } = [];
    public SourceProvenance Source { get; set; } = new();
}

public sealed class NormalizedRegulation
{
    public string SchemaVersion { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public SourceProvenance Source { get; set; } = new();
}

public sealed class NormalizedBlueprintRule
{
    public string LicenseClassSlug { get; set; } = string.Empty;
    public string QuestionBankVersion { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int DurationSeconds { get; set; }
    public int PassingScore { get; set; }
    public int MaxCriticalMistakes { get; set; }
    public int CriticalQuestionCount { get; set; }
    public Dictionary<string, int> TopicQuestionCounts { get; set; } = [];
}

public sealed class NormalizedBlueprint
{
    public string SchemaVersion { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public List<ExamBlueprintRule> Blueprints { get; set; } = [];
    public SourceProvenance Source { get; set; } = new();
}
