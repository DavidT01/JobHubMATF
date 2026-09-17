using System.Text;
using RabbitMQ.Client;

namespace Notification.API.Messaging;

public sealed class RabbitMessagingOptions
{
    public const string SectionName = "RabbitMq";

    public bool Enabled { get; set; }
    public string? ConnectionUri { get; set; }
    public bool AllowInsecureAmqp { get; set; }
    public string ApplicationsExchange { get; set; } = "jobhub.applications.v1";
    public string RecruitmentExchange { get; set; } = "jobhub.recruitment.v1";
    public string ApplicationsQueue { get; set; } = "notification.applications";
    public string RecruitmentQueue { get; set; } = "notification.recruitment";

    public IConnectionFactory CreateConnectionFactory()
    {
        if (!Uri.TryCreate(ConnectionUri, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("amqp" or "amqps") || string.IsNullOrEmpty(uri.Host)
            || string.IsNullOrWhiteSpace(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || (uri.Scheme == "amqp" && !uri.IsLoopback && !AllowInsecureAmqp))
        {
            throw new InvalidOperationException(
                "RabbitMq:ConnectionUri must include credentials and use AMQPS (AMQP allowed only on loopback unless AllowInsecureAmqp is true).");
        }

        ValidateExchange(ApplicationsExchange, nameof(ApplicationsExchange));
        ValidateExchange(RecruitmentExchange, nameof(RecruitmentExchange));

        try
        {
            return new ConnectionFactory
            {
                Uri = uri,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10),
                ClientProvidedName = "jobhub-notification-consumer"
            };
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            throw new InvalidOperationException("RabbitMq:ConnectionUri is not a valid RabbitMQ connection URI.");
        }
    }

    private static void ValidateExchange(string exchange, string name)
    {
        if (string.IsNullOrWhiteSpace(exchange) || Encoding.UTF8.GetByteCount(exchange) > 255
            || exchange.StartsWith("amq.", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"RabbitMq:{name} must be a non-reserved name of at most 255 UTF-8 bytes.");
        }
    }
}
