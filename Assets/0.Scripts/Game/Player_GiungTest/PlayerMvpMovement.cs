using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMvpMovement : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField, Min(0f)] private float moveSpeed = 4f;

    private Rigidbody2D body;
    private InputAction move;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnEnable()
    {
        var source = inputActions != null ? inputActions.FindAction("Player/Move") : null;
        if (source == null)
        {
            Debug.LogWarning("PlayerMvpMovement requires a Player/Move input action.", this);
            return;
        }

        // Own the clone so disabling this prototype never disables another player's action.
        move = source.Clone();
        move.Enable();
    }

    private void FixedUpdate()
    {
        if (move == null) return;
        Vector2 direction = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        body.MovePosition(body.position + direction * (Mathf.Max(0f, moveSpeed) * Time.fixedDeltaTime));
    }

    private void OnDisable()
    {
        move?.Dispose();
        move = null;
        if (body != null) body.linearVelocity = Vector2.zero;
    }
}
