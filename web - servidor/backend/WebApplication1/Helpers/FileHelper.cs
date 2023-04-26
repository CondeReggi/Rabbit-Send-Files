using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            var chunkSize = 20 * 1024 * 1024;
            var buffer = new byte[chunkSize];
            int bytesRead;

            using (var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int partIndex = 0;
                while ((bytesRead = inputStream.Read(buffer, 0, chunkSize)) > 0)
                {
                    var partFileName = $"{file.FileName}.part{partIndex}";
                    var partFilePath = Path.Combine(tempFolderPath, partFileName);

                    using (var outputStream = new FileStream(partFilePath, FileMode.Create, FileAccess.Write))
                    {
                        outputStream.Write(buffer, 0, bytesRead);
                    }

                    // Enviar parte del archivo a la cola de mensajería
                    _rabbitMqHelper.SendMessageToQueue($"Archivo: {partFileName}, Tamaño: {bytesRead}");
                    partIndex++;
                }
            }
        }
    }
}
