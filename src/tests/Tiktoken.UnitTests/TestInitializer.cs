namespace Tiktoken.UnitTests;

[TestClass]
public static class TestInitializer
{
    [AssemblyInitialize]
    public static void Initialize(TestContext _)
    {
        LoadEnvFile();
    }

    private static void LoadEnvFile()
    {
        // Walk up from the test output directory to find .env at repo root
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var envPath = Path.Combine(dir, ".env");
            if (File.Exists(envPath))
            {
                foreach (var line in File.ReadAllLines(envPath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                    {
                        continue;
                    }

                    var eqIndex = trimmed.IndexOf('=');
                    if (eqIndex <= 0)
                    {
                        continue;
                    }

                    var key = trimmed[..eqIndex].Trim();
                    var value = trimmed[(eqIndex + 1)..].Trim();

                    // Only set if not already set (env vars take precedence)
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    {
                        Environment.SetEnvironmentVariable(key, value);
                    }
                }

                return;
            }

            dir = Path.GetDirectoryName(dir);
        }
    }
}
