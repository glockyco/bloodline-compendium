namespace Bloodline.Extraction;

internal sealed class PeakMemorySample : IDisposable
{
    private long peak = Environment.WorkingSet;
    private readonly Timer timer;

    public PeakMemorySample()
    {
        // PeakWorkingSet64 is zero on macOS. Sample the resident working set on every platform instead.
        timer = new Timer(_ => Sample(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
    }

    public long Bytes
    {
        get
        {
            Sample();
            return Interlocked.Read(ref peak);
        }
    }

    private void Sample()
    {
        var current = Environment.WorkingSet;
        var previous = Interlocked.Read(ref peak);
        while (current > previous)
        {
            var observed = Interlocked.CompareExchange(ref peak, current, previous);
            if (observed == previous)
                break;
            previous = observed;
        }
    }

    public void Dispose() => timer.Dispose();
}
