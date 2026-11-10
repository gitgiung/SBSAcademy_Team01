using UnityEngine;

/// <summary>히트박스 armed일 때 손 스케일 펄스. 바닥 경고 아님.</summary>
[DisallowMultipleComponent]
public sealed class SoulWeaponArmedPulse : MonoBehaviour
{
    private SoulWeaponHitbox3D hitbox;
    private Vector3 baseScale;
    private bool bound;

    private void Awake()
    {
        hitbox = GetComponent<SoulWeaponHitbox3D>();
        baseScale = transform.localScale;
        bound = true;
    }

    private void LateUpdate()
    {
        if (!bound || hitbox == null) return;
        if (hitbox.IsArmed)
        {
            float s = 1f + 0.25f * Mathf.Abs(Mathf.Sin(Time.time * 28f));
            transform.localScale = baseScale * s;
        }
        else
            transform.localScale = baseScale;
    }
}
