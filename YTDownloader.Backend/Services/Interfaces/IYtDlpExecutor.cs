using YTDownloader.Backend.Models;
using System.Threading.Tasks;

namespace YTDownloader.Backend.Services.Interfaces;

public interface IYtDlpExecutor
{
    Task<string> GetYtDlpVersionAsync();
    Task<string> GetFfmpegVersionAsync();
    Task<DownloadResponse> DownloadVideoAsync(DownloadRequest request);
    Task<VideoInfoResponse> GetVideoInfoAsync(string url);
}
