using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class PaymentRequest
{
    [Range(1, int.MaxValue)] public int OrderId { get; set; }
    [Required, StringLength(128, MinimumLength = 32)] public string CancellationToken { get; set; } = string.Empty;
}
