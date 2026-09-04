using Gplx.BuildingBlocks;
using Marten;
using Wolverine.Attributes;
using System.Security.Cryptography;
using System.Text;

namespace Gplx.Modules.QuestionBank;

public sealed record AdminQuestionInput(
    Guid Id,
    string Slug,
    IReadOnlyList<string> LicenseClassSlugs,
    string Topic,
    string Text,
    IReadOnlyList<QuestionOptionDocument> Options,
    string CorrectOptionId,
    bool IsCritical,
    string Explanation,
    string? MemoryTip);

public sealed record ImportQuestionBankCommand(
    string Version,
    DateOnly EffectiveFrom,
    IReadOnlyList<string> LicenseClassSlugs,
    IReadOnlyList<AdminQuestionInput> Questions,
    SourceProvenance Source);

public sealed record ImportQuestionBankResult(
    Guid Id,
    string Version,
    string Status,
    int QuestionCount);

public sealed record EditQuestionCommand(Guid QuestionBankVersionId, Guid QuestionId, AdminQuestionInput Question);
public sealed record SaveLicenseClassCommand(
    Guid? Id,
    string Slug,
    string Code,
    string Name,
    string Description,
    SourceProvenance Source);
public sealed record SaveLicenseClassResult(LicenseClassDocument LicenseClass);

public sealed record PublishQuestionBankVersionCommand(Guid Id);
public sealed record DeprecateQuestionBankVersionCommand(Guid Id);

public sealed record SaveRegulationVersionCommand(
    Guid? Id,
    string Version,
    string Title,
    string Summary,
    DateOnly EffectiveFrom,
    SourceProvenance Source);

public sealed record SaveRegulationVersionResult(RegulationVersionDocument Version);
public sealed record PublishRegulationVersionCommand(Guid Id);

public sealed record SaveExamBlueprintVersionCommand(
    Guid? Id,
    string Version,
    DateOnly EffectiveFrom,
    IReadOnlyList<ExamBlueprintRule> Blueprints,
    SourceProvenance Source);

public sealed record SaveExamBlueprintVersionResult(ExamBlueprintVersionDocument Version);
public sealed record PublishExamBlueprintVersionCommand(Guid Id);

[WolverineHandler]
public sealed class AdminVersionCommandHandlers
{
    public static async Task<ImportQuestionBankResult> Handle(
        ImportQuestionBankCommand command,
        IDocumentSession session,
        IQuerySession query,
        CancellationToken cancellationToken)
    {
        var licenseClassSlugs = NormalizeSlugs(command.LicenseClassSlugs);
        var errors = ValidateSource(command.Source);
        errors.AddRange(await ValidateLicenseClasses(query, licenseClassSlugs, cancellationToken));
        errors.AddRange(ValidateQuestionInputs(command, licenseClassSlugs));
        if (errors.Count > 0)
        {
            throw new DomainRuleViolationException(string.Join(" ", errors));
        }

        var duplicate = await query.Query<QuestionBankVersionDocument>()
            .Where(item => item.Version == command.Version.Trim())
            .AnyAsync(cancellationToken);
        if (duplicate)
        {
            throw new DomainRuleViolationException("A question bank with this version already exists.");
        }

        var version = new QuestionBankVersionDocument
        {
            Id = Guid.NewGuid(),
            Version = command.Version.Trim(),
            Status = "Validated",
            EffectiveFrom = command.EffectiveFrom,
            LicenseClassSlugs = licenseClassSlugs,
            Source = command.Source
        };
        session.Store(version);

        foreach (var input in command.Questions)
        {
            session.Store(ToDocument(input, version.Version, licenseClassSlugs, command.Source));
        }

        await session.SaveChangesAsync(cancellationToken);
        return new ImportQuestionBankResult(version.Id, version.Version, version.Status, command.Questions.Count);
    }

