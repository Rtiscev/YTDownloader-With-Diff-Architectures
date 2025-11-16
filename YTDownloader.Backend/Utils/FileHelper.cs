namespace YTDownloader.Backend.Utils;

public static class FileHelper
{
    public static string SanitizeFilename(string filename) =>
        new(filename.Where(c => c < 128).ToArray());

    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:F2} {sizes[order]}";
    }
}
