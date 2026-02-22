using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;


public class GestureReceiver : MonoBehaviour
{
    [Header("Configuración UDP")]
    [Tooltip("Puerto UDP — debe coincidir con gesture_sender.py")]
    public int port = 5052;

    // ── Eventos públicos ────────────────────────────────────────────────────
    // Otros scripts se suscriben así:
    //   GestureReceiver.OnGesture += MiMetodo;
    public static event Action<string> OnGesture;

    // ── Variables internas ──────────────────────────────────────────────────
    private UdpClient    udpClient;
    private Thread       receiveThread;
    private bool         isRunning = false;
    private string       lastGesture = "";
    private bool         newGesture  = false;   // flag para hilo principal

    // ── Unity: iniciar ──────────────────────────────────────────────────────
    void Start()
    {
        try
        {
            udpClient  = new UdpClient(port);
            isRunning  = true;
            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();
            Debug.Log($"[GestureReceiver] Escuchando UDP en puerto {port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GestureReceiver] Error al abrir UDP: {e.Message}");
        }
    }

    // ── Hilo secundario: recibir paquetes UDP ───────────────────────────────
    // ⚠️ No llamar a APIs de Unity desde aquí (solo desde el hilo principal).
    void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data    = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data).Trim();

                if (!string.IsNullOrEmpty(message))
                {
                    lastGesture = message;
                    newGesture  = true;   // el hilo principal lo procesará en Update()
                }
            }
            catch (SocketException)
            {
                // Se lanza al cerrar el socket — es normal, ignorar.
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GestureReceiver] {e.Message}");
            }
        }
    }

    // ── Unity: hilo principal — procesar gestos recibidos ───────────────────
    void Update()
    {
        if (newGesture)
        {
            newGesture = false;
            ProcessGesture(lastGesture);
        }
    }

    // ── Procesar gesto y disparar evento ───────────────────────────────────
    void ProcessGesture(string gesture)
    {
        Debug.Log($"[GestureReceiver] Gesto recibido: {gesture}");
        OnGesture?.Invoke(gesture);   // notifica a todos los suscriptores
    }

    // ── Unity: limpiar al cerrar ────────────────────────────────────────────
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