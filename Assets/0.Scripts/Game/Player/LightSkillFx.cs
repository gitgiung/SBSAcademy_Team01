using System.Collections.Generic;
using UnityEngine;

/// <summary>약공격 한 발의 그림과 타격입니다. 이동과 피해만 담당합니다.</summary>
public sealed class LightSkillFx : MonoBehaviour, IPoolable
{
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D hitCollider;
    private PooledObject pooled;
    private LightSkillCast skill;
    private Vector2 direction;
    private float damage;
    private float life;
    private float traveled;
    private int hitsLeft;
    private float hitTimer;
    private float frameTimer;
    private float frameDuration;
    private int frame;
    private bool running;
    private bool projectile;
    private bool bursting;
    private string enemyTag = "Enemy";
    private Sprite[] frames;
    private readonly HashSet<EntityId> hitIds = new HashSet<EntityId>();

    public void OnPoolRent() => Cache();
    public void OnPoolReturn()
    {
        running = false;
        bursting = false;
        StopAllCoroutines();
        hitIds.Clear();
    }

    public void Play(LightSkillCast skill, Vector2 direction, float damage, string enemyTag, float projectileSize = 1f)
    {
        Cache();
        this.skill = skill;
        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        this.damage = damage;
        projectile = skill.Shape == LightSkillShape.Projectile;
        this.enemyTag = string.IsNullOrEmpty(enemyTag) ? "Enemy" : enemyTag;
        hitsLeft = Mathf.Max(1, skill.Hits);
        hitTimer = 0f;
        traveled = 0f;
        life = projectile ? skill.Range / 12f + 0.2f : 0.45f + skill.RepeatSpacing * (hitsLeft - 1);
        frame = 0;
        frameTimer = 0f;
        hitIds.Clear();
        running = true;
        bursting = false;

        frames = LightSkillVisuals.Frames(skill.Shape, out frameDuration);
        if (spriteRenderer != null)
        {
            spriteRenderer.sharedMaterial = LightSkillVisuals.MaterialFor(skill.Element, skill.Shape);
            spriteRenderer.sprite = frames.Length > 0 ? frames[0] : null;
            spriteRenderer.sortingOrder = 30;
        }
        // 화염 고리 그림은 바닥에 깔린 가로 타원이다. 조준과 상관없이 플레이어 주위의 가로 범위로 둔다.
        if (skill.Shape == LightSkillShape.Ring)
            transform.rotation = Quaternion.identity;
        else
            transform.right = this.direction;
        ApplyScale(projectileSize);
        if (hitCollider != null)
        {
            hitCollider.enabled = projectile;
            hitCollider.radius = 0.28f;
        }
        gameObject.name = skill.Name;
    }

