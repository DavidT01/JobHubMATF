using System.Text;
using RabbitMQ.Client;

namespace ApplicationService.Infrastructure.Messaging;

public sealed class OutboxMessagingOptions
{
    public bool Enabled { get; set; }
    public string? ConnectionUri { get; set; }
    public string Exchange { get; set; } = "jobhub.applications.v1";

    public IConnectionFactory CreateConnectionFactory()
    {
        if (!Uri.TryCreate(ConnectionUri, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("amqp" or "amqps") || string.IsNullOrEmpty(uri.Host)
            || string.IsNullOrWhiteSpace(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment) || (uri.Scheme == "amqp" && !uri.IsLoopback))
            throw new InvalidOperationException("Outbox:ConnectionUri must include credentials and use AMQPS (AMQP allowed only on loopback).");
        if (string.IsNullOrWhiteSpace(Exchange) || Encoding.UTF8.GetByteCount(Exchange) > 255
            || Exchange.StartsWith("amq.", StringComparison.Ordinal))
            throw new InvalidOperationException("Outbox:Exchange must be a non-reserved name of at most 255 UTF-8 bytes.");
        try
        {
            return new ConnectionFactory
            {
                Uri = uri,
                AutomaticRecoveryEnabled = false,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10),
                ClientProvidedName = "jobhub-application-outbox"
            };
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            // Do not expose the original URI or inner exception containing credentials.
            throw new InvalidOperationException("Outbox:ConnectionUri is not a valid RabbitMQ connection URI.");
        }
    }
}
