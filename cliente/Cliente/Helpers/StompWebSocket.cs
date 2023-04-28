using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

public class StompWebSocket
{
    private WebSocket _webSocket;
    private readonly string _url;
    private readonly string _queueName;
    private readonly string _username;
    private readonly string _password;
    private string _uniqueSubscriptionId = "";
    private string _currentMessageId = "";
    public bool IsConnected { get; private set; } = true;
    public bool _connectReceiptReceived { get; private set; } = false;

    private bool _lastMessageAcknowledged = false;
    private bool _subscriptionActive = false;


    public event EventHandler<string> MessageReceived;
    public event EventHandler<string> Closed;

    public StompWebSocket(string url, string queueName, string username, string password)
    {
        _url = url;
        _queueName = queueName;
        _username = username;
        _password = password;
    }

    private void inizializeParams()
    {
        _uniqueSubscriptionId = "";
        _currentMessageId = "";
        IsConnected = true;
        _connectReceiptReceived = false;
        _lastMessageAcknowledged = false;
        _subscriptionActive = false;

    }

public void Connect()
    {
        _webSocket = new WebSocket(_url);
        _webSocket.MessageReceived += WebSocket_MessageReceived;
        _webSocket.Opened += WebSocket_Opened;
        _webSocket.Closed += WebSocket_Closed;
        _webSocket.Error += WebSocket_Error;
        _webSocket.Open();
    }

    private void WebSocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
    {
        Console.WriteLine($"Se Desconecta con error, Evento: {JsonConvert.SerializeObject(e)}");
    }

    public WebSocketState GetSocketState()
    {
        return _webSocket.State;
    }

    private async void WebSocket_Opened(object sender, EventArgs e)
    {
        Console.WriteLine($"Se Conecta, Evento: {JsonConvert.SerializeObject(e)}");
        await ConnectStompAsync();
        IsConnected = true;
    }

    private async void WebSocket_Closed(object sender, EventArgs e)
    {
        Console.WriteLine($"Se desconecto, Evento: {JsonConvert.SerializeObject(e)}");

        //Matar websocket y desfibrilador!!!

        _webSocket.Dispose();
        inizializeParams();
        //IsConnected = false;
        //_connectReceiptReceived = false;

        Connect();
        //await ConnectStompAsync();
    }

