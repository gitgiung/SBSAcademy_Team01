using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public float speed = 5f;
    public VariableJoystick variableJoystick;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (variableJoystick == null) return;

        Vector2 direction = new Vector2(variableJoystick.Horizontal, variableJoystick.Vertical);
        rb.linearVelocity = direction * speed;
    }
}