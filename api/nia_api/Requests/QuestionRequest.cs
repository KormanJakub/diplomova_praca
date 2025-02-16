namespace nia_api.Requests;

public class QuestionRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public string? FileId { get; set; }
    public string? PathOfUrl { get; set; }
}