using System.Collections;
using UnityEngine;

/// <summary>Converts movement input into player displacement.</summary>
internal sealed class PlayerMovement
{
    private readonly Transform transform;
    private readonly PlayerData data;
    private readonly System.Func<float> speedMultiplier;
    private Vector2 dashDirection;
    private float dashDuration;
    private float dashSpeedMultiplier;
    private float nextDashTime;

    public bool IsDashing { get; private set; }

    public PlayerMovement(Transform transform, PlayerData data, System.Func<float> speedMultiplier)
    {
        this.transform = transform;
        this.data = data;
        this.speedMultiplier = speedMultiplier;
    }

    public void Tick(Vector2 inputDirection)
    {
        if (IsDashing) return;

        Vector2 direction = Vector2.ClampMagnitude(inputDirection, 1f);
        transform.position += (Vector3)(direction * data.speed * speedMultiplier() * Time.deltaTime);
    }

    public bool TryBeginDash(Vector2 direction, float duration, float speedMultiplier, float cooldown)
    {
        if (IsDashing || Time.time < nextDashTime || direction.sqrMagnitude < 0.0001f)
            return false;

        dashDirection = direction.normalized;
        dashDuration = Mathf.Max(0.01f, duration);
        dashSpeedMultiplier = Mathf.Max(1f, speedMultiplier);
        nextDashTime = Time.time + Mathf.Max(0f, cooldown);
        IsDashing = true;
        return true;
    }

    public IEnumerator DashRoutine()
    {
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            float deltaTime = Time.deltaTime;
            transform.position += (Vector3)(dashDirection * data.speed * dashSpeedMultiplier * deltaTime);
            elapsed += deltaTime;
            yield return null;
        }
        IsDashing = false;
    }

    public void Reset()
    {
        IsDashing = false;
    }
}
