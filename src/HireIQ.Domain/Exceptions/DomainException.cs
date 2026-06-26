namespace HireIQ.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.") { }
}

public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}
