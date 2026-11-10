using UnityEngine;

/// <summary>폴백 플레이어 공격 시 팔 휘두르기 시각. 본 애니 없을 때만.</summary>
[DisallowMultipleComponent]
public sealed class SoulFallbackPlayerArm : MonoBehaviour
{
    private Transform shoulder;
    private float swing;

    public void Bind(Transform shoulderBone) => shoulder = shoulderBone;

    public void Pulse() => swing = 1f;

    private void Update()
    {
        if (shoulder == null) return;
        if (swing <= 0f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J)))
            swing = 1f;

        if (swing > 0f)
        {
            swing -= Time.deltaTime * 4f;
            float t = 1f - Mathf.Clamp01(swing);
            float angle = Mathf.Lerp(-50f, 70f, t);
            shoulder.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
        else
            shoulder.localRotation = Quaternion.Euler(-20f, 0f, 0f);
    }
}
