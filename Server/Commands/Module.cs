namespace Server.Commands;

public abstract class Module
{
    // protected CommandContext Context;
    //
    // private readonly SemaphoreSlim _semaphore = new(1, 1);
    // public async Task SetContext(CommandContext context)
    // {
    //     await _semaphore.WaitAsync();
    //     Context = context;
    // }
    //
    // public void ReleaseContext() => _semaphore.Release();
}