using System.Diagnostics;

namespace TaskPilot.IntegrationTests;

internal static class Eventually
{
    public static async Task<T> UntilAsync<T>(
        Func<Task<T>> probe,
        Func<T, bool> completed,
        string description,
        TimeSpan? timeout = null)
    {
        var limit = timeout ?? TimeSpan.FromSeconds(15);
        var stopwatch = Stopwatch.StartNew();
        T last;
        do
        {
            last = await probe();
            if (completed(last))
            {
                return last;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        } while (stopwatch.Elapsed < limit);

        throw new TimeoutException($"Timed out waiting for {description}. Last value: {last}");
    }
}
