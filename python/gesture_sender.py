import cv2
import mediapipe as mp
import socket
import time

# ── Configuración UDP ──────────────────────────────────────────────────────────
UDP_IP   = "127.0.0.1"
UDP_PORT = 5052
sock     = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# ── MediaPipe ──────────────────────────────────────────────────────────────────
mp_hands   = mp.solutions.hands
mp_drawing = mp.solutions.drawing_utils
hands      = mp_hands.Hands(
    static_image_mode        = False,
    max_num_hands            = 2,
    min_detection_confidence = 0.7,
    min_tracking_confidence  = 0.6
)

# ── Zonas de movimiento ────────────────────────────────────────────────────────

JUGADORES = {
    "P1": {
        "hand_label" : "Right",   # MediaPipe con espejo: mano derecha del cuerpo
        "zona_min"   : 0.5,        # límite izquierdo de su mitad
        "zona_max"   : 1.0,        # límite derecho de su mitad
        # zonas internas de movimiento
        "right_max"  : 0.83,       # X < 0.67  → RIGHT
        "left_min"   : 0.67,       # X > 0.83  → LEFT
                                   # entre ambos → STOP
    },
    "P2": {
        "hand_label" : "Left",
        "zona_min"   : 0.0,
        "zona_max"   : 0.5,
        "right_max"  : 0.33,       # X < 0.17  → RIGHT
        "left_min"   : 0.17,       # X > 0.33  → LEFT
    },
}

# Colores HUD
COLORES = {"P1": (0, 200, 255), "P2": (255, 130, 0)}

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
    dedos    = dedos_extendidos(landmarks)
    abiertos = sum(dedos)
    if abiertos >= 4:
        return "JUMP"
    elif abiertos == 0:
        return "ATTACK"
    elif dedos[1] and not dedos[2] and not dedos[3] and not dedos[4]:
        return "SHOOT"
    return None

def detectar_movimiento(munieca_x, config):
    if munieca_x > config["right_max"]:
        return "RIGHT"
    elif munieca_x < config["left_min"]:
        return "LEFT"
    return "STOP"

def validar_mano(label_detectado, munieca_x, config):
    """Doble validación: etiqueta MediaPipe + zona de pantalla."""
    label_ok = (label_detectado == config["hand_label"])
    zona_ok  = (config["zona_min"] <= munieca_x <= config["zona_max"])
    return label_ok and zona_ok

def enviar(jugador, mensaje):
    sock.sendto(f"{jugador}:{mensaje}".encode('utf-8'), (UDP_IP, UDP_PORT))

# ── Bucle principal ────────────────────────────────────────────────────────────
cap = cv2.VideoCapture(0)

tiempos = {
    "P1": {"gesto": 0, "movimiento": 0},
    "P2": {"gesto": 0, "movimiento": 0},
}
COOLDOWN_GESTO      = 0.4
COOLDOWN_MOVIMIENTO = 0.05

ultimos = {
    "P1": {"gesto": "---", "movimiento": "STOP"},
    "P2": {"gesto": "---", "movimiento": "STOP"},
}

print("=== Gesture Sender — 2 Jugadores ===")
print(f"UDP → {UDP_IP}:{UDP_PORT}")
print()
print("Posición frente a la cámara:")
print("  J1: párate a la IZQUIERDA, usa tu mano DERECHA")
print("  J2: párate a la DERECHA,   usa tu mano IZQUIERDA")
print()
print("Movimiento (dentro de tu mitad de pantalla):")
print("  Mano en zona izquierda  → LEFT")
print("  Mano en zona central    → STOP")
print("  Mano en zona derecha    → RIGHT")
print()
print("Gestos:")
print("  Mano abierta ✋  → JUMP")
print("  Puño ✊          → ATTACK")
print("  Solo índice ☝️   → SHOOT")
print()
print("Presiona Q para salir\n")

