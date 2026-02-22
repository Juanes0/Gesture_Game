import cv2
import mediapipe as mp
import socket
import time

# ── Configuración UDP ──────────────────────────────────────────────────────────
UDP_IP   = "127.0.0.1"   # localhost (Python y Unity en la misma PC)
UDP_PORT = 5052           # debe coincidir con el script de Unity
sock     = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# ── MediaPipe Hands ────────────────────────────────────────────────────────────
mp_hands   = mp.solutions.hands
mp_drawing = mp.solutions.drawing_utils
hands      = mp_hands.Hands(
    static_image_mode        = False,   # modo video (no foto)
    max_num_hands            = 2,       # detectar hasta 2 manos
    min_detection_confidence = 0.7,
    min_tracking_confidence  = 0.6
)

# ── Función: detectar si la mano está abierta ──────────────────────────────────
# MediaPipe devuelve 21 landmarks por mano.
# Comparamos la punta de cada dedo con su nudillo para saber si está extendido.
#
#  Landmark IDs:
#   Pulgar : punta=4,  base=2
#   Índice : punta=8,  base=6
#   Medio  : punta=12, base=10
#   Anular : punta=16, base=14
#   Meñique: punta=20, base=18

def dedos_extendidos(landmarks):
    """Devuelve una lista [pulgar, indice, medio, anular, menique] con True/False."""
    tips   = [4, 8, 12, 16, 20]
    bases  = [2, 6, 10, 14, 18]
    estado = []
    for tip, base in zip(tips, bases):
        # Para el pulgar usamos eje X; para el resto, eje Y (arriba = menor Y)
        if tip == 4:
            estado.append(landmarks[tip].x < landmarks[base].x)
        else:
            estado.append(landmarks[tip].y < landmarks[base].y)
    return estado

def detectar_gesto(landmarks):
    """Devuelve el nombre del gesto detectado o None."""
    dedos = dedos_extendidos(landmarks)
    abiertos = sum(dedos)

    if abiertos >= 4:
        return "JUMP"      # mano abierta  → saltar
    elif abiertos == 0:
        return "ATTACK"    # puño cerrado  → atacar
    elif dedos[1] and not dedos[2] and not dedos[3] and not dedos[4]:
        return "SHOOT"     # solo índice extendido → disparar
    else:
        return None        # gesto no reconocido

# ── Bucle principal ────────────────────────────────────────────────────────────
cap              = cv2.VideoCapture(0)   # 0 = primera webcam
ultimo_gesto     = None
tiempo_ultimo    = 0
COOLDOWN         = 0.3   # segundos entre envíos del mismo gesto (evita spam)

print("=== Gesture Sender iniciado ===")
print(f"Enviando a Unity en {UDP_IP}:{UDP_PORT}")
print("Gestos:")
print("  Mano abierta  → JUMP")
print("  Puño cerrado  → ATTACK")
print("  Solo índice   → SHOOT")
print("Presiona 'q' para salir\n")

while cap.isOpened():
    ret, frame = cap.read()
    if not ret:
        print("ERROR: No se puede leer la cámara.")
        break

    # Voltear horizontalmente (efecto espejo, más intuitivo)
    frame = cv2.flip(frame, 1)

    # Convertir BGR → RGB para MediaPipe
    rgb    = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    result = hands.process(rgb)

    gesto_actual = None

    if result.multi_hand_landmarks:
        for idx, hand_landmarks in enumerate(result.multi_hand_landmarks):
            # Dibujar los landmarks sobre el frame
            mp_drawing.draw_landmarks(
                frame,
                hand_landmarks,
                mp_hands.HAND_CONNECTIONS,
                mp_drawing.DrawingSpec(color=(0, 255, 0), thickness=2, circle_radius=4),
                mp_drawing.DrawingSpec(color=(255, 255, 255), thickness=2)
            )

            # Detectar gesto de la primera mano encontrada
            if idx == 0:
                gesto_actual = detectar_gesto(hand_landmarks.landmark)

    # Enviar por UDP si hay gesto y pasó el cooldown
    ahora = time.time()
    if gesto_actual and (ahora - tiempo_ultimo) > COOLDOWN:
        mensaje = gesto_actual.encode('utf-8')
        sock.sendto(mensaje, (UDP_IP, UDP_PORT))
        tiempo_ultimo = ahora
        ultimo_gesto  = gesto_actual
        print(f"Enviado → {gesto_actual}")

    # ── HUD en pantalla ────────────────────────────────────────────────────────
    color_hud = (0, 255, 0) if gesto_actual else (100, 100, 100)
    texto_hud = f"Gesto: {gesto_actual if gesto_actual else '---'}"
    cv2.putText(frame, texto_hud, (10, 40),
                cv2.FONT_HERSHEY_SIMPLEX, 1.2, color_hud, 2)
    cv2.putText(frame, f"Ultimo enviado: {ultimo_gesto or '---'}", (10, 80),
                cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 0), 2)
    cv2.putText(frame, "Presiona Q para salir", (10, frame.shape[0] - 15),
                cv2.FONT_HERSHEY_SIMPLEX, 0.6, (200, 200, 200), 1)

    cv2.imshow("Gesture Sender - MediaPipe", frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# ── Limpieza ───────────────────────────────────────────────────────────────────
cap.release()
cv2.destroyAllWindows()
sock.close()
print("Gesture Sender cerrado.")