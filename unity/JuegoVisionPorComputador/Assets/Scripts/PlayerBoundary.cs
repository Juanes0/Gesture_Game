using UnityEngine;

public class PlayerBoundary : MonoBehaviour
{
    private CameraController camCtrl;
    private Transform otherPlayer;
    private Rigidbody2D rb;

    [HideInInspector] public bool isBlocked = false;

    public void Init(CameraController controller, Transform other)
    {
        camCtrl = controller;
        otherPlayer = other;
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (camCtrl == null || otherPlayer == null) return;
        if (!camCtrl.IsAtLimit)
        {
            isBlocked = false;
            return;
        }

        // Determinar si este jugador está a la izquierda o derecha del otro
        bool iAmToTheLeft = transform.position.x < otherPlayer.position.x;

        if (iAmToTheLeft)
        {
            // Bloquear si quiere moverse más a la izquierda
            if (rb.linearVelocity.x < 0f)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                isBlocked = true;
            }
        }
        else
        {
            // Bloquear si quiere moverse más a la derecha
            if (rb.linearVelocity.x > 0f)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                isBlocked = true;
            }
        }
    }
}