using System.Collections.Generic;
using UnityEngine;

/// <summary>약공격 한 발의 그림과 타격입니다. 이동과 피해만 담당합니다.</summary>
public sealed class LightSkillFx : MonoBehaviour, IPoolable
{
    private const int HeatWaveSkillId = 20008;
    private const float HeatWaveKnockbackMultiplier = 3f;

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
    private bool forking;
    private bool sunderScaled;
    private bool whirlScaled;
    private bool missileArt;
    // 스킬마다 구워 둔 그림입니다. 없으면 null이고 예전 시트로 그립니다.
    private LightSkillVisuals.BakedClip baked;
    // 구운 그림은 프레임 수가 아니라 흐른 시간으로 재생합니다. 한 번 재생하는 그림은 끝나면 숨깁니다.
    private bool clipTimed;
    private bool clipLoop;
    private float clipTime;
    private bool homing;
    private Vector2 castOrigin;
    private Vector2 fallbackAim;
    private Collider2D target;
    private float age;
    private float seekTimer;
    private string enemyTag = "Enemy";
    private Sprite[] frames;
    private readonly HashSet<EntityId> hitIds = new HashSet<EntityId>();
    private readonly List<SpriteRenderer> forks = new List<SpriteRenderer>();
    private Sprite[] forkFrames;
    private int forkFrame;
    private float forkTimer;
    // 구운 불줄기 그림의 가장 굵은 부분이 뿌리 쪽 반폭 약 14픽셀입니다. 맞히는 두께는 그 절반 정도의 임시값입니다.
    private const float ForkHitThickness = 0.7f;
    private const float ProjectileSpeed = 12f;
    private const float ProjectileHitRadius = 0.28f;
    // 유도 미사일의 움직임입니다. 표에 없는 연출용 임시값입니다.
    // 가시에서 곧게 튀어나오는 시간, 느리게 떠나 제 속도가 될 때까지의 시간과 처음 속도 비율입니다.
    private const float HomingStraightTime = 0.06f;
    private const float HomingBoostTime = 0.22f;
    private const float HomingLaunchSpeedRatio = 0.35f;
    // 초당 꺾을 수 있는 각도입니다. 날수록 세게 꺾어 표적 둘레를 맴돌지 않게 합니다.
    private const float HomingTurnStart = 300f;
    private const float HomingTurnMax = 1080f;
    private const float HomingTurnRampEnd = 0.45f;
    // 쫓을 적이 없을 때 다시 찾는 간격, 휘어 가는 만큼 더 주는 수명입니다.
    private const float HomingSeekInterval = 0.1f;
    private const float HomingExtraLife = 0.8f;

    internal System.Action<Vector3> ProjectileHitFlash { get; set; }

