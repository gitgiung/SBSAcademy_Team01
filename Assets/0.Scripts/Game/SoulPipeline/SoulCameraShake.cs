using UnityEngine;

/// <summary>짧은 카메라 흔들림. SoulCameraRig LateUpdate 이후에 오프셋만 더한다.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public sealed class SoulCameraShake : MonoBehaviour
{
    [SerializeField] private float trauma;
    [SerializeField] private float falloff = 1.6f;
    [SerializeField] private float maxOffset = 0.35f;

    public static SoulCameraShake Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        if (trauma <= 0f) return;
        trauma = Mathf.Max(0f, trauma - Time.deltaTime * falloff);
        float shake = trauma * trauma;
        Vector3 offset = new Vector3(
            (Mathf.PerlinNoise(Time.time * 25f, 0.1f) * 2f - 1f) * maxOffset * shake,
            (Mathf.PerlinNoise(0.3f, Time.time * 25f) * 2f - 1f) * maxOffset * shake * 0.5f,
            (Mathf.PerlinNoise(0.7f, Time.time * 21f) * 2f - 1f) * maxOffset * shake);
        transform.position += offset;
    }

    public void AddTrauma(float amount) => trauma = Mathf.Clamp01(trauma + amount);

    public static void Punch(float amount = 0.35f)
    {
        if (Instance != null) Instance.AddTrauma(amount);
    }
}
