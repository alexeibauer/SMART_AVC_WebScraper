
namespace SMART_AVC_WebScraper;

public class Program
{

    private static NotificationsHub? notificationsHub;

    public static async Task Main(string[] args)
    {
        InitNotificationsHub();

        while(true)
        {
            bool exit = await PromptAndPublishUrlAsync();
            if (exit) break;
        }
    }

    private static async Task<bool> PromptAndPublishUrlAsync() { 
        Console.WriteLine("Enter a URL to scrape (or type exit to end process):");
        string url = Console.ReadLine() ?? string.Empty;

        if(string.IsNullOrWhiteSpace(url))
        {
            Console.WriteLine("No URL provided. Exiting.");
            return true;
        }

        if (url == "exit") {
            return true;
        }

        await notificationsHub.PublishAsync(
            new { Url = url, Timestamp = DateTime.UtcNow },
            routingKey: "webscraper.urls"
        );
        Console.WriteLine($"Published URL to scrape: {url}");
        return false;
    
    }

    private static void InitNotificationsHub() { 
        
        notificationsHub = new NotificationsHub(
            hostName: "localhost",
            userName: "guest",
            password: "guest"
        );
    }
}