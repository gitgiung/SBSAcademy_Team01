using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SurvivalIntegrationTests
{
    GameObject root, enemyPrefab, pickupPrefab, playerObject, cameraObject;
    SceneEnemyPool pool;
    PlayerData player;
    EnemySpawnDirector director;
    static void Set(object target,string name,object value) => (target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)
        ?? throw new Exception("설정 없음: "+name)).SetValue(target,value);
    static object Get(object target,string name) => (target.GetType().GetProperty(name)
        ?? throw new Exception("계약 없음: "+name)).GetValue(target);
    static bool Collect(ExperiencePickup p,PlayerData d)
    {
        var method=typeof(ExperiencePickup).GetMethod("TryCollect"); Assert.That(method,Is.Not.Null,"중복 회수 방어/풀 반납 API 필요");
        return (bool)method.Invoke(p,new object[]{d});
    }
    static ExperiencePickup RentedPickup() => UnityEngine.Object.FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None)
        .First(p=>p.GetComponent<PooledObject>()!=null && p.GetComponent<PooledObject>().IsRented);
    [SetUp] public void Setup()
    {
        Time.timeScale=1; Managers.Pool=new PoolManager();
        root=new GameObject("SpawnWorld"); root.SetActive(false); pool=root.AddComponent<SceneEnemyPool>();
        playerObject=new GameObject("PlayerData"); player=playerObject.AddComponent<PlayerData>();
        cameraObject=new GameObject("Camera"); var camera=cameraObject.AddComponent<Camera>(); camera.orthographic=true; camera.transform.position=new Vector3(0,0,-10);
        pickupPrefab=new GameObject("XP"); pickupPrefab.SetActive(false); pickupPrefab.AddComponent<SpriteRenderer>(); pickupPrefab.AddComponent<CircleCollider2D>().isTrigger=true;
        var pickup=pickupPrefab.AddComponent<ExperiencePickup>(); Set(pickup,"experienceAmount",1f);
        enemyPrefab=new GameObject("Enemy"); enemyPrefab.SetActive(false); enemyPrefab.AddComponent<CircleCollider2D>(); enemyPrefab.AddComponent<EnemyFollow>();
        var health=enemyPrefab.AddComponent<EnemyHealth>(); Set(health,"experiencePickupPrefab",pickupPrefab);
        root.SetActive(true);
    }
    void Director(int spawners=5)
    {
        root.SetActive(false); director=root.AddComponent<EnemySpawnDirector>();
        var templateObject=new GameObject("SpawnerTemplate"); templateObject.transform.SetParent(root.transform); templateObject.SetActive(false);
        var template=templateObject.AddComponent<EnemySpawner>(); var minute=new EnemySpawnMinute(); Set(minute,"enemyPrefabs",new[]{enemyPrefab});
        Set(director,"poolProvider",pool); Set(director,"player",player.transform); Set(director,"playerCamera",cameraObject.GetComponent<Camera>());
        Set(director,"spawnerTemplate",template); Set(director,"spawnerCount",spawners); Set(director,"minutes",new[]{minute});
        root.SetActive(true); director.Initialize();
    }
    GameObject Spawn()
    {
        GameObject result=null; Assert.That(pool.TrySpawn(enemyPrefab,Vector3.zero,player.transform,root.scene,e=>result=e),Is.True); return result;
    }
    [TearDown] public void Cleanup()
    {
        UnityEngine.Object.DestroyImmediate(root); Managers.Pool.Dispose(); Managers.Pool=null;
        UnityEngine.Object.DestroyImmediate(enemyPrefab); UnityEngine.Object.DestroyImmediate(pickupPrefab); UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(cameraObject);
        Time.timeScale=1;
    }
    [Test] public void KillDropsOnceAndOnlyCollectionAwardsXP()
    {
        var enemy=Spawn(); var health=enemy.GetComponent<EnemyHealth>(); health.TakeDamage(3); health.TakeDamage(3);
        Assert.That(Managers.Pool.GetActiveCount(pickupPrefab),Is.EqualTo(1)); Assert.That(player.CurrentEXP,Is.Zero);
        var pickup=RentedPickup(); Assert.That((float)Get(pickup,"RawExperience"),Is.EqualTo(2));
        Assert.That(Collect(pickup,player),Is.True); Assert.That(Collect(pickup,player),Is.False);
        Assert.That(player.CurrentEXP,Is.EqualTo(2)); Assert.That(Managers.Pool.GetActiveCount(pickupPrefab),Is.Zero); Assert.That(pool.ActiveEnemyCount,Is.Zero);
    }
    [UnityTest]
    public IEnumerator PooledAreaSkillKillReturnsEnemyAndDropsExperience(
        [Values(LightSkillShape.Circle, LightSkillShape.Ring, LightSkillShape.Line,
            LightSkillShape.Cone, LightSkillShape.Arc)] LightSkillShape shape)
    {
        enemyPrefab.tag = "Respawn";
        GameObject enemy = Spawn();
        Vector3 origin = Vector3.left * 2f;
        var skill = new LightSkillCast(10003, "Area skill", "ELEM_FIRE", 0f, shape,
            2.5f, 90f, 0.8f, 1, 1, 1f, 3f, 0f);
        var effectPrefab = new GameObject("Area skill prefab");
        effectPrefab.transform.SetParent(root.transform);
        effectPrefab.SetActive(false);
        effectPrefab.AddComponent<LightSkillFx>();
        Physics2D.SyncTransforms();

        Managers.Pool.Pop(effectPrefab, origin, Quaternion.identity, initialize: effect =>
            effect.GetComponent<LightSkillFx>().Play(skill, Vector2.right, skill.Damage(0f), enemyPrefab.tag));
        yield return null;
        yield return null;

        Assert.That(enemy.GetComponent<EnemyHealth>().IsDead, Is.True);
        Assert.That(enemy.activeSelf, Is.False);
        Assert.That(pool.ActiveEnemyCount, Is.Zero);
        Assert.That(Managers.Pool.GetActiveCount(pickupPrefab), Is.EqualTo(1));
    }
    [Test] public void DespawnAndExternalReturnsDoNotDropAndCounterRecovers()
    {
        var enemy=Spawn(); enemy.GetComponent<EnemyHealth>().Despawn();
        Assert.That(Managers.Pool.GetActiveCount(pickupPrefab),Is.Zero); Assert.That(pool.ActiveEnemyCount,Is.Zero);
        enemy=Spawn(); Managers.Pool.Push(enemy); Assert.That(pool.ActiveEnemyCount,Is.Zero);
        enemy=Spawn(); UnityEngine.Object.DestroyImmediate(enemy); Assert.That(pool.ActiveEnemyCount,Is.Zero);
    }
    [Test] public void ReturnedPickupCanAwardAgainWithNewRawXP()
    {
        Spawn().GetComponent<EnemyHealth>().TakeDamage(3); var first=RentedPickup(); Collect(first,player);
        Spawn().GetComponent<EnemyHealth>().TakeDamage(3); var second=RentedPickup(); Assert.That(second,Is.SameAs(first));
        Assert.That(Collect(second,player),Is.True); Assert.That(player.CurrentEXP,Is.EqualTo(4));
    }
    [Test] public void NormalCapIs130AndDuplicateReturnDoesNotDoubleDecrement()
    {
        GameObject first=null; for(int i=0;i<130;i++) { var e=Spawn(); if(i==0) first=e; }
        Assert.That(pool.TrySpawn(enemyPrefab,Vector3.zero,player.transform,root.scene),Is.False); Assert.That(pool.ActiveEnemyCount,Is.EqualTo(130));
        pool.ReturnEnemy(first); pool.ReturnEnemy(first); Assert.That(pool.ActiveEnemyCount,Is.EqualTo(129)); Spawn(); Assert.That(pool.ActiveEnemyCount,Is.EqualTo(130));
    }
    [Test] public void FiveSpawnersShareFiftyAttemptsInsteadOfMultiplying()
    {
        Director(); for(int i=0;i<300;i++) director.Tick(.1f);
        Assert.That((int)Get(director,"SuccessfulSpawns"),Is.EqualTo(50)); Assert.That((int)Get(director,"AttemptedSpawns"),Is.EqualTo(50));
        Assert.That(director.ElapsedTime,Is.EqualTo(30).Within(.001f));
    }
    [Test] public void FrameSpikeIsBoundedAndSkippedAttemptsAreNotQueued()
    {
        Director(); director.Tick(100);
        Assert.That((int)Get(director,"AttemptedSpawns"),Is.LessThanOrEqualTo(8)); Assert.That((int)Get(director,"FrameSkippedAttempts"),Is.GreaterThan(0));
        int before=(int)Get(director,"AttemptedSpawns"); director.Tick(.001f); Assert.That((int)Get(director,"AttemptedSpawns"),Is.EqualTo(before));
    }
    [Test] public void PausedAndTimeScaleZeroDoNotAdvanceAnd18MinutesStopsOnlySpawn()
    {
        Director(); director.SetPaused(true); director.Tick(20); Assert.That(director.ElapsedTime,Is.Zero);
        director.SetPaused(false); Time.timeScale=0; director.Tick(20); Assert.That(director.ElapsedTime,Is.Zero); Time.timeScale=1;
        director.Tick(1080); int before=(int)Get(director,"AttemptedSpawns"); director.Tick(10);
        Assert.That(director.ElapsedTime,Is.EqualTo(1080)); Assert.That((int)Get(director,"AttemptedSpawns"),Is.EqualTo(before)); Assert.That(Time.timeScale,Is.EqualTo(1));
    }
    [Test] public void RewardIsSnapshotAtSpawnAcross180SecondBoundary()
    {
        Director(0); director.Tick(179.999f); var enemy=Spawn(); director.Tick(.002f); enemy.GetComponent<EnemyHealth>().TakeDamage(3);
        Assert.That((float)Get(RentedPickup(),"RawExperience"),Is.EqualTo(2));
        Spawn().GetComponent<EnemyHealth>().TakeDamage(3);
        Assert.That(UnityEngine.Object.FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None).Any(p=>(float)Get(p,"RawExperience")==3),Is.True);
    }
    [Test] public void ExistingBossEncounterUsesDirectorClockAndKeepsCombatRunning()
    {
        Director(); var bossObject=new GameObject("Boss"); bossObject.SetActive(false); bossObject.transform.SetParent(root.transform);
        bossObject.AddComponent<EnemyHealth>(); var boss=bossObject.AddComponent<BossController>();
        var encounterObject=new GameObject("Encounter"); encounterObject.SetActive(false); encounterObject.transform.SetParent(root.transform);
        var encounter=encounterObject.AddComponent<BossEncounterController>(); Set(encounter,"boss",boss); Set(encounter,"spawnDirector",director); Set(encounter,"bossAppearTime",1f); encounterObject.SetActive(true);
        director.Tick(2); Assert.That(encounter.Phase,Is.EqualTo(EncounterPhase.BossFight)); Assert.That(director.ElapsedTime,Is.EqualTo(1)); Assert.That(encounter.ElapsedTime,Is.EqualTo(1)); Assert.That(Time.timeScale,Is.EqualTo(1));
        director.Tick(10); Assert.That(director.ElapsedTime,Is.EqualTo(1)); bossObject.GetComponent<EnemyHealth>().TakeDamage(3); director.Tick(.5f);
        Assert.That(encounter.ElapsedTime,Is.EqualTo(1.5f)); Assert.That(encounter.Phase,Is.EqualTo(EncounterPhase.AfterBoss));
    }
    [Test] public void DisabledBossControllerDoesNotLeaveAnArmedStopBoundary()
    {
        Director(); var bossObject=new GameObject("Boss"); bossObject.SetActive(false); bossObject.transform.SetParent(root.transform);
        bossObject.AddComponent<EnemyHealth>(); var boss=bossObject.AddComponent<BossController>();
        var encounterObject=new GameObject("Encounter"); encounterObject.SetActive(false); encounterObject.transform.SetParent(root.transform);
        var encounter=encounterObject.AddComponent<BossEncounterController>(); Set(encounter,"boss",boss); Set(encounter,"spawnDirector",director); Set(encounter,"bossAppearTime",1f); encounterObject.SetActive(true);
        encounter.enabled=false; director.Tick(2); director.Tick(1); Assert.That(director.ElapsedTime,Is.EqualTo(3));
    }
    [Test] public void ReentrantRentDuringXPNotificationDoesNotReturnNewPickupLease()
    {
        Spawn().GetComponent<EnemyHealth>().TakeDamage(3); var first=RentedPickup(); ExperiencePickup rerented=null;
        Action<float,float> onXP=(a,b)=>{ Managers.Pool.Push(first.gameObject); pool.TryDropExperience(pickupPrefab,Vector3.zero,player.transform,9); rerented=RentedPickup(); };
        player.EXPChanged+=onXP; Assert.That(Collect(first,player),Is.True); player.EXPChanged-=onXP;
        Assert.That(rerented,Is.SameAs(first)); Assert.That(rerented.GetComponent<PooledObject>().IsRented,Is.True);
        Assert.That(player.CurrentEXP,Is.EqualTo(2)); Assert.That((float)Get(rerented,"RawExperience"),Is.EqualTo(9));
    }
    [Test] public void CapFailureConsumesAttemptWithoutBackfill()
    {
        Director(); director.Settings.maxActiveNormalEnemies=1;
        for(int i=0;i<300;i++) director.Tick(.1f);
        Assert.That(director.SuccessfulSpawns,Is.EqualTo(1)); Assert.That(director.CapSkippedAttempts,Is.EqualTo(49)); Assert.That(director.AttemptedSpawns,Is.EqualTo(50));
        var enemy=UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).First(h=>h.GetComponent<PooledObject>()!=null && h.GetComponent<PooledObject>().IsRented);
        enemy.Despawn(); director.Tick(.01f); Assert.That(director.SuccessfulSpawns,Is.EqualTo(1));
    }
    [Test] public void PoolUnavailableConsumesAttemptAndDoesNotQueue()
    {
        Director(); pool.enabled=false; director.Tick(.6f); Assert.That(director.PoolFailedAttempts,Is.EqualTo(1));
        pool.enabled=true; director.Tick(.001f); Assert.That(director.AttemptedSpawns,Is.EqualTo(1)); Assert.That(director.SuccessfulSpawns,Is.Zero);
    }
    [Test] public void ClearAndOwnerDisableReturnUncollectedRewardsWithoutPayment()
    {
        Spawn().GetComponent<EnemyHealth>().TakeDamage(3); Assert.That(Managers.Pool.GetActiveCount(pickupPrefab),Is.EqualTo(1));
        pool.enabled=false; Assert.That(Managers.Pool.GetActiveCount(pickupPrefab),Is.Zero); Assert.That(player.CurrentEXP,Is.Zero);
        pool.enabled=true; Spawn(); Managers.Pool.Clear(); Assert.That(pool.ActiveEnemyCount,Is.Zero); Spawn(); Assert.That(pool.ActiveEnemyCount,Is.EqualTo(1));
    }
    [Test] public void OldEnemyLeaseCannotReturnRerentedEnemy()
    {
        var first=Spawn(); var item=first.GetComponent<PooledObject>(); uint lease=item.LeaseVersion; pool.ReturnEnemy(first); var second=Spawn();
        Assert.That(second,Is.SameAs(first)); Assert.That(item.ReturnToPool(lease),Is.False); Assert.That(pool.ActiveEnemyCount,Is.EqualTo(1));
    }
    [Test] public void DeathListenerRerentDoesNotLetOldDeathDespawnNewGeneration()
    {
        var first=Spawn(); var health=first.GetComponent<EnemyHealth>(); GameObject rerented=null;
        health.Died+=()=>{ pool.ReturnEnemy(first); rerented=Spawn(); };
        health.TakeDamage(3);
        Assert.That(rerented,Is.SameAs(first)); Assert.That(pool.ActiveEnemyCount,Is.EqualTo(1));
        Assert.That(health.IsDead,Is.False); Assert.That(health.GetComponent<PooledObject>().IsRented,Is.True);
    }
    [Test] public void CollectionUsesCurrentBonusButKeepsDroppedRawAndColor()
    {
        Director(0); typeof(PlayerData).GetMethod("BindSurvivalDirector",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,null);
        director.Tick(179.99f); pool.TryDropExperience(pickupPrefab,Vector3.zero,player.transform,2); var first=RentedPickup();
        var color=first.GetComponent<SpriteRenderer>().color; director.Tick(.02f); Assert.That(player.CatchupActive,Is.True);
        Assert.That(first.RawExperience,Is.EqualTo(2)); Assert.That(first.GetComponent<SpriteRenderer>().color,Is.EqualTo(color));
        Collect(first,player); Assert.That(player.CurrentEXP,Is.EqualTo(2)); Assert.That(player.ExperienceFractionalCarry,Is.EqualTo(.5));
        pool.TryDropExperience(pickupPrefab,Vector3.zero,player.transform,2); Collect(RentedPickup(),player);
        Assert.That(player.CurrentEXP,Is.EqualTo(5)); Assert.That(player.ExperienceFractionalCarry,Is.Zero);
    }
    [Test] public void Full1080SecondClockSimulationHas36SamplesAnd8100ScheduledAttempts()
    {
        Director(); int samples=0; int scheduled=0; director.Sampled+=sample=>{samples++; scheduled+=sample.attempted+sample.frameSkipped; Assert.That(sample.peakActive,Is.LessThanOrEqualTo(130));};
        for(int i=0;i<21600;i++) director.Tick(.05f);
        Assert.That(director.ElapsedTime,Is.EqualTo(1080)); Assert.That(samples,Is.EqualTo(36)); Assert.That(scheduled,Is.EqualTo(8100));
        Assert.That(director.SuccessfulSpawns,Is.EqualTo(130)); Assert.That(director.LastSample.targetAttempts,Is.EqualTo(400));
        Assert.That(director.LastSample.ToCsv().Split(',').Length,Is.EqualTo(SurvivalWaveMetrics.CsvHeader.Split(',').Length));
    }
    [UnityTest] public IEnumerator RealPhysicsTriggerCollectsPooledRewardOnlyOnce()
    {
        var body=playerObject.AddComponent<Rigidbody2D>(); body.gravityScale=0; playerObject.AddComponent<CircleCollider2D>();
        Spawn().GetComponent<EnemyHealth>().TakeDamage(3);
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(player.CurrentEXP,Is.EqualTo(2)); Assert.That(Managers.Pool.GetActiveCount(pickupPrefab),Is.Zero);
    }
}
