namespace nia_api.Requests;

public class PaymentRequest
{
    public int OrderId { get; set; }
    public string CancellationToken { get; set; }
}
