﻿using Cliente.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace Cliente.Helpers
{
    public class FileProcessingApp
    {
        private readonly RabbitMqSettings _rabbitMqSettings;
        private readonly StompWebSocket _stompWebSocket;
        //private readonly ILogger _logger;
        private readonly FileRepository _fileRepository;

        //public FileProcessingApp(IOptions<RabbitMqSettings> rabbitMqSettings, ILogger logger)
        public FileProcessingApp(IOptions<RabbitMqSettings> rabbitMqSettings, FileRepository fileRepository)
        {
            _rabbitMqSettings = rabbitMqSettings.Value;
            _stompWebSocket = new StompWebSocket("ws://localhost:15674/ws", _rabbitMqSettings.QueueName, _rabbitMqSettings.Username, _rabbitMqSettings.Password); // Cambia a la dirección de tu servidor
            //_stompWebSocket.MessageReceived += StompWebSocket_MessageReceived;

            //_logger = logger;
            _fileRepository = fileRepository;   
        }

        public void RunWithAMQP()
        {
            //var factory = new ConnectionFactory() { HostName = _rabbitMqSettings.HostName };
            //using (var connection = factory.CreateConnection())
            //using (var channel = connection.CreateModel())
            //{
            //    channel.QueueDeclare(queue: _rabbitMqSettings.QueueName,
            //                         durable: false,
            //                         exclusive: false,
            //                         autoDelete: false,
            //                         arguments: null);

            //    var consumer = new EventingBasicConsumer(channel);
            //    consumer.Received += (model, ea) =>
            //    {
            //        var body = ea.Body.ToArray();
            //        var message = Encoding.UTF8.GetString(body);
            //        Console.WriteLine($"Mensaje recibido: {message}");
            //    };

            //    channel.BasicConsume(queue: _rabbitMqSettings.QueueName,
            //                 autoAck: true,
            //                 consumer: consumer);

            //    Console.WriteLine("Presione [enter] para salir.");
            //    Console.ReadLine();
            //}
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // Conéctate al servidor STOMP
            _stompWebSocket.Connect();

            _stompWebSocket.MessageReceived += StompWebSocket_MessageReceived;

            // Me suscribo a la cola que tengo en el appsettings
            //SubscribeToQueueAsync();

            // Espera a que se cancele la aplicación
            await Task.Delay(-1, cancellationToken);
        }

        private void ProcessFile(string message)
        {
            // Aquí puedes implementar la lógica para procesar el archivo
            // según la información recibida en el mensaje
            Console.WriteLine($"Procesando archivo: {message}");
        }

        private async void StompWebSocket_MessageReceived(object sender, string message)
        {
            if (message.Contains("SUBSCRIBE") || message.Contains("CONNECT")) return;

            //_logger.LogInformation("Message received: {0}", message);

            // 1. Parsea el mensaje STOMP
            try
            {
                var (fileName, totalChunks, currentChunkIndex, fileChunkData) = ParseStompMessage(message);

                if (String.IsNullOrEmpty(fileName)) return;

                Console.WriteLine("LLego: " + fileName);
                // (aquí deberías parsear el mensaje STOMP y extraer la información relevante, como el nombre del archivo, el índice del chunk, etc.)

                // 2. Guarda el estado del archivo en la base de datos
                var fileRecord = await _fileRepository.GetFileRecordByIdAsync(fileName.GetHashCode());

                if (fileRecord == null)
                {
                    fileRecord = new FileRecord
                    {
                        FileName = fileName,
                        TotalChunks = totalChunks,
                        ReceivedChunks = 1
                    };
                    await _fileRepository.AddFileRecordAsync(fileRecord);
                }
                else
                {
                    fileRecord.ReceivedChunks = currentChunkIndex;
                    await _fileRepository.UpdateFileRecordAsync(fileRecord);
                }

                // 3. Procesa el chunk de archivo
                string chunksDirectory = "chunks";
                Directory.CreateDirectory(chunksDirectory);
                string chunkFile = Path.Combine(chunksDirectory, $"{fileName}.part{currentChunkIndex}");

                await File.WriteAllBytesAsync(chunkFile, fileChunkData);

                // 4. Envía un ACK al servidor
                SendStompAckAsync(_stompWebSocket, message);
                // (aquí deberías enviar un ACK al servidor usando la conexión STOMP)

                // 5. Verifica si se han recibido todos los chunks
                if (fileRecord.ReceivedChunks == fileRecord.TotalChunks)
                {
                    // Combina todos los chunks en el archivo final y elimina el registro de la base de datos
                    await CombineFileChunksAsync(fileName, totalChunks);
                    await _fileRepository.DeleteFileRecordAsync(fileRecord.Id);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private string ExtractJsonContentFromStompMessage(string stompMessage)
        {
            int indexOfEmptyLine = stompMessage.IndexOf("\n\n");
            if (indexOfEmptyLine >= 0)
            {
                return stompMessage.Substring(indexOfEmptyLine + 2);
            }

            return "";
        }

        private (string fileName, int totalChunks, int currentChunkIndex, byte[] fileChunkData) ParseStompMessage(string message)
        {
            string jsonContent = ExtractJsonContentFromStompMessage(message);
            // Asume que el mensaje STOMP tiene un formato JSON
            var messageData = JsonConvert.DeserializeObject<FileChunkInfo>(jsonContent);

            if (messageData == null) return ("", 0, 0, null);

            string fileName = messageData.FileName;
            int totalChunks = messageData.TotalChunks;
            int currentChunkIndex = messageData.CurrentChunkIndex;
            byte[] fileChunkData = Convert.FromBase64String((string)messageData.FileChunkData);

            return (fileName, totalChunks, currentChunkIndex, fileChunkData);
        }

        private void SendStompAckAsync(StompWebSocket stompWebSocket, string message)
        {
            // Envía un ACK al servidor usando la conexión WebSocket
            stompWebSocket.SendMessage("ACK");
        }

        private async Task CombineFileChunksAsync(string fileName, int totalChunks)
        {
            string chunksDirectory = "chunks";
            string outputFile = Path.Combine("output", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < totalChunks; i++)
                {
                    string chunkFile = Path.Combine(chunksDirectory, $"{fileName}.part{i}");
                    using (var inputStream = new FileStream(chunkFile, FileMode.Open, FileAccess.Read))
                    {
                        await inputStream.CopyToAsync(outputStream);
                    }
                    File.Delete(chunkFile);
                }
            }
        }
        //private async Task SubscribeToQueueAsync()
        //{
        //    await _stompWebSocket.SubscribeAsync(_rabbitMqSettings.QueueName);
        //}
    }
}
