namespace BusinessManagement.Infrastructure.Messaging;
public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "";
    public string Password { get; init; } = "";
    public string VirtualHost { get; init; } = "/";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException(
                "Missing configuration 'RabbitMq:Host', 'RabbitMq:UserName' or 'RabbitMq:Password'. " +
                "Set the password with user-secrets or the environment variable RabbitMq__Password.");
    }
}