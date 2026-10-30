using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncMonitor;

var passed = 0;
var failed = 0;

void Check(bool ok, string name)
{
    if (ok)
    {
        passed++;
        Console.WriteLine("PASS " + name);
    }
    else
    {
        failed++;
        Console.WriteLine("FAIL " + name);
    }
}

var s = new CheckService();

// 1 Одиночная проверка
var one = await s.CheckServiceAsync("Тест", TimeSpan.FromMilliseconds(25));
Check(
    one.Name == "Тест" && one.Success && one.Duration >= TimeSpan.Zero,
    "одиночная проверка возвращает результат"
);

// 2 Последовательный запуск
var seq = await s.RunSequentialAsync(
    new[]
    {
        ("A", TimeSpan.FromMilliseconds(60)),
        ("B", TimeSpan.FromMilliseconds(60)),
        ("C", TimeSpan.FromMilliseconds(60))
    }
);
Check(
    seq.Count == 3 && seq.Select(x => x.Name).SequenceEqual(new[] { "A", "B", "C" }),
    "последовательный запуск"
);

// 3 Параллельный запуск
var all = await s.RunParallelAsync(
    new[]
    {
        ("A", TimeSpan.FromMilliseconds(60)),
        ("B", TimeSpan.FromMilliseconds(60)),
        ("C", TimeSpan.FromMilliseconds(60))
    }
);
Check(all.Count == 3 && all.All(x => x.Success), "параллельный запуск WhenAll");

// 4 Тест на пустое имя
try
{
    await s.CheckServiceAsync("", TimeSpan.FromMilliseconds(10));
    Check(false, "пустое имя должно вызывать исключение");
}
catch (ArgumentException)
{
    Check(true, "пустое имя вызывает ArgumentException");
}
catch
{
    Check(false, "пустое имя вызывает неверный тип исключения");
}

// 5 Тест на отрицательную задержку
try
{
    await s.CheckServiceAsync("Тест", TimeSpan.FromMilliseconds(-10));
    Check(false, "отрицательная задержка должна вызывать исключение");
}
catch (ArgumentOutOfRangeException)
{
    Check(true, "отрицательная задержка вызывает ArgumentOutOfRangeException");
}
catch
{
    Check(false, "отрицательная задержка вызывает неверный тип исключения");
}

// 6 Тест на CancellationToken
var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await s.CheckServiceAsync("Отмена", TimeSpan.FromSeconds(1), cts.Token);
    Check(false, "отменённый токен должен вызывать OperationCanceledException");
}
catch (OperationCanceledException)
{
    Check(true, "отменённый токен вызывает OperationCanceledException");
}
catch
{
    Check(false, "отменённый токен вызывает неверный тип исключения");
}

Console.WriteLine($"\nИтого: {passed} пройдено, {failed} ошибок.");
return failed == 0 ? 0 : 1;