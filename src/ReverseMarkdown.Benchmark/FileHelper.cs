namespace ReverseMarkdown.Benchmark {
    internal sealed class FileHelper {
        public static string ReadFile(string path)
        {
            return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, path));
        }
    }
}
