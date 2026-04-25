namespace ConsoleStudentRegistration.Helpers;

public static class ConsoleHelper
{
    public static void PrintHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"  {title.ToLower()}");
        Console.WriteLine(new string('-', 60));
        Console.ResetColor();
    }

    public static void PrintSubHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n  --- {title} ---\n");
        Console.ResetColor();
    }

    public static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n  [SUCCESS] {message}");
        Console.ResetColor();
    }

    public static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n  [ERROR] {message}");
        Console.ResetColor();
    }

    public static void PrintWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"\n  [WARNING] {message}");
        Console.ResetColor();
    }

    public static void PrintInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  {message}");
        Console.ResetColor();
    }

    public static void PrintMenuItem(string key, string description)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"    [{key}] ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(description);
        Console.ResetColor();
    }

    public static string ReadInput(string prompt)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"\n  {prompt}: ");
        Console.ForegroundColor = ConsoleColor.White;
        var input = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.ResetColor();
        return input;
    }

    public static int ReadInt(string prompt, int min = int.MinValue, int max = int.MaxValue)
    {
        while (true)
        {
            var input = ReadInput(prompt);
            if (int.TryParse(input, out int value) && value >= min && value <= max)
                return value;
            PrintError($"Please enter a valid number{(min != int.MinValue ? $" between {min} and {max}" : "")}.");
        }
    }

    public static double ReadDouble(string prompt, double min = 0, double max = 100)
    {
        while (true)
        {
            var input = ReadInput(prompt);
            if (double.TryParse(input, out double value) && value >= min && value <= max)
                return value;
            PrintError($"Please enter a valid number between {min} and {max}.");
        }
    }

    public static DateTime ReadDate(string prompt)
    {
        while (true)
        {
            var input = ReadInput($"{prompt} (yyyy-MM-dd)");
            if (DateTime.TryParse(input, out DateTime date))
                return date;
            PrintError("Invalid date format. Use yyyy-MM-dd.");
        }
    }

    public static bool AskContinue(string action = "continue")
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write($"\n  Do you want to {action}? (yes/no): ");
        Console.ForegroundColor = ConsoleColor.White;
        var input = Console.ReadLine()?.Trim().ToLower() ?? "no";
        Console.ResetColor();
        return input is "yes" or "y";
    }

    public static void PrintTable(string[] headers, List<string[]> rows, int? totalCountOverride = null)
    {
        if (rows.Count == 0)
        {
            PrintWarning("No records found.");
            return;
        }

        var widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
        {
            widths[i] = headers[i].Length;
            foreach (var row in rows)
            {
                if (i < row.Length && row[i].Length > widths[i])
                    widths[i] = row[i].Length;
            }
            widths[i] = Math.Min(widths[i] + 2, 30);
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  ");
        for (int i = 0; i < headers.Length; i++)
            Console.Write("+" + new string('-', widths[i]));
        Console.WriteLine("+");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("  ");
        for (int i = 0; i < headers.Length; i++)
            Console.Write("|" + headers[i].PadRight(widths[i]));
        Console.WriteLine("|");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  ");
        for (int i = 0; i < headers.Length; i++)
            Console.Write("+" + new string('-', widths[i]));
        Console.WriteLine("+");

        Console.ForegroundColor = ConsoleColor.White;
        foreach (var row in rows)
        {
            Console.Write("  ");
            for (int i = 0; i < headers.Length; i++)
            {
                var val = i < row.Length ? row[i] : "";
                if (val.Length > widths[i] - 1) val = val[..(widths[i] - 2)] + "~";
                Console.Write("|" + val.PadRight(widths[i]));
            }
            Console.WriteLine("|");
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  ");
        for (int i = 0; i < headers.Length; i++)
            Console.Write("+" + new string('-', widths[i]));
        Console.WriteLine("+");

        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        var total = totalCountOverride ?? rows.Count;
        if (totalCountOverride.HasValue && totalCountOverride.Value != rows.Count)
            Console.WriteLine($"  Showing {rows.Count} of {total} record(s)");
        else
            Console.WriteLine($"  Total: {rows.Count} record(s)");
        Console.ResetColor();
    }

    public static void PrintTablePaged(string[] headers, List<string[]> allRows, int pageSize)
    {
        if (allRows.Count == 0)
        {
            PrintWarning("No records found.");
            return;
        }

        pageSize = Math.Clamp(pageSize, 5, 200);
        var total = allRows.Count;
        var pageCount = (total + pageSize - 1) / pageSize;
        var current = 0;

        while (true)
        {
            var slice = allRows.Skip(current * pageSize).Take(pageSize).ToList();
            PrintTable(headers, slice, total);

            if (pageCount <= 1)
                break;

            PrintInfo($"Paging: page {current + 1} of {pageCount}. Commands: [n]ext  [p]revious  [q]uit");
            var cmd = ReadInput("Command").ToLowerInvariant();
            if (cmd is "n" or "next")
            {
                if (current < pageCount - 1) current++;
            }
            else if (cmd is "p" or "prev" or "previous")
            {
                if (current > 0) current--;
            }
            else if (cmd is "q" or "quit")
            {
                break;
            }
        }
    }

    public static void Pause()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("\n  Press any key to continue...");
        Console.ResetColor();
        Console.ReadKey(true);
        Console.WriteLine();
    }
}
