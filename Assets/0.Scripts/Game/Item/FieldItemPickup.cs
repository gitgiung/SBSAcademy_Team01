using System;
using UnityEngine;

/// <summary>처치 보상으로 떨어진 필드 아이템입니다. 플레이어가 가까이 오면 효과를 한 번만 알립니다.</summary>
[DisallowMultipleComponent]
public sealed class FieldItemPickup : MonoBehaviour
{
    private const string EntitySortingLayer = "Entity";
    private const float PopDuration = 0.18f;
    private const float PopScale = 1.7f;
    private const float BobHeight = 0.08f;
    private const float BobSpeed = 3.2f;
    private const float CollectorRetryInterval = 0.5f;

    public FieldItemKind Kind { get; private set; }
    public bool Collected { get; private set; }

    private PlayerData collector;
    private float collectDistance;
    private Action<FieldItemPickup, PlayerData> collectedReport;
    private SpriteRenderer body;
    private Vector3 restPosition;
    private Vector3 restScale = Vector3.one;
    private float visualTime;
    private float popTime;
    private float collectorRetryTimer;

    /// <summary>프리팹 없이 아이템을 만듭니다. sprite가 없으면 만들지 않습니다.</summary>
    public static FieldItemPickup Create(FieldItemKind kind, Vector3 position, Sprite sprite, float scale,
        float collectDistance, Transform target, Action<FieldItemPickup, PlayerData> collected)
    {
        if (kind == FieldItemKind.None || sprite == null) return null;

        var item = new GameObject("FieldItem " + kind).AddComponent<FieldItemPickup>();
        item.Kind = kind;
        item.collectDistance = collectDistance;
        item.collectedReport = collected;
        item.restPosition = position;
        float size = float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f ? 1f : scale;
        item.restScale = new Vector3(size, size, 1f);
        item.transform.position = position;
        item.transform.localScale = item.restScale;

        item.body = item.gameObject.AddComponent<SpriteRenderer>();
        item.body.sprite = sprite;
        // 경험치(DNA)와 같은 정렬 레이어에 둡니다. 레이어가 없으면 Default에 남습니다.
        item.body.sortingLayerName = EntitySortingLayer;
        item.body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        item.body.receiveShadows = false;

        item.ResolveCollector(target);
        return item;
    }

    /// <summary>두 지점이 획득 거리 안에 있는지 판정합니다. 거리가 0 이하이거나 잘못되면 획득하지 않습니다.</summary>
    public static bool IsWithin(Vector2 a, Vector2 b, float distance)
    {
        if (float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0f) return false;
        return (a - b).sqrMagnitude <= distance * distance;
    }

    private void Update()
    {
        if (Collected)
        {
            AnimatePop(Time.unscaledDeltaTime);
            return;
        }

        float deltaTime = Time.deltaTime;
        // 레벨업·일시정지로 전투가 멈춘 동안에는 흔들림과 획득을 모두 멈춥니다.
        if (Time.timeScale <= 0f || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;

        visualTime += deltaTime;
        transform.position = new Vector3(restPosition.x,
            restPosition.y + Mathf.Sin(visualTime * BobSpeed) * BobHeight, restPosition.z);

        if (collector == null)
        {
            collectorRetryTimer -= deltaTime;
            if (collectorRetryTimer > 0f) return;
            collectorRetryTimer = CollectorRetryInterval;
            ResolveCollector(null);
            if (collector == null) return;
        }

        if (IsWithin(collector.transform.position, restPosition, collectDistance)) TryCollect(collector);
    }

    /// <summary>효과를 한 번만 알리고 사라지는 연출을 시작합니다.</summary>
    public bool TryCollect(PlayerData player)
    {
        if (Collected || !isActiveAndEnabled || player == null || player.IsDead) return false;
        Collected = true;
        popTime = 0f;
        Action<FieldItemPickup, PlayerData> report = collectedReport;
        collectedReport = null;
        try { report?.Invoke(this, player); }
        catch (Exception exception) { Debug.LogException(exception, this); }
        return true;
    }

    /// <summary>효과 없이 제거합니다. 씬 정리 때 사용합니다.</summary>
    public void Discard()
    {
        Collected = true;
        collectedReport = null;
        Destroy(gameObject);
    }

    private void AnimatePop(float deltaTime)
    {
        if (deltaTime > 0f && !float.IsNaN(deltaTime) && !float.IsInfinity(deltaTime)) popTime += deltaTime;
        float progress = Mathf.Clamp01(popTime / PopDuration);
        float scale = Mathf.Lerp(1f, PopScale, progress);
        transform.localScale = new Vector3(restScale.x * scale, restScale.y * scale, restScale.z);
        if (body != null)
        {
            Color color = body.color;
            color.a = 1f - progress;
            body.color = color;
        }
        if (progress >= 1f) Destroy(gameObject);
    }

    private void ResolveCollector(Transform target)
    {
        if (target != null)
        {
            collector = target.GetComponent<PlayerData>();
            if (collector == null) collector = target.GetComponentInParent<PlayerData>();
            if (collector == null) collector = target.GetComponentInChildren<PlayerData>();
        }
        if (collector == null) collector = UnityEngine.Object.FindAnyObjectByType<PlayerData>();
    }
}
