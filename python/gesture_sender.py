import cv2
import mediapipe as mp
import socket
import time
import win32gui
import win32con

# ── Configuración UDP ──────────────────────────────────────────────────────────
UDP_IP   = "127.0.0.1"
UDP_PORT = 5052
sock     = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# ── MediaPipe ──────────────────────────────────────────────────────────────────
mp_hands   = mp.solutions.hands
mp_drawing = mp.solutions.drawing_utils

hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=2,
    min_detection_confidence=0.7,
    min_tracking_confidence=0.6
)

COOLDOWN_POSICION = 0.033
COOLDOWN_GESTO    = 0.4

WINDOW_NAME = "Gesture Overlay"

# ── Configuración jugadores ───────────────────────────────────────────────────
JUGADORES = {
    "P1": {
        "hand_label": "Right",
        "zona_min": 0.5,
        "zona_max": 1.0,
    },
    "P2": {
        "hand_label": "Left",
        "zona_min": 0.0,
        "zona_max": 0.5,
    },
}

COLORES = {
    "P1": (0, 200, 255),
    "P2": (255, 130, 0)
}

# ── Helpers ────────────────────────────────────────────────────────────────────
def dedos_extendidos(landmarks):
    tips  = [4, 8, 12, 16, 20]
    bases = [2, 6, 10, 14, 18]

    estado = []

    for tip, base in zip(tips, bases):
        if tip == 4:
            estado.append(landmarks[tip].x < landmarks[base].x)
        else:
            estado.append(landmarks[tip].y < landmarks[base].y)

    return estado

def detectar_gesto(landmarks):

    dedos = dedos_extendidos(landmarks)
    abiertos = sum(dedos)

    if abiertos == 0:
        return "ATTACK"

    elif dedos[1] and not dedos[2] and not dedos[3] and not dedos[4]:
        return "SHOOT"

    return "NONE"

def validar_mano(label_detectado, munieca_x, config):

    label_ok = (label_detectado == config["hand_label"])
    zona_ok  = (config["zona_min"] <= munieca_x <= config["zona_max"])

    return label_ok and zona_ok

def enviar_estado(jugador, x, y, gesto):

    mensaje = f"{jugador}:X:{x:.3f}:Y:{y:.3f}:G:{gesto}"

    sock.sendto(
        mensaje.encode('utf-8'),
        (UDP_IP, UDP_PORT)
    )

# ── Cámara ─────────────────────────────────────────────────────────────────────
cap = cv2.VideoCapture(0)

cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)
cap.set(cv2.CAP_PROP_FPS, 30)

if not cap.isOpened():
    print("❌ No se pudo abrir la cámara")
    exit()

# ── Crear ventana overlay ─────────────────────────────────────────────────────
cv2.namedWindow(WINDOW_NAME, cv2.WINDOW_NORMAL)

# Tamaño pequeño tipo HUD
cv2.resizeWindow(WINDOW_NAME, 480, 270)

# Posición esquina superior derecha
cv2.moveWindow(WINDOW_NAME, 1400, 20)

# Obtener handle ventana
hwnd = win32gui.FindWindow(None, WINDOW_NAME)

# Quitar bordes
style = win32gui.GetWindowLong(hwnd, win32con.GWL_STYLE)

win32gui.SetWindowLong(
    hwnd,
    win32con.GWL_STYLE,
    style & ~win32con.WS_OVERLAPPEDWINDOW
)

# Mantener siempre arriba
win32gui.SetWindowPos(
    hwnd,
    win32con.HWND_TOPMOST,
    1400,
    20,
    480,
    270,
    win32con.SWP_SHOWWINDOW
)

print("✅ Overlay iniciado")

# ── Timers ─────────────────────────────────────────────────────────────────────
tiempos = {
    "P1": {"gesto": 0, "movimiento": 0},
    "P2": {"gesto": 0, "movimiento": 0},
}

# ── Bucle principal ───────────────────────────────────────────────────────────
while cap.isOpened():

    ret, frame = cap.read()

    if not ret:
        break

    frame = cv2.flip(frame, 1)

    h, w = frame.shape[:2]

    rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

    result = hands.process(rgb)

    ahora = time.time()

    # Línea central
    cv2.line(frame, (w//2, 0), (w//2, h), (255,255,255), 2)

    # ── Procesar manos ──────────────────────────────────────────────────────
    if result.multi_hand_landmarks and result.multi_handedness:

        for hand_landmarks, handedness in zip(
            result.multi_hand_landmarks,
            result.multi_handedness
        ):

            label = handedness.classification[0].label

            munieca_x = hand_landmarks.landmark[0].x
            munieca_y = hand_landmarks.landmark[0].y

            jugador = None

            for j, cfg in JUGADORES.items():

                if validar_mano(label, munieca_x, cfg):
                    jugador = j
                    break

            if jugador is None:
                continue

            # Dibujar landmarks
            mp_drawing.draw_landmarks(
                frame,
                hand_landmarks,
                mp_hands.HAND_CONNECTIONS,
                mp_drawing.DrawingSpec(
                    color=COLORES[jugador],
                    thickness=2,
                    circle_radius=4
                ),
                mp_drawing.DrawingSpec(
                    color=(255,255,255),
                    thickness=2
                )
            )

            gesto = detectar_gesto(hand_landmarks.landmark)

            # Enviar estado
            if (ahora - tiempos[jugador]["movimiento"]) > COOLDOWN_POSICION:

                enviar_estado(
                    jugador,
                    munieca_x,
                    munieca_y,
                    gesto
                )

                tiempos[jugador]["movimiento"] = ahora

            # Cooldown gesto
            if (
                gesto != "NONE"
                and
                (ahora - tiempos[jugador]["gesto"]) > COOLDOWN_GESTO
            ):

                print(f"{jugador} → {gesto}")

                tiempos[jugador]["gesto"] = ahora

    # HUD
    cv2.putText(
        frame,
        "MEDIA PIPE CONTROL",
        (20, 40),
        cv2.FONT_HERSHEY_SIMPLEX,
        1,
        (0,255,255),
        2
    )

    cv2.imshow(WINDOW_NAME, frame)

    # Mantener siempre arriba
    win32gui.SetWindowPos(
        hwnd,
        win32con.HWND_TOPMOST,
        1400,
        20,
        480,
        270,
        win32con.SWP_SHOWWINDOW
    )

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# ── Cleanup ────────────────────────────────────────────────────────────────────
cap.release()

cv2.destroyAllWindows()

sock.close()