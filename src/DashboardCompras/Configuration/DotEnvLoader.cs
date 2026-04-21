namespace DashboardCompras.Configuration;

public static class DotEnvLoader
{
    public static void LoadIfPresent(string contentRootPath)
    {
        var filePath = FindDotEnvFile(contentRootPath);
        if (filePath is null)
        {
            return;
        }

        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string? FindDotEnvFile(string contentRootPath)
    {
        foreach (var basePath in EnumerateSearchRoots(contentRootPath))
        {
            var current = new DirectoryInfo(basePath);
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, ".env");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSearchRoots(string contentRootPath)
    {
        yield return contentRootPath;
        yield return AppContext.BaseDirectory;

        var currentDirectory = Environment.CurrentDirectory;
        if (!string.IsNullOrWhiteSpace(currentDirectory))
        {
            yield return currentDirectory;
        }
    }
}
