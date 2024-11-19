namespace nia_api.Requests;

public class PaymentRequest
{
    public string ProductName { get; set; }
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
}