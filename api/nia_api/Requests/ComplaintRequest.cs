namespace nia_api.Requests;

public class ComplaintRequest
{
    public int OrderId { get; set; }
    public string UserId { get; set; } 
    public string Email { get; set; }
    public string ComplaintDetails { get; set; }
}