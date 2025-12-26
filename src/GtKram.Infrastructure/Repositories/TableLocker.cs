namespace GtKram.Infrastructure.Repositories;

internal sealed class TableLocker : IDisposable
{
    public SemaphoreSlim SellerRegistration { get; } = new(1, 1);
    public SemaphoreSlim SellerNumber { get; } = new(1, 1);
    public SemaphoreSlim LabelNumber { get; } = new(1, 1);

    public void Dispose()
    {
        SellerRegistration.Dispose();
        SellerNumber.Dispose();
        LabelNumber.Dispose();
    }
}
