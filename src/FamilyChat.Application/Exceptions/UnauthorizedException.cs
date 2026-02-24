namespace FamilyChat.Application.Exceptions;

public sealed class UnauthorizedException(string message = "Unauthorized") : AppException(message);
