using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Globalization;
public class GestureReceiver : MonoBehaviour
{
    [Header("Configuración UDP")]
    public int port = 5052;

    // Último estado recibido (thread-safe)
    public static (float x, float y, string gesture) Player1State = (0.75f, 0.5f, "NONE");
    public static (float x, float y, string gesture) Player2State = (0.25f, 0.5f, "NONE");

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;

    private readonly ConcurrentQueue<string> messageQueue = new();

    void Start()
    {
        try
        {
            udpClient = new UdpClient(port);
            isRunning = true;
            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();
            Debug.Log($"[GestureReceiver] ✅ Escuchando puerto {port} - Nuevo sistema X/Y/G listo");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GestureReceiver] Error al abrir UDP: {e.Message}");
        }
    }

    private void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string raw = Encoding.UTF8.GetString(data).Trim();
                if (!string.IsNullOrEmpty(raw))
                    messageQueue.Enqueue(raw);
            }
            catch { }
        }
    }

    void Update()
    {
        int processed = 0;
        while (messageQueue.TryDequeue(out string raw))
        {
            processed++;
            if (!raw.Contains(":X:") || !raw.Contains(":Y:") || !raw.Contains(":G:")) continue;

            string[] parts = raw.Split(':');
            if (parts.Length != 7) continue;   // ← AQUÍ ESTABA EL ERROR (era < 8)

            string player = parts[0].Trim();
            float x = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
            float y = float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture);
            string gesto = parts[6].Trim().ToUpper();

            if (player == "P1")
                Player1State = (x, y, gesto);
            else if (player == "P2")
                Player2State = (x, y, gesto);

            // Debug útil (puedes comentarlo después)
            // Debug.Log($"[Gesture] {player} → X:{x:F3} Y:{y:F3} G:{gesto}");
        }

        if (processed > 0)
            Debug.Log($"[GestureReceiver] Procesados {processed} mensajes este frame");
    }

    void OnDestroy()
    {
        isRunning = false;
        udpClient?.Close();
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        udpClient?.Close();
    }
}