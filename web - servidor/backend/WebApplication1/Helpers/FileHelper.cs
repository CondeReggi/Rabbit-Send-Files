using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Helpers
{
    public class FileHelper
    {
        private readonly RabbitMqHelper _rabbitMqHelper;
        public FileHelper(RabbitMqHelper rabbitMqHelper)
        {
            _rabbitMqHelper = rabbitMqHelper;
        }
        public void DivideFile(IFormFile file, string filePath, string tempFolderPath)
        {
            int partIndex = 0;
            var chunkSize = 20 * 1024 * 1024;
            var buffer = new byte[chunkSize];
            int bytesRead;

            using (var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int totalChunks = (int)Math.Ceiling((double)file.Length / chunkSize);

                while ((bytesRead = inputStream.Read(buffer, 0, chunkSize)) > 0)
                {
                    var fileChunkData = Convert.ToBase64String(buffer, 0, bytesRead);

                    var fileChunkInfo = new FileChunkInfo
                    {
                        FileName = file.FileName,
                        TotalChunks = totalChunks,
                        CurrentChunkIndex = partIndex,
                        FileChunkData = fileChunkData
                    };

                    var jsonMessage = JsonConvert.SerializeObject(fileChunkInfo);

                    // Enviar parte del archivo a la cola de mensajería
                    _rabbitMqHelper.SendMessageToQueue(jsonMessage);
                    partIndex++;
                }
            }
        }
    }
}
