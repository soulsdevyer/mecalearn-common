using System;
using System.IO.Ports;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    // Configuración del puerto serial y el servidor WebSocket
    static SerialPort serialPort = new SerialPort("COM14", 9600); // Ajusta el COM y baud rate según tu Arduino
    static HttpListener httpListener = new HttpListener();
    static string wsAddress = "ws://localhost:8080/arduino";
    static string latestSensorData = "Esperando datos del sensor..."; // Dato inicial

    static async Task Main(string[] args)
    {
        // Inicia el puerto serial
        serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        serialPort.Open();

        // Inicia el servidor WebSocket
        httpListener.Prefixes.Add("http://localhost:8080/arduino/");
        httpListener.Start();
        Console.WriteLine("WebSocket server is listening on ws://localhost:8080/arduino");

        while (true)
        {
            // Acepta la conexión WebSocket
            var context = await httpListener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                Console.WriteLine("Client connected");
                var wsContext = await context.AcceptWebSocketAsync(null);
                _ = Task.Run(() => HandleWebSocket(wsContext.WebSocket)); // Inicia una tarea para manejar el WebSocket
            }
        }
    }

    static async Task HandleWebSocket(WebSocket webSocket)
    {
        var buffer = new byte[1024];
        while (webSocket.State == WebSocketState.Open)
        {
            // Envía los datos del sensor recibidos por el puerto serial al cliente WebSocket
            if (!string.IsNullOrEmpty(latestSensorData))
            {
                var dataBuffer = Encoding.UTF8.GetBytes(latestSensorData);
                await webSocket.SendAsync(new ArraySegment<byte>(dataBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }

            // Espera un segundo antes de enviar los datos nuevamente
            await Task.Delay(1000);
        }
    }

    // Manejador de eventos cuando se reciben datos del puerto serial
    private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        string inData = serialPort.ReadLine(); // Lee los datos del sensor
        Console.WriteLine("Received data: " + inData);
        latestSensorData = inData; // Almacena los últimos datos para ser enviados al WebSocket
    }
}