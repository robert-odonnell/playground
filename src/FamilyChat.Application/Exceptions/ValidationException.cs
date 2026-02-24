namespace FamilyChat.Application.Exceptions;

public sealed class ValidationException(string message) : AppException(message);
