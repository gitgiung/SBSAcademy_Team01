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
            Charging(true);
            type.GetMethod("SetGuardAnimation").Invoke(view, new object[] { false });
            Step(); Check(State("Guard"), "defense cleanup does not cancel active charging pose");
            Charging(false); Step(); Check(State("Idle"), "both guard sources cleared return to Idle");
            File.WriteAllText(result, $"PASS {count}/{count}");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllText(result, $"FAIL after {count}: {error}");
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
    }
}
