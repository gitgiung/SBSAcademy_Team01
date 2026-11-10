using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public VariableJoystick variableJoystick;
    public PlayerData playerData;

    private Rigidbody2D rb;
    private Player player;
    private Vector2 joystickDirection;
    public Vector2 MovementDirection => joystickDirection;
    public Vector2 AimDirection => variableJoystick == null ? Vector2.zero
        : new Vector2(variableJoystick.Horizontal, variableJoystick.Vertical);

    private void Start()
    {
       playerData = GetComponent<PlayerData>();
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        player = GetComponent<Player>();
    }

    private void FixedUpdate()
    {
        if (player != null && (!player.CanMove || player.IsDashing))
        {
            StopMovement();
            return;
        }
        joystickDirection = variableJoystick == null
            ? Vector2.zero
            : new Vector2(variableJoystick.Horizontal, variableJoystick.Vertical);
        rb.linearVelocity = joystickDirection * playerData.MoveSpeed;
    }

    public void StopMovement()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        joystickDirection = Vector2.zero;
    }

    private void Update()
    {
        player?.SetExternalMovementAnimation(joystickDirection, player.IsDashing);
    }
}