    public static async Task<QuestionDocument> Handle(
        EditQuestionCommand command,
        IDocumentSession session,
        IQuerySession query,
        CancellationToken cancellationToken)
    {
        var version = await query.LoadAsync<QuestionBankVersionDocument>(command.QuestionBankVersionId, cancellationToken)
            ?? throw new DomainRuleViolationException("Question bank version was not found.");
        if (version.Status == "Published")
        {
            throw new DomainRuleViolationException("Published question banks are immutable; import a new version.");
        }
        if (command.Question.Id != command.QuestionId)
        {
            throw new DomainRuleViolationException("Question id does not match the route.");
        }

        var errors = ValidateQuestionInputs(
            new ImportQuestionBankCommand(version.Version, version.EffectiveFrom, version.LicenseClassSlugs, [command.Question], version.Source),
            version.LicenseClassSlugs);
        if (errors.Count > 0) throw new DomainRuleViolationException(string.Join(" ", errors));

        var duplicateSlug = await query.Query<QuestionDocument>()
            .Where(item => item.QuestionBankVersion == version.Version && item.Id != command.QuestionId && item.Slug == command.Question.Slug.Trim())
            .AnyAsync(cancellationToken);
        if (duplicateSlug) throw new DomainRuleViolationException("Question slug must be unique within the version.");

        var question = await query.Query<QuestionDocument>()
            .Where(item => item.QuestionBankVersion == version.Version && item.Id == command.QuestionId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainRuleViolationException("Question was not found in the selected version.");

        question.Slug = command.Question.Slug.Trim();
        question.LicenseClassSlug = command.Question.LicenseClassSlugs.FirstOrDefault() ?? version.LicenseClassSlugs[0];
        question.LicenseClassSlugs = command.Question.LicenseClassSlugs;
        question.Topic = command.Question.Topic.Trim();
        question.Text = command.Question.Text.Trim();
        question.Options = command.Question.Options;
        question.CorrectOptionId = command.Question.CorrectOptionId.Trim();
        question.IsCritical = command.Question.IsCritical;
        question.Explanation = command.Question.Explanation.Trim();
        question.MemoryTip = string.IsNullOrWhiteSpace(command.Question.MemoryTip) ? null : command.Question.MemoryTip.Trim();
        session.Store(question);
        await session.SaveChangesAsync(cancellationToken);
        return question;
    }

    public static async Task<SaveLicenseClassResult> Handle(
        SaveLicenseClassCommand command,
        IDocumentSession session,
        IQuerySession query,
        CancellationToken cancellationToken)
    {
        var slug = command.Slug.Trim().ToLowerInvariant();
        var errors = ValidateSource(command.Source);
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("License class slug, code and name are required.");
        }

        LicenseClassDocument licenseClass;
        if (command.Id is null)
        {
            var duplicate = await query.Query<LicenseClassDocument>().Where(item => item.Slug == slug).AnyAsync(cancellationToken);
            if (duplicate) throw new DomainRuleViolationException("A license class with this slug already exists.");
            licenseClass = new LicenseClassDocument { Id = Guid.NewGuid() };
        }
        else
        {
            licenseClass = await query.LoadAsync<LicenseClassDocument>(command.Id.Value, cancellationToken)
                ?? throw new DomainRuleViolationException("License class was not found.");
            if (!string.Equals(licenseClass.Slug, slug, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("A license class slug cannot be changed after creation.");
            }
        }

        if (errors.Count > 0) throw new DomainRuleViolationException(string.Join(" ", errors));
        licenseClass.Slug = slug;
        licenseClass.Code = command.Code.Trim();
        licenseClass.Name = command.Name.Trim();
        licenseClass.Description = command.Description.Trim();
        licenseClass.Source = command.Source;
        session.Store(licenseClass);
        await session.SaveChangesAsync(cancellationToken);
        return new SaveLicenseClassResult(licenseClass);
    }

    public static async Task<QuestionBankVersionDocument> Handle(
        PublishQuestionBankVersionCommand command,
        IDocumentSession session,
        IQuerySession query,
        CancellationToken cancellationToken)
    {
        var version = await query.LoadAsync<QuestionBankVersionDocument>(command.Id, cancellationToken)
            ?? throw new DomainRuleViolationException("Question bank version was not found.");
        var questions = await query.Query<QuestionDocument>()
            .Where(item => item.QuestionBankVersion == version.Version)
            .ToListAsync(cancellationToken);
        var errors = ValidateQuestionBank(version, questions);
        if (errors.Count > 0)
        {
            throw new DomainRuleViolationException(string.Join(" ", errors));
        }

        var previousVersions = await query.Query<QuestionBankVersionDocument>()
            .Where(item => item.Status == "Published" && item.Id != version.Id)
            .ToListAsync(cancellationToken);
        foreach (var previous in previousVersions)
        {
            previous.Status = "Deprecated";
            session.Store(previous);
        }

        version.Status = "Published";
        session.Store(version);
        await session.SaveChangesAsync(cancellationToken);
        return version;
    }

    public static async Task<QuestionBankVersionDocument> Handle(
        DeprecateQuestionBankVersionCommand command,
        IDocumentSession session,
        IQuerySession query,
        CancellationToken cancellationToken)
    {
        var version = await query.LoadAsync<QuestionBankVersionDocument>(command.Id, cancellationToken)
            ?? throw new DomainRuleViolationException("Question bank version was not found.");
        if (version.Status == "Published")
        {
            throw new DomainRuleViolationException("A published question bank must be replaced by publishing another version.");
        }

        version.Status = "Deprecated";
        session.Store(version);
        await session.SaveChangesAsync(cancellationToken);
        return version;
    }

    public static async Task<SaveRegulationVersionResult> Handle(
        SaveRegulationVersionCommand command,
        IDocumentSession session,
        IQuerySession query,
        CancellationToken cancellationToken)
    {
        var errors = ValidateSource(command.Source);
        if (string.IsNullOrWhiteSpace(command.Version) || string.IsNullOrWhiteSpace(command.Title))
        {
            errors.Add("Regulation version and title are required.");
        }

        RegulationVersionDocument version;
        if (command.Id is null)
        {
            var duplicate = await query.Query<RegulationVersionDocument>()
                .Where(item => item.Version == command.Version.Trim())
                .AnyAsync(cancellationToken);
            if (duplicate) throw new DomainRuleViolationException("A regulation with this version already exists.");
            version = new RegulationVersionDocument { Id = Guid.NewGuid() };
        }
        else
        {
            version = await query.LoadAsync<RegulationVersionDocument>(command.Id.Value, cancellationToken)
                ?? throw new DomainRuleViolationException("Regulation version was not found.");
            if (version.Status == "Published")
            {
                throw new DomainRuleViolationException("Published regulations are immutable; create a new version.");
            }
        }

        if (errors.Count > 0)
        {
            throw new DomainRuleViolationException(string.Join(" ", errors));
        }

        version.Version = command.Version.Trim();
        version.Title = command.Title.Trim();
        version.Summary = command.Summary.Trim();
        version.EffectiveFrom = command.EffectiveFrom;
        version.Source = command.Source;
        version.Status = "Draft";
        session.Store(version);
        await session.SaveChangesAsync(cancellationToken);
        return new SaveRegulationVersionResult(version);
    }

    public static async Task<RegulationVersionDocument> Handle(
        PublishRegulationVersionCommand command,
        IDocumentSession session,
        IQuerySession query,
        CancellationToken cancellationToken)
    {
        var version = await query.LoadAsync<RegulationVersionDocument>(command.Id, cancellationToken)
            ?? throw new DomainRuleViolationException("Regulation version was not found.");
        var errors = ValidateSource(version.Source);
        if (string.IsNullOrWhiteSpace(version.Title) || string.IsNullOrWhiteSpace(version.Summary))
        {
            errors.Add("Regulation title and summary are required before publishing.");
        }
        if (errors.Count > 0) throw new DomainRuleViolationException(string.Join(" ", errors));

        var previousVersions = await query.Query<RegulationVersionDocument>()
            .Where(item => item.Status == "Published" && item.Id != version.Id)
            .ToListAsync(cancellationToken);
        foreach (var previous in previousVersions)
        {
            previous.Status = "Deprecated";
            session.Store(previous);
        }

        version.Status = "Published";
        session.Store(version);
        await session.SaveChangesAsync(cancellationToken);
        return version;
    }

    public static async Task<SaveExamBlueprintVersionResult> Handle(
        SaveExamBlueprintVersionCommand command,
        IDocumentSession session,
        IQuerySession query,
        CancellationToken cancellationToken)
    {
        var errors = ValidateSource(command.Source);
        errors.AddRange(ValidateBlueprintRules(command.Blueprints));
        if (string.IsNullOrWhiteSpace(command.Version))
        {
            errors.Add("Blueprint version is required.");
        }

        ExamBlueprintVersionDocument version;
        if (command.Id is null)
        {
            var duplicate = await query.Query<ExamBlueprintVersionDocument>()
                .Where(item => item.Version == command.Version.Trim())
                .AnyAsync(cancellationToken);
            if (duplicate) throw new DomainRuleViolationException("An exam blueprint with this version already exists.");
            version = new ExamBlueprintVersionDocument { Id = Guid.NewGuid() };
        }
        else
        {
            version = await query.LoadAsync<ExamBlueprintVersionDocument>(command.Id.Value, cancellationToken)
                ?? throw new DomainRuleViolationException("Exam blueprint version was not found.");
            if (version.Status == "Published")
            {
                throw new DomainRuleViolationException("Published blueprints are immutable; create a new version.");
            }
        }

        if (errors.Count > 0)
        {
            throw new DomainRuleViolationException(string.Join(" ", errors));
        }

        version.Version = command.Version.Trim();
        version.EffectiveFrom = command.EffectiveFrom;
        version.Blueprints = command.Blueprints;
        version.Source = command.Source;
        version.Status = "Draft";
        session.Store(version);
        await session.SaveChangesAsync(cancellationToken);
        return new SaveExamBlueprintVersionResult(version);
    }

    public static async Task<ExamBlueprintVersionDocument> Handle(
        PublishExamBlueprintVersionCommand command,
        IDocumentSession session,
        IQuerySession query,
        CancellationToken cancellationToken)
    {
        var version = await query.LoadAsync<ExamBlueprintVersionDocument>(command.Id, cancellationToken)
            ?? throw new DomainRuleViolationException("Exam blueprint version was not found.");
        var errors = ValidateSource(version.Source);
        errors.AddRange(ValidateBlueprintRules(version.Blueprints));
        foreach (var rule in version.Blueprints)
        {
            var bankExists = await query.Query<QuestionBankVersionDocument>()
                .Where(item => item.Status == "Published" && item.Version == rule.QuestionBankVersion)
                .AnyAsync(cancellationToken);
            if (!bankExists)
            {
                errors.Add($"No published question bank exists for blueprint rule {rule.LicenseClassSlug}.");
            }
        }
        if (errors.Count > 0) throw new DomainRuleViolationException(string.Join(" ", errors));

        var previousVersions = await query.Query<ExamBlueprintVersionDocument>()
            .Where(item => item.Status == "Published" && item.Id != version.Id)
            .ToListAsync(cancellationToken);
        foreach (var previous in previousVersions)
        {
            previous.Status = "Deprecated";
            session.Store(previous);
        }

        version.Status = "Published";
        session.Store(version);
        await session.SaveChangesAsync(cancellationToken);
        return version;
    }

    public static List<string> ValidateQuestionBank(
        QuestionBankVersionDocument version,
        IReadOnlyCollection<QuestionDocument> questions)
    {
        var errors = ValidateSource(version.Source);
        if (string.IsNullOrWhiteSpace(version.Version)) errors.Add("Question bank version is required.");
        if (version.LicenseClassSlugs.Count == 0) errors.Add("Question bank must target at least one license class.");
        if (questions.Count == 0) errors.Add("Question bank must contain at least one question.");

        var ids = new HashSet<Guid>();
        foreach (var question in questions)
        {
            if (question.Id == Guid.Empty) errors.Add("Question id must be a non-empty GUID.");
            if (!ids.Add(question.Id)) errors.Add($"Duplicate question id: {question.Id}.");
            if (string.IsNullOrWhiteSpace(question.Text) || string.IsNullOrWhiteSpace(question.Topic))
            {
                errors.Add($"Question {question.Id} must have text and topic.");
            }
            var optionIds = question.Options.Select(option => option.Id).ToHashSet();
            if (question.Options.Count < 2 || optionIds.Count != question.Options.Count || question.Options.Any(option => string.IsNullOrWhiteSpace(option.Text)))
            {
                errors.Add($"Question {question.Id} has invalid options.");
            }
            if (!optionIds.Contains(question.CorrectOptionId)) errors.Add($"Question {question.Id} has no valid correct option.");
        }

        return errors;
    }

    private static List<string> ValidateQuestionInputs(
        ImportQuestionBankCommand command,
        IReadOnlyCollection<string> licenseClassSlugs)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Version)) errors.Add("Question bank version is required.");
        if (command.EffectiveFrom == default) errors.Add("Question bank effective date is required.");
        if (licenseClassSlugs.Count == 0) errors.Add("Question bank must target at least one license class.");
        if (command.Questions.Count == 0) errors.Add("Question bank must contain at least one question.");

        var ids = new HashSet<Guid>();
        foreach (var question in command.Questions)
        {
            if (question.Id == Guid.Empty) errors.Add("Question id must be a non-empty GUID.");
            if (!ids.Add(question.Id)) errors.Add($"Duplicate question id: {question.Id}.");
            if (question.LicenseClassSlugs.Count == 0 || !question.LicenseClassSlugs.All(licenseClassSlugs.Contains))
            {
                errors.Add($"Question {question.Id} references an unknown license class.");
            }
            var optionIds = question.Options.Select(option => option.Id).ToHashSet();
            if (string.IsNullOrWhiteSpace(question.Text) || string.IsNullOrWhiteSpace(question.Topic))
            {
                errors.Add($"Question {question.Id} must have text and topic.");
            }
            if (question.Options.Count < 2 || optionIds.Count != question.Options.Count || question.Options.Any(option => string.IsNullOrWhiteSpace(option.Text)))
            {
                errors.Add($"Question {question.Id} has invalid options.");
            }
            if (!optionIds.Contains(question.CorrectOptionId)) errors.Add($"Question {question.Id} has no valid correct option.");
        }

        return errors;
    }

    private static List<string> ValidateBlueprintRules(IReadOnlyCollection<ExamBlueprintRule> rules)
    {
        var errors = new List<string>();
        if (rules.Count == 0) errors.Add("An exam blueprint must contain at least one rule.");
        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.LicenseClassSlug) || string.IsNullOrWhiteSpace(rule.QuestionBankVersion))
            {
                errors.Add("Each blueprint rule needs a license class and question bank version.");
            }
            if (rule.QuestionCount <= 0 || rule.DurationSeconds <= 0 || rule.PassingScore < 0 || rule.PassingScore > rule.QuestionCount)
            {
                errors.Add($"Blueprint rule {rule.LicenseClassSlug} has invalid exam limits.");
            }
            if (rule.CriticalQuestionCount < 0 || rule.CriticalQuestionCount > rule.QuestionCount)
            {
                errors.Add($"Blueprint rule {rule.LicenseClassSlug} has an invalid critical-question count.");
            }
            if (rule.TopicQuestionCounts.Count > 0 && rule.TopicQuestionCounts.Values.Any(value => value < 0))
            {
                errors.Add($"Blueprint rule {rule.LicenseClassSlug} has a negative topic count.");
            }
            if (rule.TopicQuestionCounts.Count > 0 && rule.TopicQuestionCounts.Values.Sum() + rule.CriticalQuestionCount != rule.QuestionCount)
            {
                errors.Add($"Blueprint rule {rule.LicenseClassSlug} topic and critical counts do not match questionCount.");
            }
        }

        return errors;
    }

    private static List<string> ValidateSource(SourceProvenance source)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(source.Title) || string.IsNullOrWhiteSpace(source.Url))
        {
            errors.Add("Source title and URL are required.");
        }
        if (source.RetrievedAt == default) errors.Add("Source retrieval date is required.");
        return errors;
    }

    private static async Task<List<string>> ValidateLicenseClasses(
        IQuerySession query,
        IReadOnlyCollection<string> slugs,
        CancellationToken cancellationToken)
    {
        var known = await query.Query<LicenseClassDocument>()
            .Where(item => slugs.Contains(item.Slug))
            .Select(item => item.Slug)
            .ToListAsync(cancellationToken);
        return slugs.Except(known).Select(slug => $"Unknown license class: {slug}.").ToList();
    }

    private static IReadOnlyList<string> NormalizeSlugs(IEnumerable<string> slugs) =>
        slugs.Select(slug => slug.Trim().ToLowerInvariant()).Where(slug => slug.Length > 0).Distinct().ToArray();

    private static QuestionDocument ToDocument(
        AdminQuestionInput input,
        string version,
        IReadOnlyList<string> bankLicenseClassSlugs,
        SourceProvenance bankSource) =>
        new()
        {
            // QuestionDocument ids are global in Marten. Scope imported identities
            // by bank version so publishing a new bank cannot overwrite questions
            // referenced by an in-progress attempt on an older bank.
            Id = VersionedQuestionId(version, input.Id),
            Slug = input.Slug.Trim(),
            LicenseClassSlug = input.LicenseClassSlugs.FirstOrDefault() ?? bankLicenseClassSlugs[0],
            LicenseClassSlugs = input.LicenseClassSlugs,
            Topic = input.Topic.Trim(),
            Text = input.Text.Trim(),
            Options = input.Options,
            CorrectOptionId = input.CorrectOptionId.Trim(),
            IsCritical = input.IsCritical,
            Explanation = input.Explanation.Trim(),
            MemoryTip = string.IsNullOrWhiteSpace(input.MemoryTip) ? null : input.MemoryTip.Trim(),
            QuestionBankVersion = version,
            Source = bankSource
        };

    private static Guid VersionedQuestionId(string version, Guid sourceQuestionId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"question:{version}:{sourceQuestionId:D}"));
        return new Guid(bytes[..16]);
    }
}
