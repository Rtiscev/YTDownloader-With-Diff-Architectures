using YtDlpService.Models;
using System.Threading.Tasks;

namespace YtDlpService.Services.Interfaces;

public interface IYtDlpExecutor
{
    Task<string> GetYtDlpVersionAsync();
    Task<string> GetFfmpegVersionAsync();
    Task<DownloadResponse> DownloadVideoAsync(DownloadRequest request);
    Task<VideoInfoResponse> GetVideoInfoAsync(string url);
}
