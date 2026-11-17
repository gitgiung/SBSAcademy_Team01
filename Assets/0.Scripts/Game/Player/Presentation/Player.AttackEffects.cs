using System;
using System.Collections;
using UnityEngine;

public sealed partial class Player
{
    private void SpawnLightSkill(LightSkillCast skill, Vector2 direction, float damage, float projectileSize, bool impactHaptic)
    {
        EnsureLightFxPrototype();
        Vector2 aim = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector3 position = SkillOrigin(skill.Shape, aim);
        void Arm(GameObject fx)
        {
            if (fx.TryGetComponent<LightSkillFx>(out LightSkillFx skillFx))
            {
                skillFx.Play(skill, direction, damage, enemyTag, projectileSize);
                skillFx.ProjectileHitFlash = ShowProjectileHitFlash;
                skillFx.ImpactHaptic = impactHaptic;
            }
        }

        PopLightFx(position, Arm);
    }

    /// <summary>
    /// 원·고리는 플레이어 그림의 정중앙입니다. 직선·부채·호는 그 중심에서 몸 바깥으로 시작선을 둡니다.
    /// 사거리의 일정 비율로 밀지 않습니다.
    /// </summary>
    private Vector3 SkillOrigin(LightSkillShape shape, Vector2 aim)
    {
        Vector2 center = playerSpriteRenderer != null && playerSpriteRenderer.sprite != null
            ? (Vector2)playerSpriteRenderer.bounds.center : (Vector2)transform.position;
        if (shape == LightSkillShape.Circle || shape == LightSkillShape.Ring || shape == LightSkillShape.Projectile)
            return center;
        Vector2 extents = playerSpriteRenderer != null && playerSpriteRenderer.sprite != null
            ? (Vector2)playerSpriteRenderer.bounds.extents : Vector2.zero;
        if (extents == Vector2.zero)
        {
            Collider2D body = GetComponent<Collider2D>();
            if (body != null) extents = body.bounds.extents;
        }
        float clearance = Mathf.Abs(aim.x) * extents.x + Mathf.Abs(aim.y) * extents.y;
        return center + aim * clearance;
    }

    /// <summary>구운 태양 탄막 그림이 있을 때만 태양 문양을 돌려줍니다. 없으면 null이고 예전 탄환 연사로 쏩니다.</summary>
    private SunBarrageCrest SunCrestFor(LightSkillCast skill)
    {
        if (!skill.SunBarrage || skill.Shape != LightSkillShape.Projectile) return null;
        if (LightSkillVisuals.BlazeFrames(LightSkillVisuals.SunMissilePrefix).Length == 0) return null;
        if (sunCrest == null) sunCrest = SunBarrageCrest.Attach(transform);
        return sunCrest.Ready ? sunCrest : null;
    }

    /// <summary>태양 문양의 긴 가시 끝에서 유도 미사일 한 발을 쏩니다. 적이 없으면 aim 쪽으로 날아갑니다.</summary>
    private void SpawnSunMissile(LightSkillCast skill, SunBarrageCrest crest, int spike, Vector2 aim, float damage, float projectileSize, bool impactHaptic)
    {
        EnsureLightFxPrototype();
        Vector2 castOrigin = transform.position;
        Vector2 outward = crest.SpikeDirection(spike);
        crest.Flare(spike);
        void Arm(GameObject fx)
        {
            if (fx.TryGetComponent<LightSkillFx>(out LightSkillFx skillFx))
            {
                skillFx.PlayHoming(skill, outward, aim, castOrigin, damage, enemyTag, projectileSize);
                skillFx.ProjectileHitFlash = ShowProjectileHitFlash;
                skillFx.ImpactHaptic = impactHaptic;
            }
        }

        PopLightFx(crest.SpikeTip(spike), Arm);
    }

    private void ShowProjectileHitFlash(Vector3 position)
    {
        if (!isActiveAndEnabled || projectileHitFlashScale <= 0f ||
            projectileHitFlashFrames == null || projectileHitFlashFrames.Length == 0)
            return;

        StartCoroutine(PlayProjectileHitFlash(position));
    }

    private IEnumerator PlayProjectileHitFlash(Vector3 position)
    {
        const int frameCount = 8;
        const float frameDuration = 1f / 30f;
        const float initialScaleRatio = 0.25f;
        float finalScale = projectileHitFlashScale;
        GameObject flash = new GameObject("Projectile Hit Flash");
        flash.transform.position = position;
        SpriteRenderer renderer = flash.AddComponent<SpriteRenderer>();
        if (projectileHitFlashMaterial != null) renderer.sharedMaterial = projectileHitFlashMaterial;
        if (playerSpriteRenderer != null) renderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
        renderer.sortingOrder = 32;
        // Player가 비활성화되어 재생 코루틴이 멈춰도 섬광을 정리합니다.
        Destroy(flash, 1f);
        var frameWait = new WaitForSeconds(frameDuration);
        for (int frame = 0; frame < frameCount; frame++)
        {
            if (renderer == null) yield break;
            int spriteIndex = frame * projectileHitFlashFrames.Length / frameCount;
            renderer.sprite = projectileHitFlashFrames[spriteIndex];
            float progress = (float)frame / (frameCount - 1);
            flash.transform.localScale = Vector3.one * (finalScale * Mathf.Lerp(initialScaleRatio, 1f, progress));
            renderer.color = new Color(1f, 1f, 1f, 1f - progress);
            yield return frameWait;
        }
        Destroy(flash);
    }

    private void PopLightFx(Vector3 position, Action<GameObject> arm)
    {
        if (Managers.Initialized && Managers.Pool != null)
            Managers.Pool.Pop(lightFxPrototype, position, Quaternion.identity, initialize: arm);
        else
        {
            GameObject fx = Instantiate(lightFxPrototype, position, Quaternion.identity);
            fx.SetActive(true);
            arm(fx);
        }
    }

    private void EnsureLightFxPrototype()
    {
        if (lightFxPrototype != null) return;
        lightFxPrototype = new GameObject("LightSkillFx");
        lightFxPrototype.SetActive(false);
        lightFxPrototype.AddComponent<SpriteRenderer>();
        CircleCollider2D collider = lightFxPrototype.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        Rigidbody2D body = lightFxPrototype.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.useFullKinematicContacts = true;
        lightFxPrototype.AddComponent<PooledObject>();
        lightFxPrototype.AddComponent<LightSkillFx>();
    }
}
