namespace WebScraperWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("WebScraper Worker started.");
            await WebScraperUrlMultiConsumer.RunAsync(
                host: "localhost",
                queue: "webscraper.urls",
                consumers: 1,
                ct: stoppingToken
            );
        }
    }
}
