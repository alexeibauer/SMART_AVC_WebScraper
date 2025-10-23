namespace WebScraperLogic;

public class PriceRecord
{

    public PriceRecord(decimal amount, string currency, string priceName)
    {
        Amount = amount;
        Currency = currency;
        PriceName = priceName;
    }

    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? PriceName { get; set; }

    public override string ToString()
    {
        return $"Amount={Amount}, Currency={(Currency is null ? "null" : Currency)}, PriceName={(PriceName is null ? "null" : PriceName)}";
    }

}
