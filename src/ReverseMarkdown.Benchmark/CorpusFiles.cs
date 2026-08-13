namespace ReverseMarkdown.Benchmark;

/// <summary>
/// Locates the gitignored benchmark corpus and fails with an actionable message when it is missing,
/// rather than with a bare FileNotFoundException from inside a benchmark run.
/// </summary>
public static class CorpusFiles
{
    public const string FetchHint =
        "Benchmark corpus not found. Run scripts/fetch-benchmark-corpus.sh first " +
        "(the fixtures are third-party content and are not committed).";

    public static string Directory =>
        Path.Combine(AppContext.BaseDirectory, "Files", "corpus");

    public static bool Exists(string fixture) => File.Exists(Path.Combine(Directory, fixture));

    public static string Read(string fixture)
    {
        var path = Path.Combine(Directory, fixture);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"{FetchHint} Missing: {path}", path);
        }

        return File.ReadAllText(path);
    }
}
