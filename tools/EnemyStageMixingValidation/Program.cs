using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

static class Program
{
    const BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Instance;
    static int passed, failed;
    static void Test(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + (e.InnerException ?? e).Message); }
    }
    static void Check(bool value, string message = "assertion failed") { if (!value) throw new Exception(message); }
    static void Set(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, Fields) ?? throw new Exception("Missing field " + name);
        field.SetValue(target, field.FieldType.IsEnum ? Enum.ToObject(field.FieldType, value) : value);
    }
    static object Get(object target, string name) => target.GetType().GetField(name, Fields).GetValue(target);
    static object Profile()
    {
        Type type = typeof(EnemySpawnDirector).Assembly.GetType("EnemySpawnProgression");
        Check(type != null, "EnemySpawnProgression is missing");
        object profile = Activator.CreateInstance(type);
        Array types = (Array)Get(profile, "types");
        Check(types.Length == 4, "expected four archetypes");
        for (int i = 0; i < 4; i++)
        {
            var stages = new GameObject[6];
            for (int s = 0; s < 6; s++)
            {
                stages[s] = new GameObject($"Type{i}_S{s}");
                stages[s].AddComponent<EnemyHealth>(); stages[s].AddComponent<EnemyFollow>();
            }
            Set(types.GetValue(i), "stagePrefabs", stages);
        }
        return profile;
    }
    static bool Valid(object p) => (bool)p.GetType().GetProperty("IsValid").GetValue(p);
    static (GameObject[] prefabs, int[] weights) Roster(object p, double time)
    {
        object[] args = { time, 1080d, null, null };
        p.GetType().GetMethod("BuildRoster").Invoke(p, args);
        return ((GameObject[])args[2], (int[])args[3]);
    }
    static double StageShare(object p, double time, int stage)
    {
        var r = Roster(p, time);
        return r.weights.Where((w, i) => r.prefabs[i].name.EndsWith("S" + stage)).Sum() / (double)r.weights.Sum();
    }
    static void Near(double expected, double actual) => Check(Math.Abs(expected - actual) < .002, $"expected {expected}, actual {actual}");
    static (EnemySpawnDirector d, TestPool pool) Director(object p)
    {
        var d = new EnemySpawnDirector(); var pool = new TestPool();
        Set(d, "poolProvider", pool); Set(d, "player", new Transform()); Set(d, "playerCamera", new Camera());
        Set(d, "spawnerTemplate", new EnemySpawner());
        if (p != null) { Set(d, "rosterMode", 1); Set(d, "stageMixing", p); }
        else
        {
            var wave = new EnemySpawnWave(); var entry = new EnemySpawnWave.Entry();
            var enemy = new GameObject("manual"); enemy.AddComponent<EnemyHealth>(); enemy.AddComponent<EnemyFollow>();
            Set(entry, "prefab", enemy); Set(entry, "weight", 1); Set(wave, "enemies", new[] { entry }); Set(d, "waves", new[] { wave });
        }
        d.Initialize(); return (d, pool);
    }
    static void Advance(EnemySpawnDirector d, float seconds)
    {
        int steps = (int)(seconds * 4); for (int i = 0; i < steps; i++) d.Tick(.25f);
    }
    static void Main()
    {
        Test("24 stage prefabs validate", () => Check(Valid(Profile())));
        Test("first stage until three minutes", () => { var p = Profile(); Near(1, StageShare(p, 0, 0)); Near(1, StageShare(p, 179.999, 0)); });
        Test("boundary starts at zero new share", () => Near(0, StageShare(Profile(), 180, 1)));
        Test("30 second mixing steps", () => { var p = Profile(); Near(1d/3, StageShare(p, 210, 1)); Near(2d/3, StageShare(p, 240, 1)); });
        Test("90 seconds completes replacement", () => Near(1, StageShare(Profile(), 270, 1)));
        Test("every boundary uses previous and current only", () => { var p = Profile(); for (int s = 1; s < 6; s++) { Near(1, StageShare(p, s*180, s-1)); Near(1, StageShare(p, s*180+90, s)); Near(1, StageShare(p, s*180+30, s-1) + StageShare(p, s*180+30, s)); } });
        Test("final stage clamps beyond end", () => { var p = Profile(); Near(1, StageShare(p, 990, 5)); Near(1, StageShare(p, 1080, 5)); Near(1, StageShare(p, 5000, 5)); });
        Test("zero transition switches at boundary", () => { var p = Profile(); Set(p, "transitionSeconds", 0f); Near(1, StageShare(p, 180, 1)); });
        Test("full stage transition never introduces future", () => { var p = Profile(); Set(p, "transitionSeconds", 180f); Check(Valid(p)); Near(.5, StageShare(p, 270, 1)); Near(1, StageShare(p, 360, 1)); });
        Test("invalid transition rejected", () => { foreach (float value in new[] {-1f, 181f, float.NaN, float.PositiveInfinity}) { var p=Profile(); Set(p,"transitionSeconds",value); Check(!Valid(p)); } });
        Test("missing prefab rejected", () => { var p=Profile(); ((GameObject[])Get(((Array)Get(p,"types")).GetValue(0),"stagePrefabs"))[2]=null; Check(!Valid(p)); });
        Test("missing root components rejected", () => { var p=Profile(); ((GameObject[])Get(((Array)Get(p,"types")).GetValue(0),"stagePrefabs"))[2]=new GameObject(); Check(!Valid(p)); });
        Test("all zero endpoint rejected", () => { foreach (string endpoint in new[]{"earlyWeight","lateWeight"}) { var p=Profile(); foreach (object t in (Array)Get(p,"types")) Set(t,endpoint,0); Check(!Valid(p)); } });
        Test("zero archetype never selected", () => { var p=Profile(); object t=((Array)Get(p,"types")).GetValue(0); Set(t,"earlyWeight",0); Set(t,"lateWeight",0); var r=Roster(p,240); Check(r.weights.Where((w,i)=>r.prefabs[i].name.StartsWith("Type0_")).All(w=>w==0)); });
        Test("early late archetype weights interpolate", () => { var p=Profile(); var types=(Array)Get(p,"types"); foreach(object t in types) {Set(t,"earlyWeight",0);Set(t,"lateWeight",0);} Set(types.GetValue(0),"earlyWeight",100); Set(types.GetValue(1),"lateWeight",100); var r=Roster(p,540); Near(.5,r.weights.Where((w,i)=>r.prefabs[i].name.StartsWith("Type0_")).Sum()/(double)r.weights.Sum()); });
        Test("director no early stage then gradual replacement", () => { var (d,pool)=Director(Profile()); Advance(d,210); Check(pool.Spawns.All(x=>x.name.EndsWith("S0"))); pool.Spawns.Clear(); Advance(d,30); Check(pool.Spawns.Any(x=>x.name.EndsWith("S1"))); Check(pool.Spawns.Any(x=>x.name.EndsWith("S0"))); pool.Spawns.Clear(); Advance(d,60); Check(pool.Spawns.Any(x=>x.name.EndsWith("S1"))); });
        Test("pause freezes progression and spawning", () => { var (d,pool)=Director(Profile()); Advance(d,210); int count=pool.Spawns.Count; d.SetPaused(true); d.Tick(90); Check(d.ElapsedTime==210 && pool.Spawns.Count==count); d.SetPaused(false); Advance(d,30); Check(d.ElapsedTime==240); });
        Test("pause boundary respected", () => { var (d,pool)=Director(Profile()); d.SetPauseBoundary(180); d.Tick(240); Check(d.ElapsedTime==180); d.Tick(90); Check(d.ElapsedTime==180); d.ClearPauseBoundary(); Advance(d,90); Check(d.ElapsedTime==270); });
        Test("large tick visits every wave roster", () => { var (d,pool)=Director(Profile()); d.Settings.maxAttemptsPerFrame=10000; d.Tick(300); Check(pool.Spawns.Take(50+60+70+80+90+100+110).All(x=>x.name.EndsWith("S0"))); Check(pool.Spawns.Any(x=>x.name.EndsWith("S1"))); Check(d.ElapsedTime==300); });
        Test("capacity failure remains counted", () => { var (d,pool)=Director(Profile()); pool.Failure=EnemySpawnFailure.Capacity; Advance(d,30); Check(d.CapSkippedAttempts==50 && d.SuccessfulSpawns==0); });
        Test("unavailable prefab does not starve other types", () => { var (d,pool)=Director(Profile()); pool.RejectType0=true; Advance(d,30); Check(d.PoolFailedAttempts>0 && d.SuccessfulSpawns>0); });
        Test("manual waves remain default", () => { var (d,pool)=Director(null); Advance(d,90); Check(pool.Spawns.Count==180 && pool.Spawns.All(p=>p.name=="manual")); });
        Test("18 minute stop and normal budget preserved", () => { var (d,pool)=Director(Profile()); Advance(d,1100); Check(d.ElapsedTime==1080); Check(d.Settings.ScheduledAttempts(1080)==8100); Check(pool.Spawns.Last().name.EndsWith("S5")); });
        Console.WriteLine($"RESULT {passed}/{passed+failed} passed"); Environment.ExitCode=failed==0?0:1;
    }
}

sealed class TestPool : MonoBehaviour, IEnemySpawnPool, IEnemySpawnDiagnostics
{
    public readonly List<GameObject> Spawns = new List<GameObject>();
    public EnemySpawnFailure Failure;
    public bool RejectType0;
    public int ActiveEnemyCount => 0;
    public EnemySpawnFailure LastSpawnFailure { get; private set; }
    public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, Scene scene)
    {
        LastSpawnFailure = Failure != EnemySpawnFailure.None ? Failure : RejectType0 && prefab.name.StartsWith("Type0_") ? EnemySpawnFailure.PoolUnavailable : EnemySpawnFailure.None;
        if (LastSpawnFailure != EnemySpawnFailure.None) return false;
        Spawns.Add(prefab); return true;
    }
    public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, Scene scene, Action<GameObject> spawned) => TrySpawn(prefab, position, target, scene);
    public void ReturnEnemy(GameObject enemy) { }
}
