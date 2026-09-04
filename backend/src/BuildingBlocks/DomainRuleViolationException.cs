namespace Gplx.BuildingBlocks;

public sealed class DomainRuleViolationException(string message) : Exception(message);
