using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public sealed class BossHealthGaugePreview : MonoBehaviour
{
    [SerializeField, Min(0.01f), Tooltip("초당 줄어드는 흰색 잔량의 게이지 비율입니다.")]
    private float bossHpSpeed = 0.2f;
    [SerializeField] private Material bossHpDamageMaterial;

    private ResourceGaugePreview.Gauge gauge;

    private void Awake()
    {
        gauge = ResourceGaugePreview.Gauge.Create(GetComponent<EnemyHealth>().HealthSlider, bossHpDamageMaterial);
    }

    private void LateUpdate()
    {
        gauge?.UpdateDamage(bossHpSpeed, Time.unscaledDeltaTime);
    }

    private void OnDisable()
    {
        gauge?.Reset();
    }

    private void OnDestroy()
    {
        gauge?.Dispose();
    }
}
