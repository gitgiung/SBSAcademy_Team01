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

    private void Start()
    {
       playerData = GetComponent<PlayerData>();
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        player = GetComponent<Player>();
    }

    private void FixedUpdate()
    {
        joystickDirection = variableJoystick == null
            ? Vector2.zero
            : new Vector2(variableJoystick.Horizontal, variableJoystick.Vertical);
        rb.linearVelocity = player != null && player.IsDashing
            ? Vector2.zero
            : joystickDirection * playerData.speed;
    }

    private void Update()
    {
        player?.SetExternalMovementAnimation(joystickDirection, player.IsDashing);
    }
}
