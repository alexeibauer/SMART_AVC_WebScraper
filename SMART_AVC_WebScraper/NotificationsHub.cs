using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace SMART_AVC_WebScraper;

public class NotificationsHub
{
    private readonly ConnectionFactory _factory;

    public NotificationsHub(string hostName, string userName, string password)
    {
        _factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password
        };
    }

    public async Task PublishAsync<T>(T message, string routingKey, string exchange = "")
    {
        // open connection and channel asynchronously
        await using IConnection connection = await _factory.CreateConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();

        // declare the queue if needed (optional)
        await channel.QueueDeclareAsync(queue: routingKey,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false);

        // serialize to JSON and convert to bytes
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        // create message properties (BasicProperties in v7+)
        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent // persistent
        };
        

        // publish the message asynchronously
        await channel.BasicPublishAsync(exchange,
                                        routingKey,
                                        mandatory: false,
                                        basicProperties: props,
                                        body: body.AsMemory());
    }
}
