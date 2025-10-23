using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebScraperLogic;

public static class WebScraperUrlMultiConsumer
{
    public static async Task RunAsync(
        string host = "localhost",
        string queue = "webscraper.urls",
        int consumers = 4,
        CancellationToken ct = default)
    {
        var factory = new ConnectionFactory { HostName = host };
        await using var connection = await factory.CreateConnectionAsync();

        var tasks = new Task[consumers];

        for (int i = 0; i < consumers; i++)
        {
            int id = i; // capture loop variable

            tasks[i] = Task.Run(async () =>
            {
                await using var channel = await connection.CreateChannelAsync();

                // Make sure the queue exists
                await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);

                // Fair dispatch (each consumer gets one unacked message at a time)
                await channel.BasicQosAsync(0, prefetchCount: 1, global: false);

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    var body = ea.Body.ToArray();
                    string msg = Encoding.UTF8.GetString(body);

                    try
                    {
                        Console.WriteLine($"[Consumer {id}] Processing: {msg}");

                        using var doc = JsonDocument.Parse(msg);

                        string? url = doc.RootElement.GetProperty("Url").GetString();

                        // Acknowledge the message, so it won't be re-delivered
                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        Console.WriteLine($"[Consumer {id}] Acked");

                        var result = await WebPriceCrawler.CrawlAsync(
                            startUrl: url,
                            maxDepth: 1,
                            sameHostOnly: true,
                            ct: ct);

                        Console.WriteLine($"[Consumer {id}] Found {result.Count} prices::");
                        foreach (var scrapedUrl in result)
                        {
                            Console.WriteLine($"[Consumer {id}]   {scrapedUrl.Key} => {scrapedUrl.Value}");
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Consumer {id}] Error: {ex.Message}");
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: queue,
                    autoAck: false,
                    consumer: consumer);

                while (!ct.IsCancellationRequested)
                    await Task.Delay(500, ct);
            }, ct);
        }

        await Task.WhenAll(tasks);
    }
}