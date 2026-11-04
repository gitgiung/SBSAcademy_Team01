using UnityEngine;

[DisallowMultipleComponent]
public sealed class ExperiencePickup : MonoBehaviour, IExperiencePickup
{
    [Header("획득 설정")]
    [Min(0f)]
    [SerializeField] private float experienceAmount; // 획득시 경험치량
    [Min(0f)]
    [SerializeField] private float attractionSpeed; // 플레이어에게 끌려가는 속도. 0이면 끌려가지 않음

    private PlayerData playerData;
    private bool collected;

    private void Awake()
    {
        ResolvePlayerData(null);
    }

    public void Initialize(Transform target)
    {
        ResolvePlayerData(target);
    }

    private void Update()
    {
        if (playerData == null || playerData.IsDead || attractionSpeed <= 0f)
            return;

        float pickupDistance = Mathf.Max(0f, playerData.experiencePickupDistance);
        if (pickupDistance <= 0f)
            return;

        Vector2 offset = (Vector2)playerData.transform.position - (Vector2)transform.position;
        if (offset.sqrMagnitude > pickupDistance * pickupDistance)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            playerData.transform.position,
            attractionSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        PlayerData collector = other.GetComponentInParent<PlayerData>();
        if (collector == null || !collector.AddEXP(experienceAmount))
            return;

        collected = true;
        Destroy(gameObject);
    }

    private void ResolvePlayerData(Transform target)
    {
        if (target != null)
        {
            playerData = target.GetComponent<PlayerData>();
            if (playerData == null)
                playerData = target.GetComponentInParent<PlayerData>();
            if (playerData == null)
                playerData = target.GetComponentInChildren<PlayerData>();
        }

        if (playerData == null)
            playerData = UnityEngine.Object.FindAnyObjectByType<PlayerData>();
    }
}
