namespace FamilyChat.Application.Exceptions;

public sealed class NotFoundException(string message = "Not found") : AppException(message);
