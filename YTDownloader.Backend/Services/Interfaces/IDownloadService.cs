using System.Threading.Tasks;
using YTDownloader.Backend.Models;
using YTDownloader.Backend.DTOs;

namespace YTDownloader.Backend.Services.Interfaces;

public interface IDownloadService
{
    Task<YtDlpCheckFileResponse> CheckFileExistsAsync(YtDlpCheckFileRequest request);
    Task<DownloadAndUploadResponse> DownloadAndUploadAsync(
        DownloadRequest request,
        string bucketName = "my-bucket");
}
