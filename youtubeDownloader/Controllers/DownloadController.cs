using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using YoutubeExplode.Common;

namespace youtubeDownloader.Controllers
{
    [ApiController]
    [Route("api/youtube/[controller]")]

    public class DownloadController : ControllerBase
    {
        private readonly YoutubeClient _youtubeClient;

        private static int _requestCount = 0;

        private readonly ILogger<DownloadController> _logger;

        public DownloadController(ILogger<DownloadController> logger)
        {
            _youtubeClient = new YoutubeClient();
            _logger = logger;

        }

        [HttpGet("GetDownload")]
        public async Task<IActionResult> DownloadVideo(string videoUrl, string format = "mp4")
        {
            if (string.IsNullOrWhiteSpace(videoUrl))
            {
                return BadRequest("URL de YouTube no proporcionada.");
            }
            try
            {
                IncrementRequestCount();
                var video = await _youtubeClient.Videos.GetAsync(videoUrl);
                var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);
                IStreamInfo streamInfo;

                if (format.ToLower() == "mp3")
                {
                    streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                }
                else
                {
                    streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
                }

                if (streamInfo != null)
                {
                    var downloadUrl = streamInfo.Url;
                    var imgUrl = video.Thumbnails.GetWithHighestResolution();

                    return Ok(new
                    {
                        video.Title,
                        video.Author,
                        video.Duration,
                        downloadUrl,
                        ThumbnailUrl = imgUrl.Url,
                        _requestCount,
                        format
                    });
                }
                else
                {
                    return NotFound("No se encontró un stream adecuado para este video.");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error al realizar la solicitud HTTP.");
                return StatusCode(502, $"Error en la solicitud HTTP: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado.");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private void IncrementRequestCount()
        {
            _requestCount++;
            _logger.LogInformation($"Número total de solicitudes hasta ahora: {_requestCount}");
        }


    }
}

