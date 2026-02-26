using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// GestureReceiver v2 — recibe mensajes UDP con formato "P1:JUMP", "P2:LEFT", etc.
// y dispara eventos separados para cada jugador.

public class GestureReceiver : MonoBehaviour
{
    [Header("Configuración UDP")]
    public int port = 5052;

    // ── Eventos por jugador ─────────────────────────────────────────────────
    public static event Action<string> OnGestureP1;
    public static event Action<string> OnGestureP2;

    // ── Variables internas ──────────────────────────────────────────────────
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;

    // Buffer thread-safe para pasar mensajes al hilo principal
    private string pendingPlayer = null;
    private string pendingMessage = null;
    private readonly object bufferLock = new object();

    void Start()
    {
        try
        {
            udpClient = new UdpClient(port);
            isRunning = true;
            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();
            Debug.Log($"[GestureReceiver] Escuchando en puerto {port} — modo 2 jugadores");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GestureReceiver] Error al abrir UDP: {e.Message}");
        }
    }

    // ── Hilo secundario: recibir paquetes ───────────────────────────────────
    void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data).Trim();

                // Formato esperado: "P1:JUMP", "P2:LEFT", etc.
                if (!string.IsNullOrEmpty(message) && message.Contains(":"))
                {
                    string[] parts = message.Split(':');
                    if (parts.Length == 2)
                    {
                        lock (bufferLock)
                        {
                            pendingPlayer = parts[0];   // "P1" o "P2"
                            pendingMessage = parts[1];   // "JUMP", "LEFT", etc.
                        }
                    }
                }
            }
            catch (SocketException) { }
            catch (Exception e)
            {
                Debug.LogWarning($"[GestureReceiver] {e.Message}");
            }
        }
    }

    // ── Hilo principal: despachar eventos ───────────────────────────────────
    void Update()
    {
        string player, msg;

        lock (bufferLock)
        {
            if (pendingPlayer == null) return;
            player = pendingPlayer;
            msg = pendingMessage;
            pendingPlayer = null;
        }

        Debug.Log($"[GestureReceiver] {player} → {msg}");

        if (player == "P1")
            OnGestureP1?.Invoke(msg);
        else if (player == "P2")
            OnGestureP2?.Invoke(msg);
    }

    void OnDestroy()
    {
        isRunning = false;
        udpClient?.Close();
        receiveThread?.Abort();
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        udpClient?.Close();
    }
}