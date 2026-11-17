using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

public partial class ResourceManager
{
    private bool skillPilotRequired;
    private bool skillPilotReady;

    public T LoadGameplayAsset<T>(string key) where T : Object
    {
        string address = SkillPilotContent.Address(key);
        if (address != null && skillPilotRequired) return Load<T>(address);
        return LoadLocalGameplayAsset<T>(key);
    }

    internal static T LoadLocalGameplayAsset<T>(string key) where T : Object
    {
        string address = SkillPilotContent.Address(key);
        if (address == null) return Resources.Load<T>(key);
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(SkillPilotContent.AssetPath(address));
#else
        var local = Resources.Load<SkillPilotLocalAssets>(SkillPilotContent.LocalHolderKey);
        return local != null ? local.Load<T>(address) : null;
#endif
    }

    public IEnumerator PrepareSkillPilotAsync(Action<bool, string> completed)
    {
        if (!ReferenceEquals(Managers.CurrentResource, this))
        {
            InvokeSafely(() => completed?.Invoke(false, "스킬 준비는 현재 Managers의 Resource에서 실행해야 합니다."));
            yield break;
        }
        if (skillPilotReady) { InvokeSafely(() => completed?.Invoke(true, null)); yield break; }
        int generation = _generation;
        skillPilotRequired = true;
        CombatSkillTable.ClearCached();
        LightSkillVisuals.ClearBaked("WaterArrow");
        string error = null;
        // requiredLabel이 일부 번들을 누락했어도 메모리 로딩이 동의 없는 네트워크 전송을 시작하면 안 된다.
        foreach (string address in new[] { SkillPilotContent.TableAddress, SkillPilotContent.TextureAddress, SkillPilotContent.FramesAddress, SkillPilotContent.IconAddress })
        {
            long bytes = -1;
            yield return GetDownloadSizeAsync(address, (count, reason) => { bytes = count; error = reason; });
            if (error == null && bytes != 0) error = "필수 스킬 에셋이 다운로드 대상에 포함되지 않았습니다: " + address;
            if (error != null || generation != _generation) break;
        }
        if (generation != _generation) error = "스킬 준비 중 리소스가 정리되었습니다.";
        // 동일 PNG의 Sprite/Texture 자동 선택과 섞지 않고 명시한 타입만 요청한다.
        if (error == null) yield return LoadPilotAsset<TextAsset>(SkillPilotContent.TableAddress, generation, reason => error = reason);
        if (error == null) yield return LoadPilotAsset<Texture2D>(SkillPilotContent.TextureAddress, generation, reason => error = reason);
        if (error == null) yield return LoadPilotAsset<TextAsset>(SkillPilotContent.FramesAddress, generation, reason => error = reason);
        if (error == null) yield return LoadPilotAsset<Sprite>(SkillPilotContent.IconAddress, generation, reason => error = reason);
        if (generation != _generation) error = "스킬 준비 중 리소스가 정리되었습니다.";
        if (error == null && !LightSkillVisuals.TryPrepareBaked("WaterArrow", out string visualError)) error = visualError;
        if (error == null && !CombatSkillTable.TryInstall(Load<TextAsset>(SkillPilotContent.TableAddress), out string tableError)) error = tableError;
        if (error == null && !CombatSkillTable.TryGet(10025, out _)) error = "스킬 표에 수살(10025)이 없습니다.";
        if (error == null) skillPilotReady = true;
        else if (generation == _generation)
        {
            CombatSkillTable.ClearCached();
            LightSkillVisuals.ClearBaked("WaterArrow");
        }
        InvokeSafely(() => completed?.Invoke(error == null, error));
    }

    private IEnumerator LoadPilotAsset<T>(string address, int generation, Action<string> completed) where T : Object
    {
        if (generation != _generation) { completed("스킬 준비 중 리소스가 정리되었습니다."); yield break; }
        LoadBatch batch = BeginBatch(address, typeof(T), null, null);
        while (!batch.Finished) yield return null;
        string error = generation == _generation ? batch.Error : "스킬 준비 중 리소스가 정리되었습니다.";
        if (error == null && Load<T>(address) == null) error = "필수 스킬 에셋이 없습니다: " + address;
        completed(error);
    }

    private void ClearSkillPilot()
    {
        if (!skillPilotRequired) return;
        skillPilotReady = false;
        CombatSkillTable.ClearCached();
        LightSkillVisuals.ClearBaked("WaterArrow");
        // 같은 실행에서 다시 준비할 때도 원격 누락을 로컬 대체로 감추지 않는다.
    }
}

public partial class Managers
{
    internal static ResourceManager CurrentResource => s_instance != null ? s_instance._resource : null;
    public static T LoadGameplayAsset<T>(string key) where T : Object
        => CurrentResource != null ? CurrentResource.LoadGameplayAsset<T>(key) : ResourceManager.LoadLocalGameplayAsset<T>(key);
}