    public void OnPoolRent() => Cache();
    public void OnPoolReturn()
    {
        running = false;
        bursting = false;
        forking = false;
        homing = false;
        target = null;
        ProjectileHitFlash = null;
        HideForks();
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
        float sizeMul = Mathf.Max(0.01f, projectileSize);
        hitsLeft = Mathf.Max(1, skill.Hits);
        hitTimer = 0f;
        traveled = 0f;
        life = projectile ? skill.Range / ProjectileSpeed + 0.2f : 0.45f + skill.RepeatSpacing * (hitsLeft - 1);
        frame = 0;
        frameTimer = 0f;
        hitIds.Clear();
        running = true;
        bursting = false;
        forking = false;
        homing = false;
        target = null;
        HideForks();
        if (spriteRenderer != null) spriteRenderer.enabled = true;

        // 스킬마다 구워 둔 그림이 있으면 그 그림을 씁니다. 탄환은 반복, 범위는 한 번 재생하는 그림이어야 합니다.
        baked = LightSkillVisuals.Baked(LightSkillVisuals.BakedClipName(skill.Id));
        if (baked != null && baked.Loop != projectile) baked = null;
        clipTimed = baked != null;
        clipLoop = projectile;
        clipTime = 0f;
        Material material;
        if (baked != null)
        {
            frames = baked.Frames;
            frameDuration = baked.FrameDuration;
            material = LightSkillVisuals.BlazeMaterial();
            if (projectile)
            {
                // 여러 발이 똑같이 깜빡이지 않도록 반복 그림의 시작 지점을 흩어 둡니다.
                clipTime = Random.Range(0f, baked.Duration);
                frame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(clipTime / frameDuration));
            }
            else
                life = Mathf.Max(baked.Duration, skill.RepeatSpacing * (hitsLeft - 1) + 0.1f);
        }
        else
        {
            frames = LightSkillVisuals.Frames(skill.Shape, out frameDuration);
            material = LightSkillVisuals.MaterialFor(skill.Element, skill.Shape);
        }
        // 작열 구체는 pixelfx plasma-shot을 불 색으로 구운 그림을 씁니다. 없으면 기존 불꽃 탄환 그림입니다.
        if (skill.ForksOnHit && projectile)
        {
            Sprite[] orb = LightSkillVisuals.BlazeFrames(LightSkillVisuals.BlazeOrbPrefix);
            if (orb.Length > 0)
            {
                frames = orb;
                frameDuration = LightSkillVisuals.BlazeFrameDuration;
                material = LightSkillVisuals.BlazeMaterial();
            }
        }
        // 태양 탄막은 구워 둔 흰 불꽃 미사일 그림을 씁니다. 없으면 기존 불꽃 탄환 그림입니다.
        missileArt = false;
        if (skill.SunBarrage && projectile)
        {
            Sprite[] missile = LightSkillVisuals.BlazeFrames(LightSkillVisuals.SunMissilePrefix);
            if (missile.Length > 0)
            {
                frames = missile;
                frameDuration = LightSkillVisuals.BlazeFrameDuration;
                material = LightSkillVisuals.BlazeMaterial();
                missileArt = true;
            }
        }
        // 절멸의 불은 구워 둔 불 균열 그림을 씁니다. 없으면 기존 직선 그림입니다.
        sunderScaled = false;
        if (skill.SunderingFire && skill.Shape == LightSkillShape.Line)
        {
            Sprite[] sunder = LightSkillVisuals.BlazeFrames(LightSkillVisuals.SunderFirePrefix);
            if (sunder.Length > 0)
            {
                frames = sunder;
                frameDuration = LightSkillVisuals.BlazeFrameDuration;
                material = LightSkillVisuals.BlazeMaterial();
                sunderScaled = true;
                life = frameDuration * frames.Length + skill.RepeatSpacing * (hitsLeft - 1);
            }
        }
        // 화염 고리는 구워 둔 불 회오리 그림을 한 번 재생합니다. 없으면 기존 바닥 고리 그림입니다.
        whirlScaled = false;
        if (skill.FlameWhirl && skill.Shape == LightSkillShape.Ring)
        {
            Sprite[] whirl = LightSkillVisuals.BlazeFrames(LightSkillVisuals.FlameWhirlPrefix);
            if (whirl.Length > 0)
            {
                frames = whirl;
                frameDuration = LightSkillVisuals.BlazeFrameDuration;
                material = LightSkillVisuals.BlazeMaterial();
                whirlScaled = true;
                life = frameDuration * frames.Length + skill.RepeatSpacing * (hitsLeft - 1);
            }
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.sharedMaterial = material;
            spriteRenderer.sprite = frames.Length > 0 ? frames[Mathf.Clamp(frame, 0, frames.Length - 1)] : null;
            spriteRenderer.sortingOrder = 30;
        }
        // 화염 고리 그림은 바닥에 깔린 가로 타원이다. 조준과 상관없이 플레이어 주위의 가로 범위로 둔다.
        // 구운 원형 그림도 불기둥·낙하물이 위로 서 있으므로 돌리지 않는다.
        if (skill.Shape == LightSkillShape.Ring || (baked != null && skill.Shape == LightSkillShape.Circle))
            transform.rotation = Quaternion.identity;
        else if (baked != null)
            FaceDirection();
        else
            transform.right = this.direction;
        ApplyScale(sizeMul);
        if (hitCollider != null)
        {
            hitCollider.enabled = projectile;
            // 미사일 그림은 1배로 보여 주므로, 맞는 크기는 기존 탄환 배율을 곱했던 것과 같게 맞춥니다.
            // 구운 탄환 그림은 스탯 배율만큼만 키우므로 탄환 배율을 반경에 곱해 예전과 같은 크기로 맞습니다.
            hitCollider.radius = missileArt
                ? ProjectileHitRadius * ProjectileScale() * sizeMul
                : baked != null ? ProjectileHitRadius * ProjectileScale() : ProjectileHitRadius;
        }
        gameObject.name = skill.Name;
    }

    /// <summary>
    /// 적을 쫓는 미사일로 쏩니다. launchDirection으로 곧게 떠난 뒤, 시전 지점(castOrigin)에서 사거리 안에 있는 적 중
    /// 자신과 가장 가까운 적으로 휘어 갑니다. 적이 없으면 aim 쪽으로 날아갑니다. 닿은 적에게 일반 탄환처럼 한 번 피해를 주고 사라집니다.
    /// </summary>
    public void PlayHoming(LightSkillCast skill, Vector2 launchDirection, Vector2 aim, Vector2 castOrigin, float damage, string enemyTag, float projectileSize = 1f)
    {
        Play(skill, launchDirection, damage, enemyTag, projectileSize);
        if (!projectile) return;
        homing = true;
        this.castOrigin = castOrigin;
        fallbackAim = aim.sqrMagnitude > 0.0001f ? aim.normalized : direction;
        target = null;
        age = 0f;
        seekTimer = 0f;
        life += HomingExtraLife;
        FaceDirection();
    }

    // 휘는 동안 그림이 뒤집히지 않도록 Z축으로만 돌립니다.
    private void FaceDirection()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private float ProjectileScale() => Mathf.Clamp(skill.Width / 0.5f, 0.7f, 1.8f);

    private void Cache()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (hitCollider == null) hitCollider = GetComponent<CircleCollider2D>();
        if (pooled == null) pooled = GetComponent<PooledObject>();
    }

    private void ApplyScale(float projectileSize = 1f)
    {
        if (frames == null || frames.Length == 0) return;
        float width = frames[0].rect.width / LightSkillVisuals.PixelsPerUnit;
        float height = frames[0].rect.height / LightSkillVisuals.PixelsPerUnit;
        if (width <= 0f || height <= 0f) return;
        float sizeMul = Mathf.Max(0.01f, projectileSize);
        if (baked != null)
        {
            // 구운 그림은 플레이어와 같은 픽셀 크기입니다. 탄환은 스탯 배율만, 범위는 원점에서 판정 끝까지가 Range가 되도록 같은 비율로만 키웁니다.
            transform.localScale = projectile || baked.ReachUnits <= 0f
                ? Vector3.one * sizeMul
                : Vector3.one * (skill.Range / baked.ReachUnits);
            return;
        }
        switch (skill.Shape)
        {
            case LightSkillShape.Circle:
                float diameter = skill.Range * 2f;
                transform.localScale = Vector3.one * (diameter / height);
                break;
            case LightSkillShape.Ring:
                // 회오리 그림은 가운데에서 불 벽까지가 Range가 되도록 같은 비율로만 키웁니다.
                if (whirlScaled)
                    transform.localScale = Vector3.one * (skill.Range / (LightSkillVisuals.FlameWhirlReachPixels / LightSkillVisuals.PixelsPerUnit));
                else
                    transform.localScale = Vector3.one * ((skill.Range * 2f) / height);
                break;
            case LightSkillShape.Line:
                // 균열 그림은 가로세로를 따로 늘리지 않고 닿는 길이에 맞춰 같은 비율로만 키웁니다.
                if (sunderScaled)
                    transform.localScale = Vector3.one * (skill.Range / (LightSkillVisuals.SunderFireReachPixels / LightSkillVisuals.PixelsPerUnit));
                else
                    transform.localScale = new Vector3(skill.Range / width, Mathf.Max(0.4f, skill.Width) / height, 1f);
                break;
            case LightSkillShape.Cone:
            case LightSkillShape.Arc:
                transform.localScale = Vector3.one * (skill.Range / height);
                break;
            default:
                // 미사일 그림은 플레이어와 같은 픽셀 크기로 구웠으므로 기본은 1배, 스탯 배율만 적용합니다.
                transform.localScale = missileArt
                    ? Vector3.one * sizeMul
                    : Vector3.one * (ProjectileScale() * sizeMul);
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
            if (forking) AnimateForks();
            if (life <= 0f) Finish();
            return;
        }
        if (projectile)
        {
            if (homing)
            {
                FlyHoming(Time.deltaTime);
                return;
            }
            float step = ProjectileSpeed * Time.deltaTime;
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

    /// <summary>유도 미사일 한 프레임입니다. 표적을 고르고, 꺾을 수 있는 만큼만 방향을 틀어 나아갑니다.</summary>
    private void FlyHoming(float deltaTime)
    {
        age += deltaTime;
        Vector2 position = transform.position;
        if (!CanChase(target))
        {
            target = null;
            seekTimer -= deltaTime;
            if (seekTimer <= 0f)
            {
                seekTimer = HomingSeekInterval;
                target = ClosestEnemy(position);
            }
        }

        Vector2 wanted = target != null ? (Vector2)target.bounds.center - position : fallbackAim;
        if (age >= HomingStraightTime && wanted.sqrMagnitude > 0.0001f)
        {
            float turn = Mathf.Lerp(HomingTurnStart, HomingTurnMax,
                Mathf.InverseLerp(HomingStraightTime, HomingTurnRampEnd, age)) * deltaTime;
            float angle = Mathf.Clamp(Vector2.SignedAngle(direction, wanted), -turn, turn);
            direction = ((Vector2)(Quaternion.Euler(0f, 0f, angle) * (Vector3)direction)).normalized;
            FaceDirection();
        }

        float speed = ProjectileSpeed * Mathf.Lerp(HomingLaunchSpeedRatio, 1f, Mathf.Clamp01(age / HomingBoostTime));
        float step = speed * deltaTime;
        transform.position += (Vector3)(direction * step);
        traveled += step;

        // 휘어 가므로 날아간 길이가 아니라 시전 지점에서 사거리 밖으로 나가는 순간에 끝냅니다.
        Vector2 fromOrigin = (Vector2)transform.position - castOrigin;
        bool leaving = Vector2.Dot(fromOrigin, direction) > 0f;
        if (life <= 0f || (leaving && fromOrigin.sqrMagnitude >= skill.Range * skill.Range)) Finish();
    }

    private bool CanChase(Collider2D enemy)
    {
        if (enemy == null || !enemy.enabled || !enemy.gameObject.activeInHierarchy || !enemy.CompareTag(enemyTag)) return false;
        return !(enemy.TryGetComponent<EnemyHealth>(out EnemyHealth health) && health.IsDead);
    }

    /// <summary>시전 지점에서 사거리 안에 있는 살아 있는 적 중 from과 가장 가까운 적입니다.</summary>
    private Collider2D ClosestEnemy(Vector2 from)
    {
        Collider2D closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Collider2D candidate in Physics2D.OverlapCircleAll(castOrigin, skill.Range))
        {
            if (!CanChase(candidate)) continue;
            float distance = ((Vector2)candidate.bounds.center - from).sqrMagnitude;
            if (distance >= closestDistance) continue;
            closestDistance = distance;
            closest = candidate;
        }
        return closest;
    }

    private void Animate()
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null) return;
        if (clipTimed)
        {
            clipTime += Time.deltaTime;
            int index = Mathf.FloorToInt(clipTime / frameDuration);
            if (clipLoop) index %= frames.Length;
            else if (index >= frames.Length)
            {
                // 마지막 빈 프레임은 굽지 않았으므로 끝나면 숨깁니다. 남은 다단 히트는 계속 처리합니다.
                spriteRenderer.enabled = false;
                return;
            }
            if (index == frame) return;
            frame = index;
            spriteRenderer.sprite = frames[frame];
            return;
        }
        frameTimer += Time.deltaTime;
        if (frameTimer < frameDuration) return;
        frameTimer = 0f;
        bool loop = !bursting && !whirlScaled && (skill.Shape == LightSkillShape.Projectile || skill.Shape == LightSkillShape.Ring);
        if (!loop && frame >= frames.Length - 1) return;
        frame = (frame + 1) % frames.Length;
        spriteRenderer.sprite = frames[frame];
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!running || bursting || !projectile || other == null || !other.CompareTag("Enemy")) return;
        Vector3 impactPosition = other.ClosestPoint(transform.position);
        impactPosition.z = transform.position.z;
        if (Damage(other)) ProjectileHitFlash?.Invoke(impactPosition);
        if (!skill.ExplodesOnHit)
        {
            Finish();
            return;
        }
        if (skill.ForksOnHit) BurstForks();
        else
        {
            Splash();
            BeginImpactBurst();
        }
    }

    private void BurstForks()
    {
        forkFrames = LightSkillVisuals.BlazeFrames(LightSkillVisuals.BlazeForkPrefix);
        float reach = skill.SplashRadius;
        // 구운 그림이 없거나 반경이 없으면 예전 원형 폭발로 대신합니다.
        if (forkFrames.Length == 0 || reach <= 0f)
        {
            Splash();
            BeginImpactBurst();
            return;
        }

        bursting = true;
        forking = true;
        projectile = false;
        if (hitCollider != null) hitCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        int count = Random.Range(6, 9);
        float spin = Random.Range(0f, 360f);
        float scale = reach / (LightSkillVisuals.BlazeForkReachPixels / LightSkillVisuals.PixelsPerUnit);
        EnsureForks(count);
        Vector2 origin = transform.position;
        for (int i = 0; i < forks.Count; i++)
        {
            SpriteRenderer fork = forks[i];
            bool active = i < count;
            fork.gameObject.SetActive(active);
            if (!active) continue;
            float angle = spin + 360f * i / count + Random.Range(-10f, 10f);
            float length = Random.Range(0.92f, 1.08f);
            fork.transform.localPosition = Vector3.zero;
            fork.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            fork.transform.localScale = new Vector3(scale * length, scale * (Random.value < 0.5f ? 1f : -1f), 1f);
            fork.sprite = forkFrames[0];
            DamageFork(origin, angle, reach * length);
        }
        forkFrame = 0;
        forkTimer = 0f;
        life = LightSkillVisuals.BlazeFrameDuration * forkFrames.Length;
    }

    /// <summary>불줄기 한 가닥이 닿는 적에게 피해를 줍니다. 한 적은 줄기가 몇 개 닿아도 한 번만 맞습니다.</summary>
    private void DamageFork(Vector2 origin, float angleDegrees, float length)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        Vector2 center = origin + dir * (length * 0.5f);
        Vector2 size = new Vector2(length, ForkHitThickness);
        foreach (Collider2D hit in Physics2D.OverlapCapsuleAll(center, size, CapsuleDirection2D.Horizontal, angleDegrees))
            if (hit != null && hit.CompareTag(enemyTag)) Damage(hit);
    }

    private void EnsureForks(int count)
    {
        while (forks.Count < count)
        {
            var child = new GameObject("Blaze Fork");
            child.transform.SetParent(transform, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = LightSkillVisuals.BlazeMaterial();
            if (spriteRenderer != null) renderer.sortingLayerID = spriteRenderer.sortingLayerID;
            renderer.sortingOrder = 31;
            child.SetActive(false);
            forks.Add(renderer);
        }
    }

    private void AnimateForks()
    {
        if (forkFrames == null || forkFrames.Length == 0) return;
        forkTimer += Time.deltaTime;
        while (forkTimer >= LightSkillVisuals.BlazeFrameDuration && forkFrame < forkFrames.Length - 1)
        {
            forkTimer -= LightSkillVisuals.BlazeFrameDuration;
            forkFrame++;
        }
        for (int i = 0; i < forks.Count; i++)
            if (forks[i] != null && forks[i].gameObject.activeSelf) forks[i].sprite = forkFrames[forkFrame];
    }

    private void HideForks()
    {
        for (int i = 0; i < forks.Count; i++)
            if (forks[i] != null) forks[i].gameObject.SetActive(false);
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
        // 구운 탄환(화염구)은 폭발도 구운 그림으로 보여 줍니다. 가운데부터 폭발 반경까지가 그림의 reach입니다.
        LightSkillVisuals.BakedClip burst = baked != null ? LightSkillVisuals.Baked(LightSkillVisuals.FireballBurstClip) : null;
        if (burst != null && !burst.Loop && burst.ReachUnits > 0f && skill.SplashRadius > 0f)
        {
            frames = burst.Frames;
            frameDuration = burst.FrameDuration;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sharedMaterial = LightSkillVisuals.BlazeMaterial();
                spriteRenderer.sprite = frames[0];
                spriteRenderer.sortingOrder = 31;
            }
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * (skill.SplashRadius / burst.ReachUnits);
            clipTimed = true;
            clipLoop = false;
            clipTime = 0f;
            frame = 0;
            life = burst.Duration;
            return;
        }
        clipTimed = false;
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

    private bool Damage(Collider2D hit)
    {
        EntityId id = hit.GetEntityId();
        if (!hitIds.Add(id) || !hit.TryGetComponent<IDamageable>(out IDamageable damageable)) return false;
        EnemyHealth health = damageable as EnemyHealth;
        float previousHealth = health != null ? health.CurrentHealth : 0f;
        if (skill.Id == HeatWaveSkillId && health != null)
            health.TakeDamage(damage, HeatWaveKnockbackMultiplier);
        else
            damageable.TakeDamage(damage);
        return health == null || health.CurrentHealth < previousHealth;
    }

    private void Finish()
    {
        if (!running) return;
        running = false;
        if (pooled != null && pooled.IsPoolOwned) pooled.ReturnToPool();
        else Destroy(gameObject);
    }
}
