var cts = new CancellationTokenSource();

// Start writing stats on background thread.
_ = Task.Run(() => new StatisticsFile().WriteCurrentStatisticsUntilCanceled(cts.Token));

// Let's pretend we do 30-ish seconds of work here.
for (var i = 0; i < 30; i++)
{
    Console.WriteLine("Doing some work.");
    await Task.Delay(TimeSpan.FromSeconds(1));

    // Pretend the work is hard enough that we need to run the GC and perform memory management.
    GC.Collect();
    GC.WaitForPendingFinalizers();
}

// All done! Stats writer can stop now.
cts.Cancel();

sealed class StatisticsFile
{
    private const string Path = "stats.bin";

    ~StatisticsFile()
    {
        // Once we get cleaned up, we do not need the file anymore - statistics are only valid while app is running.
        Console.WriteLine("Deleting stats file.");
        File.Delete(Path);
    }

    public async Task WriteCurrentStatisticsUntilCanceled(CancellationToken cancel)
    {
        // Open the file and allow other processes to read it as well.
        using var file = File.Open(Path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

        while (!cancel.IsCancellationRequested)
        {
            // Clear the file.
            file.SetLength(0);

            // Dummy data.
            await file.WriteAsync(new byte[] { 1, 2, 3, 4, 0 });
            await file.FlushAsync();

            Console.WriteLine("Stats updated.");

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}