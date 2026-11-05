// 격리된 Unity 검증 프로젝트에서만 사용합니다. Player 전투/리소스/사운드/승패 의존성을 대체합니다.
using System;
using System.Collections.Generic;
using UnityEngine;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("GrowthValidation.Tests")]
public class PlayerDefenseVfx : MonoBehaviour
{
    public void Initialize(Transform owner, SpriteRenderer renderer) { }
    public void PlayParry() { }
    public void SetGuard(bool guarding) { }
    public void ResetVfx() { }
}
public class Player : MonoBehaviour { public PlayerData data; public void EndParryHitStop() { } }
public class BossController : MonoBehaviour { }
public class HordeLordBoss : MonoBehaviour { public EnemyHealth Health => GetComponent<EnemyHealth>(); }
public enum GameState { Playing, Victory, Defeat }
public class GameManager : MonoBehaviour
{
    public GameState state = GameState.Playing;
    public event Action<GameState> StateChanged;
    public void Finish(GameState next) { state=next; Time.timeScale=0; StateChanged?.Invoke(next); }
}
public class UI_Base : InitBase { }
public class UI_Scene : UI_Base { }
public static class Util
{
    public static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T component=go.GetComponent<T>();
        return component!=null ? component : go.AddComponent<T>();
    }
}
namespace Unity.VisualScripting
{
    public static class Extensions { public static T GetOrAddComponent<T>(this GameObject go) where T : Component => Util.GetOrAddComponent<T>(go); }
}
public static class Define { public enum EEffectSize { } public enum ESound { Bgm, Effect } }
public interface ILoader<TKey,TValue> { Dictionary<TKey,TValue> MakeDict(); }
public sealed class ResourceBoundary
{
    public T Load<T>(string key) where T : UnityEngine.Object => null;
    public GameObject Instantiate(string key, Transform parent=null, bool pooling=true) => null;
}
public sealed class SoundBoundary
{
    public float MasterVolume { get; set; }
    public float GetVolume(Define.ESound sound) => 1;
    public void SetVolume(Define.ESound sound, float value) { }
}
public static class Managers
{
    public static PoolManager Pool;
    public static UIManager UI = new UIManager();
    public static ResourceBoundary Resource = new ResourceBoundary();
    public static SoundBoundary Sound = new SoundBoundary();
}