    private string GetMessageId(string stompMessage)
    {
        var lines = stompMessage.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("message-id:"))
            {
                return line.Substring("message-id:".Length);
            }
        }
        return null;
    }

    private async void WebSocket_MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Console.WriteLine($"Se recibe mensaje?, Evento: {JsonConvert.SerializeObject(e)}");
        try
        {
            var stompMessage = e.Message;

            if (stompMessage.Contains("CONNECTED"))
            {
                Console.WriteLine("El servidor aceptó la conexión, pero esperamos el recibo de conexión antes de suscribirnos");
                // El servidor aceptó la conexión, pero esperamos el recibo de conexión antes de suscribirnos

                if (!_connectReceiptReceived)
                {
                    Console.WriteLine("Ahora que se ha recibido el recibo, intentamos suscribirnos a la cola");
                    _connectReceiptReceived = true;
                    await SubscribeAsync(_queueName);
                }
            }
            else if (stompMessage.Contains("RECEIPT"))
            {
                if (stompMessage.Contains("receipt-id:ack-receipt"))
                {
                    // El ACK fue procesado correctamente por el servidor
                    _lastMessageAcknowledged = true;
                }
                else if (stompMessage.Contains("receipt-id:unsubscribe-receipt"))
                {
                    // El UNSUBSCRIBE fue procesado correctamente por el servidor
                    _subscriptionActive = false;
                    IsConnected = false;
                }
                else if (stompMessage.Contains("receipt-id:connect-receipt"))
                {
                    Console.WriteLine("Se ha recibido el recibo de conexión, intentamos suscribirnos a la cola");
                    // El SUSCRIBE fue procesado correctamente por el servidor
                    _subscriptionActive = true;
                    await SubscribeAsync(_queueName);
                }
            }
            else if (stompMessage == "\n")
            {
                //MessageReceived?.Invoke(this, stompMessage);
                Console.WriteLine(stompMessage);
            }
            else if (stompMessage != null)
            {
                if (stompMessage.Contains("MESSAGE"))
                {
                    _currentMessageId = GetMessageId(stompMessage);
                    Console.WriteLine(stompMessage);

                    stompMessage = $"{_currentMessageId}(messageId){stompMessage}";

                    MessageReceived?.Invoke(this, stompMessage);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private async Task ConnectStompAsync()
    {
        var connectMessage = $"CONNECT\nlogin:{_username}\npasscode:{_password}\naccept-version:1.2\nheart-beat:10000,10000\n\n\0";
        SendMessage(connectMessage);
    }
    public void UnsubscribeAsync()
    {
        if (IsConnected && !string.IsNullOrEmpty(_uniqueSubscriptionId))
        {
            if (_lastMessageAcknowledged)
            {
                var unsubscribeMessage = $"UNSUBSCRIBE\nid:{_uniqueSubscriptionId}\nreceipt:unsubscribe-receipt\n\n\0";
                SendMessage(unsubscribeMessage);

                _uniqueSubscriptionId = string.Empty;
                _subscriptionActive = false;
            }
            else
            {
                // Opcionalmente, manejar el caso en que el último mensaje no haya sido reconocido
                Console.WriteLine("No se puede cancelar la suscripción, el último mensaje no ha sido reconocido.");
            }
        }
        else if (!IsConnected)
        {
            Task.Run(ConnectStompAsync);
        }
    }

    public async Task SubscribeAsync(string queueName)
    {
        try
        {
            await AwaitForConnection();

            Thread.Sleep(TimeSpan.FromSeconds(3));

            if (IsConnected && !_subscriptionActive)
            {
                var uniqueSubscriptionId = Guid.NewGuid().ToString();
                var subscribeMessage = $"SUBSCRIBE\nid:{uniqueSubscriptionId}\ndestination:{queueName}\nack:auto\nreceipt:subscribe-receipt\n\n\0";
                _uniqueSubscriptionId = uniqueSubscriptionId;
                SendMessage(subscribeMessage);

                // Actualizar el estado de la suscripción
                _subscriptionActive = true;
            }
            else
            {
                // Manejar el caso en que no se haya establecido la conexión aún después de varios intentos
                Console.WriteLine("No se pudo suscribir a la cola después de varios intentos.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
    }

    public void SendStompAckAsync(string message)
    {
        if (String.IsNullOrWhiteSpace(message))
        {
            // Envía un ACK al servidor usando la conexión WebSocket
            SendMessage("ACK");

            _lastMessageAcknowledged = true;
        }
        else
        {
            // Envía un ACK con messageId para especificar a quien le envia ACK al servidor usando la conexión WebSocket
            var ackMessage = $"ACK\nid:{message}\nreceipt:ack-receipt\n\n\0";
            SendMessage(ackMessage);

            _lastMessageAcknowledged = true;
        }
    }

    private async Task AwaitForConnection()
    {
        const int maxRetryAttempts = 5;
        const int delayMilliseconds = 1000;

        int retryCount = 0;

        while (!IsConnected && retryCount < maxRetryAttempts)
        {
            // Espera antes de volver a intentar
            await Task.Delay(delayMilliseconds);
            retryCount++;
        }
    }

    public async void SendMessage(string message)
    {
        Console.WriteLine($"Estoy queriendo mandar un mensaje: {message}");
        await AwaitForConnection();
        if (IsConnected)
        {
            Console.WriteLine("ENVIANDO " + message);
            _webSocket.Send(message);
        }
        else
        {
            // Manejar el caso en que no se haya establecido la conexión aún después de varios intentos
            Console.WriteLine("No se pudo enviar el mensaje después de varios intentos.");
        }
    }

    //public async Task SendMessage(string message)
    //{
    //    await AwaitForConnection();

    //    if (IsConnected)
    //    {
    //        Console.WriteLine("ENVIANDO " + message);
    //        await _webSocket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None);
    //    }
    //    else
    //    {
    //        // Manejar el caso en que no se haya establecido la conexión aún después de varios intentos
    //        Console.WriteLine("No se pudo enviar el mensaje después de varios intentos.");
    //    }
    //}


}
