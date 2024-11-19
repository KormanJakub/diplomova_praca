namespace nia_api.Requests;

public class GuestOrderRequest
{
    public string GuestUserId { get; set; }
    public List<string> CustomizationsId { get; set; }
}