    private void Cache()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (hitCollider == null) hitCollider = GetComponent<CircleCollider2D>();
        if (pooled == null) pooled = GetComponent<PooledObject>();
    }

    private void ApplyScale(float projectileSize)
    {
        if (frames == null || frames.Length == 0) return;
        float width = frames[0].rect.width / LightSkillVisuals.PixelsPerUnit;
        float height = frames[0].rect.height / LightSkillVisuals.PixelsPerUnit;
        if (width <= 0f || height <= 0f) return;
        switch (skill.Shape)
        {
            case LightSkillShape.Circle:
                float diameter = skill.Range * 2f;
                transform.localScale = Vector3.one * (diameter / height);
                break;
            case LightSkillShape.Ring:
                transform.localScale = Vector3.one * ((skill.Range * 2f) / height);
                break;
            case LightSkillShape.Line:
                transform.localScale = new Vector3(skill.Range / width, Mathf.Max(0.4f, skill.Width) / height, 1f);
                break;
            case LightSkillShape.Cone:
            case LightSkillShape.Arc:
                transform.localScale = Vector3.one * (skill.Range / height);
                break;
            default:
                float projectileScale = Mathf.Clamp(skill.Width / 0.5f, 0.7f, 1.8f);
                transform.localScale = Vector3.one * (projectileScale * projectileSize);
                break;
        }
    }

    private void Update()
    {
        if (!running || Time.deltaTime <= 0f) return;
        life -= Time.deltaTime;
        Animate();
        if (bursting)
        {
            if (life <= 0f) Finish();
            return;
        }
        if (projectile)
        {
            float step = 12f * Time.deltaTime;
            transform.position += (Vector3)(direction * step);
            traveled += step;
            if (traveled >= skill.Range || life <= 0f) Finish();
            return;
        }

        if (hitsLeft > 0)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
            {
                hitTimer = skill.RepeatSpacing;
                hitsLeft--;
                hitIds.Clear();
                // 풀 대여 초기화가 끝난 뒤에 적 반납과 경험치 보석 대여가 가능합니다.
                Strike();
            }
        }
        if (life <= 0f) Finish();
    }

    private void Animate()
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null) return;
        frameTimer += Time.deltaTime;
        if (frameTimer < frameDuration) return;
        frameTimer = 0f;
        bool loop = !bursting && (skill.Shape == LightSkillShape.Projectile || skill.Shape == LightSkillShape.Ring);
        if (!loop && frame >= frames.Length - 1) return;
        frame = (frame + 1) % frames.Length;
        spriteRenderer.sprite = frames[frame];
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!running || bursting || !projectile || other == null || !other.CompareTag("Enemy")) return;
        Damage(other);
        if (!skill.ExplodesOnHit)
        {
            Finish();
            return;
        }
        Splash();
        BeginImpactBurst();
    }

    private void Splash()
    {
        float radius = skill.SplashRadius;
        if (radius <= 0f) return;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, radius))
            if (hit != null && hit.CompareTag(enemyTag)) Damage(hit);
    }

    private void BeginImpactBurst()
    {
        bursting = true;
        projectile = false;
        if (hitCollider != null) hitCollider.enabled = false;
        frames = LightSkillVisuals.Frames(LightSkillShape.Circle, out frameDuration);
        if (spriteRenderer != null)
        {
            spriteRenderer.sharedMaterial = LightSkillVisuals.MaterialFor(skill.Element, LightSkillShape.Circle);
            spriteRenderer.sprite = frames.Length > 0 ? frames[0] : null;
            spriteRenderer.sortingOrder = 31;
        }
        transform.rotation = Quaternion.identity;
        float height = frames != null && frames.Length > 0
            ? frames[0].rect.height / LightSkillVisuals.PixelsPerUnit : 0f;
        if (height > 0f)
            transform.localScale = Vector3.one * ((skill.SplashRadius * 2f) / height);
        frame = 0;
        frameTimer = 0f;
        life = Mathf.Max(0.2f, frameDuration * Mathf.Max(1, frames == null ? 1 : frames.Length));
    }

    private void Strike()
    {
        Vector2 origin = transform.position;
        switch (skill.Shape)
        {
            case LightSkillShape.Circle:
                foreach (Collider2D hit in Physics2D.OverlapCircleAll(origin, skill.Range))
                    if (hit != null && hit.CompareTag(enemyTag)) Damage(hit);
                break;
            case LightSkillShape.Ring:
                float inner = Mathf.Max(0f, skill.Range - skill.Width);
                foreach (Collider2D hit in Physics2D.OverlapCircleAll(origin, skill.Range))
                {
                    if (hit == null || !hit.CompareTag(enemyTag)) continue;
                    float distance = Vector2.Distance(origin, hit.transform.position);
                    if (distance >= inner) Damage(hit);
                }
                break;
            case LightSkillShape.Line:
                Vector2 center = origin + direction * (skill.Range * 0.5f);
                Vector2 size = new Vector2(skill.Range, Mathf.Max(0.3f, skill.Width));
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, angle))
                    if (hit != null && hit.CompareTag(enemyTag)) Damage(hit);
                break;
            case LightSkillShape.Cone:
            case LightSkillShape.Arc:
                foreach (Collider2D hit in Physics2D.OverlapCircleAll(origin, skill.Range))
                {
                    if (hit == null || !hit.CompareTag(enemyTag)) continue;
                    Vector2 toTarget = (Vector2)hit.transform.position - origin;
                    if (toTarget.sqrMagnitude < 0.0001f || Vector2.Angle(direction, toTarget) <= skill.Angle * 0.5f)
                        Damage(hit);
                }
                break;
        }
    }

    private void Damage(Collider2D hit)
    {
        EntityId id = hit.GetEntityId();
        if (!hitIds.Add(id)) return;
        if (hit.TryGetComponent<IDamageable>(out IDamageable damageable))
            damageable.TakeDamage(damage);
    }

    private void Finish()
    {
        if (!running) return;
        running = false;
        if (pooled != null && pooled.IsPoolOwned) pooled.ReturnToPool();
        else Destroy(gameObject);
    }
}
