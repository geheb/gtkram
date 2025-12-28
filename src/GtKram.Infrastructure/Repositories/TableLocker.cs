namespace GtKram.Infrastructure.Repositories;

internal sealed class TableLocker : IDisposable
{
    private readonly SemaphoreSlim _sellerRegistration = new(1, 1);
    private readonly SemaphoreSlim _sellerNumber = new(1, 1);
    private readonly SemaphoreSlim _labelNumber = new(1, 1);

    public Task<IDisposable?> LockSellerRegistration(CancellationToken cancellationToken) =>
        Lock(_sellerRegistration, cancellationToken);

    public Task<IDisposable?> LockSellerNumber(CancellationToken cancellationToken) =>
        Lock(_sellerNumber, cancellationToken);

    public Task<IDisposable?> LockLabelNumber(CancellationToken cancellationToken) =>
        Lock(_labelNumber, cancellationToken);

    public void Dispose()
    {
        _sellerRegistration.Dispose();
        _sellerNumber.Dispose();
        _labelNumber.Dispose();
    }

    private static async Task<IDisposable?> Lock(SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        if (!await semaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return null;
        }
        return new Releaser(semaphore);
    }

    private readonly struct Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public Releaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}
