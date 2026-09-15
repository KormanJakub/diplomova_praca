namespace nia_api.Domain.Orders;

public interface IOrderSequenceStore
{
    Task<int> NextOrderIdAsync(CancellationToken ct = default);
    Task<string> NextOrderNumberAsync(int? year = null, CancellationToken ct = default);
}
