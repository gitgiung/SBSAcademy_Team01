using System.Collections;
using UnityEngine;

/// <summary>Converts movement input into player displacement.</summary>
internal sealed class PlayerMovement
{
    private readonly Transform transform;
    private readonly PlayerData data;
    private readonly System.Func<float> speedMultiplier;
    private readonly CollisionMovement2D collisionMovement;
    private Vector2 dashDirection;
    private float dashDuration;
    private float dashSpeedMultiplier;
    private float nextDashTime;
    private float lastDashCooldown;

    public bool IsDashing { get; private set; }
    public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);
    public float DashCooldownProgress => lastDashCooldown > 0f
        ? Mathf.Clamp01(1f - DashCooldownRemaining / lastDashCooldown) : 1f;

    public PlayerMovement(Transform transform, PlayerData data, System.Func<float> speedMultiplier)
    {
        this.transform = transform;
        this.data = data;
        this.speedMultiplier = speedMultiplier;
        Rigidbody2D body = transform.GetComponent<Rigidbody2D>();
        if (body != null) collisionMovement = new CollisionMovement2D(body);
    }

    public void Tick(Vector2 inputDirection)
    {
        if (IsDashing) return;

        Vector2 direction = Vector2.ClampMagnitude(inputDirection, 1f);
        Move(direction * data.MoveSpeed * speedMultiplier() * Time.deltaTime);
    }

    public bool CanBeginDash(Vector2 direction) =>
        !IsDashing && Time.time >= nextDashTime && direction.sqrMagnitude >= 0.0001f;

    public bool TryBeginDash(Vector2 direction, float duration, float speedMultiplier, float cooldown)
    {
        if (!CanBeginDash(direction)) return false;

        dashDirection = direction.normalized;
        dashDuration = Mathf.Max(0.01f, duration);
        dashSpeedMultiplier = Mathf.Max(1f, speedMultiplier);
        lastDashCooldown = Mathf.Max(0f, cooldown);
        nextDashTime = Time.time + lastDashCooldown;
        IsDashing = true;
        return true;
    }

    public IEnumerator DashRoutine()
    {
        float elapsed = 0f;
        while (IsDashing && elapsed < dashDuration)
        {
            float deltaTime = Time.deltaTime;
            Move(dashDirection * data.MoveSpeed * dashSpeedMultiplier * deltaTime);
            elapsed += deltaTime;
            yield return null;
        }
        IsDashing = false;
    }

    public void Reset()
    {
        IsDashing = false;
    }

    public void ApplyDisplacement(Vector2 displacement) => Move(displacement);

    private void Move(Vector2 displacement)
    {
        if (collisionMovement != null) collisionMovement.Move(displacement);
        else transform.position += (Vector3)displacement;
    }
}
