namespace Aura.Domain.Exceptions;

public class NotFoundException(string message) : DomainException(message);
