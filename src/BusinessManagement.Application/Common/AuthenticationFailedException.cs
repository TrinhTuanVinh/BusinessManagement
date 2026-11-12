namespace BusinessManagement.Application.Common;

public class AuthenticationFailedException(string message) : Exception(message);