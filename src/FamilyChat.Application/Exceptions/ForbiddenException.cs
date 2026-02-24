namespace FamilyChat.Application.Exceptions;

public sealed class ForbiddenException(string message = "Forbidden") : AppException(message);
