using System;
using System.Threading.Tasks;
using WebSocket4Net;

public class StompWebSocket
{
    private WebSocket _webSocket;
    private readonly string _url;
    private readonly string _queueName;
    private readonly string _username;
    private readonly string _password;
    public bool IsConnected { get; private set; } = true;
    public bool _connectReceiptReceived { get; private set; } = false;


    public event EventHandler<string> MessageReceived;

    public StompWebSocket(string url, string queueName, string username, string password)
    {
        _url = url;
        _queueName = queueName;
        _username = username;
        _password = password;
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
        // Aquí puedes manejar los errores de WebSocket
    }

    private async void WebSocket_Opened(object sender, EventArgs e)
    {
        await ConnectStompAsync();
        IsConnected = true;
    }

    private async void WebSocket_Closed(object sender, EventArgs e)
    {

        Connect();
        // Gestiona la desconexión del WebSocket si es necesario
    }

    private async void WebSocket_MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        try
        {
            var stompMessage = e.Message;

            Console.WriteLine(stompMessage);

            if (stompMessage.Contains("CONNECTED"))
            {
                Console.WriteLine("El servidor aceptó la conexión, pero esperamos el recibo de conexión antes de suscribirnos");
                // El servidor aceptó la conexión, pero esperamos el recibo de conexión antes de suscribirnos

                Console.WriteLine("Ahora que se ha recibido el recibo, intentamos suscribirnos a la cola");
                // Ahora que se ha recibido el recibo, intentamos suscribirnos a la cola
                if (!_connectReceiptReceived)
                {
                    _connectReceiptReceived = true;
                    await SubscribeAsync(_queueName);
                }
            }
            else if (stompMessage.Contains("RECEIPT") && stompMessage.Contains("receipt-id:connect-receipt"))
            {
                // Ahora que se ha recibido el recibo, intentamos suscribirnos a la cola
                //if (!_connectReceiptReceived)
                //{
                //    _connectReceiptReceived = true;
                //    await SubscribeAsync(_queueName);
                //}

                Console.WriteLine("Suscripción a la cola confirmada.");
            }
            else if (stompMessage == "\n")
            {
                //MessageReceived?.Invoke(this, stompMessage);
                Console.WriteLine(stompMessage);
            }
            else if (stompMessage != null)
            {
                Console.WriteLine(stompMessage);
                MessageReceived?.Invoke(this, stompMessage);
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

    public async Task SubscribeAsync(string queueName)
    {
        await AwaitForConnection();

        if (IsConnected)
        {
            var uniqueSubscriptionId = Guid.NewGuid().ToString();
            var subscribeMessage = $"SUBSCRIBE\nid:{uniqueSubscriptionId}\ndestination:{queueName}\nack:auto\nreceipt:subscribe-receipt\n\n\0";
            SendMessage(subscribeMessage);
        }
        else
        {
            // Manejar el caso en que no se haya establecido la conexión aún después de varios intentos
            Console.WriteLine("No se pudo suscribir a la cola después de varios intentos.");
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

}
