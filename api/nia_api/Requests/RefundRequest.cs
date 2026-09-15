namespace nia_api.Requests;

public class RefundRequest
{
    public decimal? Amount { get; set; }
    public string? Reason { get; set; }
}
