using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AsyncMonitor;

var service = new CheckService();
var checks = new (string Name, TimeSpan Delay)[]
{
    ("Сеть", TimeSpan.FromMilliseconds(700)),
    ("База данных", TimeSpan.FromMilliseconds(900)),
    ("Файлы", TimeSpan.FromMilliseconds(500))
};

CancellationTokenSource? cts = null;

while (true)
{
    Console.WriteLine("\n1 — одна проверка; 2 — последовательно; 3 — параллельно; 4 — длительная с отменой; 0 — выход");
    var cmd = Console.ReadLine()?.Trim();

    if (cmd == "0")
    {
        cts?.Cancel();
        break;
    }

    // 10 Режим контроля выполнения, показывающий неблокирующий ввод
    if (cmd == "4")
    {
        cts = new CancellationTokenSource();
        Console.WriteLine("Запуск длительной проверки (3 сек). Введите 'c' для отмены, не блокируя ввод.");

        var task = Task.Run(async () =>
        {
            try
            {
                var result = await service.CheckServiceAsync("Длительная", TimeSpan.FromSeconds(3), cts.Token);
                Console.WriteLine($"\n[Фон] Результат: {result.Name} - {result.Message} ({result.Duration.TotalMilliseconds} мс)");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\n[Фон] Длительная проверка была отменена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Фон] Ошибка: {ex.GetType().Name} - {ex.Message}");
            }
        });

        // Неблокирующий опрос клавиатуры во время выполнения задачи
        while (!task.IsCompleted)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true).KeyChar;
                if (key == 'c' || key == 'C' || key == 'с' || key == 'С')
                {
                    cts.Cancel();
                    Console.WriteLine("\nОтмена запрошена...");
                    break;
                }
            }
            await Task.Delay(100); // Освобождаем поток не блокируем его
        }
        continue;
    }

    try
    {
        if (cmd == "1")
        {
            // 8 Вызвать метод с await, не применять .Wait() или .Result.
            var result = await service.CheckServiceAsync("Одиночная", TimeSpan.FromMilliseconds(500));
            Console.WriteLine($"Результат: {result.Name} - {result.Message} ({result.Duration.TotalMilliseconds} мс)");
        }
        else if (cmd == "2")
        {
            var sw = Stopwatch.StartNew();
            var r = await service.RunSequentialAsync(checks);
            sw.Stop();
            Console.WriteLine($"Последовательно: {sw.ElapsedMilliseconds} мс");
            Print(r);
        }
        else if (cmd == "3")
        {
            var sw = Stopwatch.StartNew();
            var r = await service.RunParallelAsync(checks);
            sw.Stop();
            Console.WriteLine($"Параллельно: {sw.ElapsedMilliseconds} мс");
            Print(r);
        }
        else
        {
            Console.WriteLine("Неизвестная команда.");
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Проверка отменена.");
    }
    catch (Exception ex)
    {
        // 9 Не скрывать исключение; сообщить тип и причину.
        Console.WriteLine($"Ошибка: {ex.GetType().Name} - {ex.Message}");
    }
}

static void Print(IEnumerable<CheckResult> xs)
{
    foreach (var x in xs)
    {
        Console.WriteLine($"  {x.Name}: {(x.Success ? "OK" : "FAIL")} ({x.Duration.TotalMilliseconds} мс) - {x.Message}");
    }
}