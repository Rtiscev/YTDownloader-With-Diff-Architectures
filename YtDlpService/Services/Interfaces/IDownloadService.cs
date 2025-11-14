using System.Threading.Tasks;
using YtDlpService.Models;
using YtDlpService.DTOs;

namespace YtDlpService.Services.Interfaces;

public interface IDownloadService
{
    Task<CheckFileResponse> CheckFileExistsAsync(CheckFileRequest request);
    Task<DownloadAndUploadResponse> DownloadAndUploadAsync(
        DownloadRequest request,
        string bucketName = "my-bucket");
}
