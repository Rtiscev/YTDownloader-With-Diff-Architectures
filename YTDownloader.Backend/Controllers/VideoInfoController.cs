using Microsoft.AspNetCore.Mvc;
using YTDownloader.Backend.Services.Interfaces;
using YTDownloader.Backend.Services.Implementations;

namespace YTDownloader.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class VideoInfoController : ControllerBase
{
    private readonly IYtDlpExecutor _ytDlpExecutor;
    private readonly ILogger<VideoInfoController> _logger;

    public VideoInfoController(IYtDlpExecutor ytDlpExecutor, ILogger<VideoInfoController> logger)
    {
        _ytDlpExecutor = ytDlpExecutor;
        _logger = logger;
    }

    [HttpGet("/")]
    public async Task<IActionResult> GetVideoInfo([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { error = "URL parameter is required" });

        var result = await _ytDlpExecutor.GetVideoInfoAsync(url);
        return result.Success
            ? Ok(result)
            : Problem(detail: result.ErrorMessage, statusCode: 500);
    }
}