while cap.isOpened():
    ret, frame = cap.read()
    if not ret:
        break

    frame  = cv2.flip(frame, 1)
    h, w   = frame.shape[:2]
    rgb    = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    result = hands.process(rgb)

    ahora = time.time()

    # ── Dibujar divisiones de zona ─────────────────────────────────────────
    # Línea central entre jugadores
    cv2.line(frame, (w//2, 0), (w//2, h), (255, 255, 255), 2)

    # Líneas de zona interna J1 (mitad izquierda: X 0.5→1.0)
    j1_right = int(0.67 * w)
    j1_left  = int(0.83 * w)
    cv2.line(frame, (j1_right, 0), (j1_right, h), COLORES["P1"], 1)
    cv2.line(frame, (j1_left,  0), (j1_left,  h), COLORES["P1"], 1)

    # Líneas de zona interna J2 (mitad derecha: X 0.0→0.5)
    j2_right = int(0.17 * w)
    j2_left  = int(0.33 * w)
    cv2.line(frame, (j2_right, 0), (j2_right, h), COLORES["P2"], 1)
    cv2.line(frame, (j2_left,  0), (j2_left,  h), COLORES["P2"], 1)

    # Etiquetas de zona J1
    cv2.putText(frame, "L", (int(0.52*w), h//2), cv2.FONT_HERSHEY_SIMPLEX, 0.7, COLORES["P1"], 2)
    cv2.putText(frame, "N", (int(0.73*w), h//2), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (180,180,180), 2)
    cv2.putText(frame, "R", (int(0.88*w), h//2), cv2.FONT_HERSHEY_SIMPLEX, 0.7, COLORES["P1"], 2)

    # Etiquetas de zona J2
    cv2.putText(frame, "L", (int(0.02*w), h//2), cv2.FONT_HERSHEY_SIMPLEX, 0.7, COLORES["P2"], 2)
    cv2.putText(frame, "N", (int(0.20*w), h//2), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (180,180,180), 2)
    cv2.putText(frame, "R", (int(0.36*w), h//2), cv2.FONT_HERSHEY_SIMPLEX, 0.7, COLORES["P2"], 2)

    # Títulos de jugador
    cv2.putText(frame, "J1", (int(0.72*w), 30), cv2.FONT_HERSHEY_SIMPLEX, 1.0, COLORES["P1"], 2)
    cv2.putText(frame, "J2", (int(0.22*w), 30), cv2.FONT_HERSHEY_SIMPLEX, 1.0, COLORES["P2"], 2)

    # ── Procesar manos detectadas ──────────────────────────────────────────
    if result.multi_hand_landmarks and result.multi_handedness:
        for hand_landmarks, handedness in zip(result.multi_hand_landmarks, result.multi_handedness):

            label     = handedness.classification[0].label
            munieca_x = hand_landmarks.landmark[0].x

            # Identificar jugador con doble validación
            jugador = None
            for j, cfg in JUGADORES.items():
                if validar_mano(label, munieca_x, cfg):
                    jugador = j
                    break

            # Mano no reconocida → dibujar en gris y saltar
            if jugador is None:
                mp_drawing.draw_landmarks(
                    frame, hand_landmarks, mp_hands.HAND_CONNECTIONS,
                    mp_drawing.DrawingSpec(color=(80,80,80), thickness=1, circle_radius=3),
                    mp_drawing.DrawingSpec(color=(80,80,80), thickness=1)
                )
                continue

            # Dibujar con color del jugador
            mp_drawing.draw_landmarks(
                frame, hand_landmarks, mp_hands.HAND_CONNECTIONS,
                mp_drawing.DrawingSpec(color=COLORES[jugador], thickness=2, circle_radius=4),
                mp_drawing.DrawingSpec(color=(255,255,255), thickness=2)
            )

            cfg = JUGADORES[jugador]

            # ── Movimiento continuo ──────────────────────────────────────
            movimiento = detectar_movimiento(munieca_x, cfg)
            if (ahora - tiempos[jugador]["movimiento"]) > COOLDOWN_MOVIMIENTO:
                enviar(jugador, movimiento)
                ultimos[jugador]["movimiento"]     = movimiento
                tiempos[jugador]["movimiento"]     = ahora

            # ── Gesto puntual ────────────────────────────────────────────
            gesto = detectar_gesto(hand_landmarks.landmark)
            if gesto and (ahora - tiempos[jugador]["gesto"]) > COOLDOWN_GESTO:
                enviar(jugador, gesto)
                ultimos[jugador]["gesto"]     = gesto
                tiempos[jugador]["gesto"]     = ahora
                print(f"{jugador} → {gesto}")

    # ── HUD estado por jugador ─────────────────────────────────────────────
    for jugador, color in COLORES.items():
        bx = int(0.52*w) if jugador == "P1" else 5
        mov  = ultimos[jugador]["movimiento"]
        gest = ultimos[jugador]["gesto"]
        cv2.putText(frame, f"Mov:{mov}",  (bx, h-40), cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2)
        cv2.putText(frame, f"Gest:{gest}",(bx, h-15), cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2)

    cv2.putText(frame, "Q salir", (w//2-35, h-5), cv2.FONT_HERSHEY_SIMPLEX, 0.45, (200,200,200), 1)
    cv2.imshow("Gesture Sender — 2 Jugadores", frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()
sock.close()