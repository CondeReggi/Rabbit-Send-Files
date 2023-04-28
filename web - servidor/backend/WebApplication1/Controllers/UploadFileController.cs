using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using WebApplication1.Helpers;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadFileController : ControllerBase
    {
        private readonly ILogger<UploadFileController> _logger;
        private readonly FileSettings _fileSettings;
        private readonly RabbitMqHelper _rabbitMqHelper;
        private readonly FileHelper _fileHelper;

        public UploadFileController(ILogger<UploadFileController> logger, 
            IOptions<FileSettings> fileSettings, 
            IOptions<RabbitMqSettings> rabbitMqSettings, 
            RabbitMqHelper rabbitMqHelper,
            FileHelper fileHelper)
        {
            _logger = logger;
            _fileSettings = fileSettings.Value;
            _rabbitMqHelper = rabbitMqHelper;
            _fileHelper = fileHelper;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No se ha proporcionado un archivo.");
            }

            var tempFolderPath = Path.Combine(Directory.GetCurrentDirectory(), _fileSettings.TempFolder);
            Directory.CreateDirectory(tempFolderPath);

            var filePath = Path.Combine(tempFolderPath, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            if (file.Length > 8 * 1024 * 1024) // 20 MB => ahora lo tengo con 8 MB porque no tengo ninugno de mas de 20 mb entonces necesito ver si lo fracciona
            {
                _fileHelper.DivideFile(file, filePath, tempFolderPath);
            }
            else
            {
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    fileBytes = ms.ToArray();
                }

                var fileChunkInfo = new FileChunkInfo
                {
                    FileName = file.FileName,
                    TotalChunks = 1,
                    CurrentChunkIndex = 0,
                    FileChunkData = Convert.ToBase64String(fileBytes)
                };

                var jsonMessage = JsonConvert.SerializeObject(fileChunkInfo);
                _rabbitMqHelper.SendMessageToQueue(jsonMessage);
                // Procesar el archivo y enviar a la cola de mensajería
                //_rabbitMqHelper.SendMessageToQueue($"Archivo: {file.FileName}, Tamaño: {file.Length}");
            }

            return Ok(new { status = "Archivo cargado y procesado con éxito" });
        }
    }
}
