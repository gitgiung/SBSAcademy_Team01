using UnityEngine;

/// <summary>평탄 맵 가장자리에서 캐릭이 너무 멀리 나가지 않게 클램프.</summary>
[DisallowMultipleComponent]
public sealed class SoulArenaBounds : MonoBehaviour
{
    [SerializeField] private float halfExtent = 18f;

    private void LateUpdate()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, -halfExtent, halfExtent);
        p.z = Mathf.Clamp(p.z, -halfExtent, halfExtent);
        if (p.y < 0f) p.y = 0f;
        transform.position = p;
    }
}
