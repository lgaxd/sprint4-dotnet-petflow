namespace PetFlowAPI.Extensions;

/// <summary>
/// Carrega variáveis de um arquivo <c>.env</c> para o ambiente do processo. Como o provider de variáveis de
/// ambiente do .NET traduz <c>__</c> em <c>:</c>, a chave <c>Jwt__Key</c> sobrescreve <c>Jwt:Key</c> do appsettings.
/// Variáveis já definidas no ambiente (ex.: CI, Docker) nunca são sobrescritas.
/// </summary>
public static class DotEnvLoader
{
    private const string FileName = ".env";
    private const int MaxLevelsUp = 5;

    /// <summary>Procura o .env a partir do diretório atual e do diretório da aplicação, subindo até 5 níveis.</summary>
    public static string? LoadFromDefaultLocations()
    {
        var path = FindFile(Directory.GetCurrentDirectory()) ?? FindFile(AppContext.BaseDirectory);
        if (path is not null) Load(path);
        return path;
    }

    public static string? FindFile(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        for (var level = 0; directory is not null && level <= MaxLevelsUp; level++, directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, FileName);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    public static void Load(string path)
    {
        foreach (var (key, value) in Parse(File.ReadAllLines(path)))
        {
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    public static IEnumerable<KeyValuePair<string, string>> Parse(IEnumerable<string> lines)
    {
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("export ", StringComparison.Ordinal)) line = line[7..].TrimStart();

            var separator = line.IndexOf('=');
            if (separator <= 0) continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' && value[^1] == '"' || value[0] == '\'' && value[^1] == '\''))
                value = value[1..^1];

            yield return new KeyValuePair<string, string>(key, value);
        }
    }
}
