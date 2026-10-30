using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncMonitor;

public sealed class CheckService
{
    public async Task<CheckResult> CheckServiceAsync(string name, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя проверки не может быть пустым.", nameof(name));

        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), "Длительность не может быть отрицательной.");

        var sw = Stopwatch.StartNew();
        await Task.Delay(delay, cancellationToken);
        sw.Stop();

        return new CheckResult(name, true, sw.Elapsed, "Успешно");
    }

    public async Task<IReadOnlyList<CheckResult>> RunSequentialAsync(IReadOnlyList<(string Name, TimeSpan Delay)> checks, CancellationToken token = default)
    {
        var results = new List<CheckResult>();
        foreach (var check in checks)
        {
            var result = await CheckServiceAsync(check.Name, check.Delay, token);
            results.Add(result);
        }
        return results;
    }

    public async Task<IReadOnlyList<CheckResult>> RunParallelAsync(IReadOnlyList<(string Name, TimeSpan Delay)> checks, CancellationToken token = default)
    {
        var tasks = checks.Select(check => CheckServiceAsync(check.Name, check.Delay, token)).ToArray();
        var results = await Task.WhenAll(tasks);  // FIX THIS LINE
        return results;
    }
}