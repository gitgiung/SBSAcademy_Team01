using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// 검증 복사본의 Editor 폴더에서 실행. PlayerView와 Animator/클립은 원본 소스를 복사한다.
public static class ManualSkillAnimationProbe
{
    public static void Run()
    {
        string result = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "manual-animation-result.txt");
        int count = 0;
        GameObject root = null;
        try
        {
            void Check(bool ok, string name)
            {
                if (!ok) throw new Exception(name);
                count++;
                Debug.Log("PASS " + name);
            }
            root = new GameObject("Manual animation validation");
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("PlayerAnimator/Player");
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            var renderer = root.AddComponent<SpriteRenderer>();
            Type type = typeof(Player).Assembly.GetType("PlayerView");
            object view = Activator.CreateInstance(type, new object[] { root.transform, renderer, null, new PlayerData(), animator });
            var charge = type.GetMethod("SetHeavyChargeAnimation");
            Check(charge != null, "heavy charge presentation API exists");
            void Charging(bool active) => charge.Invoke(view, new object[] { active });
            void Move(bool dash = false) => type.GetMethod("SetMovementAnimation").Invoke(view,
                new object[] { Vector2.right, false, dash, .3f });
            void Step() { for (int i = 0; i < 20; i++) animator.Update(.05f); }
            bool State(string name) => animator.GetCurrentAnimatorStateInfo(0).IsName(name);

            Charging(true); Move(); Step();
            Check(State("Guard"), "charging holds Guard after the non-looping clip ends");
            Charging(true); animator.Update(0);
            Check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f,
                "charging holds the completed Guard pose without restarting");
            for (int i = 0; i < 40; i++) { Charging(true); animator.Update(.05f); }
            Charging(true); animator.Update(0);
            Check(State("Guard") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f,
                "held charging keeps the completed Guard pose over multiple periods");
            Check(!animator.GetBool("IsDashing"), "charging does not activate dash animation");
            Check(renderer.color == Color.white, "charging does not apply defense tint");
            Check(!root.transform.Find("Defense VFX").GetComponent<SpriteRenderer>().enabled,
                "charging does not enable defense effects");
            Charging(false);
            type.GetMethod("PlayLightAttack").Invoke(view, new object[] { "LightAttack", Vector2.up });
            Check(State("LightAttack"), "release immediately enters attack");
            Step(); Check(State("Idle"), "attack returns to Idle");
            type.GetMethod("PlayLightAttack").Invoke(view, new object[] { "LightAttack", Vector2.left });
            Check(State("LightAttack"), "next manual attack restarts the attack state");
            Step();
            Charging(true); Step(); Charging(false); Step();
            Check(State("Idle"), "canceled charge returns to Idle");
            Charging(true); Step();
            type.GetMethod("Reset").Invoke(view, null); Step();
            Check(State("Idle") && !animator.GetBool("IsGuarding"), "reset clears charge presentation");
            Move(true); Step(); Check(State("Charge"), "dash still uses Charge");
            Move(); Step(); Check(State("Idle"), "dash release returns to Idle");
            type.GetMethod("SetGuardAnimation").Invoke(view, new object[] { true });
            Charging(false); Step();
            Check(State("Guard"), "clearing charge does not cancel actual guard presentation");
            Check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f,
                "ordinary guarding retains its non-looping hold");
            Charging(true);
            type.GetMethod("SetGuardAnimation").Invoke(view, new object[] { false });
            Step(); Check(State("Guard"), "defense cleanup does not cancel active charging pose");
            Charging(false); Step(); Check(State("Idle"), "both guard sources cleared return to Idle");
            var owner = root.AddComponent<Player>();
            var timingData = new PlayerData();
            owner.InitializeTiming(view, timingData);
            Type skillType = typeof(Player).Assembly.GetType("PlayerLightSkill");
            var beginManual = skillType.GetMethod("BeginManualCast");
            Check(beginManual != null, "manual casts support animation-timed emission");
            int emitted = 0;
            int Begin()
            {
                Check(owner.TryBeginLightAttack(0, out int id), "manual action begins");
                beginManual.Invoke(owner.TimingSkill, new object[] { id, (Action)(() => emitted++) });
                type.GetMethod("PlayLightAttack").Invoke(view, new object[] { "LightAttack", Vector2.right });
                return id;
            }
            int firstId = Begin();
            owner.NotifyLightAttackAnimationProgress(firstId, .249f);
            Check(emitted == 0 && owner.TimingLocked, "windup prevents emission and movement before 0.25");
            owner.NotifyLightAttackAnimationProgress(firstId, .25f);
            Check(emitted == 1 && owner.TimingLocked, "impact at 0.25 retains recovery lock");
            owner.NotifyLightAttackAnimationProgress(firstId, .6f);
            Check(emitted == 1 && owner.TimingLocked, "progress never duplicates emission or releases recovery");
            Step();
            Check(!owner.TimingLocked && emitted == 1, "actual Animator exit releases movement lock");
            int secondId = Begin();
            owner.NotifyLightAttackAnimationFinished(firstId);
            Check(owner.TimingLocked, "stale animation exit cannot unlock the next attack");
            animator.Update(.24f); animator.Update(0);
            Check(emitted == 1 && owner.TimingLocked, "actual Animator holds windup at 0.24");
            animator.Update(.02f); animator.Update(0);
            Check(emitted == 2 && owner.TimingLocked, "actual Animator emits after crossing 0.25");
            animator.Update(.48f); animator.Update(0);
            Check(owner.TimingLocked, "recovery is still locked at motion time 0.74");
            animator.Update(.02f); animator.Update(0);
            Check(!owner.TimingLocked, "second attack unlocks at motion completion");
            Begin();
            Time.timeScale = 0;
            owner.NotifyLightAttackAnimationProgress(owner.ActiveLightAttackId, .3f);
            Check(emitted == 2 && owner.TimingLocked, "pause does not emit or unlock");
            Time.timeScale = 1;
            animator.Play("Idle", 0, 0); animator.Update(0);
            owner.CheckTimingAnimation();
            Check(emitted == 2 && !owner.TimingLocked, "interrupted windup cancels without a late shot");
            Begin(); animator.enabled = false; owner.CheckTimingAnimation();
            Check(!owner.TimingLocked, "disabled Animator cannot strand the action lock");
            animator.enabled = true;
            Begin(); animator.Update(1f); animator.Update(0);
            Check(emitted == 3 && !owner.TimingLocked, "large frame step emits once and completes");
            Begin(); timingData.IsDead = true; owner.CheckTimingAnimation();
            Check(emitted == 3 && !owner.TimingLocked, "death cancels pending emission and clears lock");
            timingData.IsDead = false;
            Begin(); animator.runtimeAnimatorController = null; owner.CheckTimingAnimation();
            Check(emitted == 3 && !owner.TimingLocked, "removed controller cancels pending emission and clears lock");
            File.WriteAllText(result, $"PASS {count}/{count}");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllText(result, $"FAIL after {count}: {error}");
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
        finally { Time.timeScale = 1; if (root != null) UnityEngine.Object.DestroyImmediate(root); }
    }
}
