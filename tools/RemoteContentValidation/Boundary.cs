// 격리 검증에서 원격 준비와 무관한 서비스만 경계로 대체한다.
// Managers/ResourceManager의 준비 코루틴은 원본 파일 그대로 컴파일한다.
using UnityEngine;
public enum SkillUpgradeKind { None = 0, Projectiles = 1, Range = 2, Hits = 3, Angle = 4 }
// 이번 검증에서 호출하지 않는 강화 설명용 경계다. TryFrom의 실제 형상 변환 코드는 원본을 사용한다.
public static class PlayerLoadout { public static float DamageMultiplierForStack(int stack) => 1f; }
public static class Define
{
    public enum ELanguage { Korean, English, French, SimplifiedChinese, TraditionalChinese, Japanese }
    public enum EScene { Unknown, TitleScene, LoadScene, LobbyScene, MainScene }
}
public sealed class DataManager { public bool IsInitialized { get; private set; } public void Init() { IsInitialized = true; } }
public sealed class SceneManagerEx
{
    public void Clear() { }
    public Define.EScene ConsumeLoadingDestination(Define.EScene fallback) => fallback;
    public AsyncOperation LoadSceneAsync(Define.EScene scene) => null;
}
public sealed class SoundManager { public void Init(Transform parent) { } public void Clear() { } }
public sealed class UIManager { public void Clear() { } }
public static class AndroidCapturePermit { public static void EnsureOn(GameObject go) { } }
public static class GameScreenshot { public static void EnsureOn(GameObject go) { } }
public sealed class UiFontScaleBootstrap : MonoBehaviour { }
public abstract class BaseScene : MonoBehaviour
{
    public Define.EScene SceneType { get; protected set; }
    public virtual bool Init() => true;
    public abstract void Clear();
}
public class UI_Scene : MonoBehaviour
{
    public virtual bool Init() => true;
    protected void BindObjects(System.Type type) { }
    protected GameObject GetObject(int index) => transform.Find("Spinner")?.gameObject;
}
