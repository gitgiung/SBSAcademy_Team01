using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}

public class DataManager
{
    public bool IsInitialized { get; private set; }
   
	public Dictionary<int, Data.SkillData> SkillDic { get; private set; } = new Dictionary<int, Data.SkillData>();
	
    public void Init()
    {
        IsInitialized = false;
        try
        {
            Data.SkillDataLoader loader = LoadJson<Data.SkillDataLoader, int, Data.SkillData>("SkillData");
            if (loader == null || loader.skills == null || loader.skills.Count == 0)
                throw new InvalidOperationException("스킬 목록이 비어 있습니다.");
            foreach (Data.SkillData skill in loader.skills)
                if (skill == null)
                    throw new InvalidOperationException("스킬 목록에 null 항목이 있습니다.");

            // 전체 검증에 성공한 사전만 게시합니다.
            Dictionary<int, Data.SkillData> skills = loader.MakeDict();
            SkillDic = skills;
            IsInitialized = true;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"SkillData 초기화 실패: {exception.Message}", exception);
        }
    }

    private Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    {
        TextAsset textAsset = Managers.Resource.Load<TextAsset>(path);
        if (textAsset == null)
            throw new InvalidOperationException($"필수 데이터 {path}가 로드되지 않았습니다.");
        return JsonConvert.DeserializeObject<Loader>(textAsset.text);
    }
}
