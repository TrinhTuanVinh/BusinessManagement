namespace BusinessManagement.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "";
    public string Password { get; init; } = "";
    public string VirtualHost { get; init; } = "/";
    public int RetryDelayMs { get; init; } = 5000;
    public int MaxAttempts { get; init; } = 3;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException(
                "Missing configuration 'RabbitMq:Host', 'RabbitMq:UserName' or 'RabbitMq:Password'. " +
                "Set the password with user-secrets or the environment variable RabbitMq__Password.");
        if (RetryDelayMs < 100)
            throw new InvalidOperationException("RetryDelayMs must be at least 100.");

        if (MaxAttempts < 1 || MaxAttempts > 10)
            throw new InvalidOperationException("MaxAttempts must be between 1 and 10.");
    }
}