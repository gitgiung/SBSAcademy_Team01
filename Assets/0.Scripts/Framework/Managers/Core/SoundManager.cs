using UnityEngine;

public class SoundManager
{
    private AudioSource[] _audioSources = new AudioSource[(int)Define.ESound.Max];
    private GameObject _soundRoot = null;
    private Transform soundParent;

    public void Init()
    {
        Init(soundParent);
    }

    public void Init(Transform parent)
    {
        if (parent != null)
            soundParent = parent;
        else
            parent = soundParent;

        if (_soundRoot == null)
        {
            _soundRoot = parent != null ? parent.Find("@SoundRoot")?.gameObject : null;
            if (_soundRoot == null)
                _soundRoot = GameObject.Find("@SoundRoot");
            if (_soundRoot == null)
                _soundRoot = new GameObject { name = "@SoundRoot" };
        }

        if (parent != null)
            _soundRoot.transform.SetParent(parent, false);
        else if (_soundRoot.transform.parent == null)
            UnityEngine.Object.DontDestroyOnLoad(_soundRoot);

        for (int index = 0; index < _audioSources.Length; index++)
        {
            string sourceName = ((Define.ESound)index).ToString();
            Transform child = _soundRoot.transform.Find(sourceName);
            if (child == null)
            {
                child = new GameObject(sourceName).transform;
                child.SetParent(_soundRoot.transform, false);
            }

            _audioSources[index] = Util.GetOrAddComponent<AudioSource>(child.gameObject);
            _audioSources[index].playOnAwake = false;
        }

        _audioSources[(int)Define.ESound.Bgm].loop = true;
    }

    /// <summary>재생 상태를 정리하고 ResourceManager 소유 에셋은 유지합니다.</summary>
    public void Clear()
    {
        foreach (AudioSource source in _audioSources)
        {
            if (source == null) continue;
            source.Stop();
            source.clip = null;
            source.pitch = 1f;
        }
    }

    private AudioSource GetSource(Define.ESound type)
    {
        int index = (int)type;
        if (index < 0 || index >= _audioSources.Length)
        {
            Debug.LogWarning($"Invalid sound type: {type}.");
            return null;
        }
        if (_soundRoot == null || _audioSources[index] == null)
            Init();
        return _audioSources[index];
    }

    public void Play(Define.ESound type)
    {
        AudioSource source = GetSource(type);
        if (source != null && source.clip != null)
            source.Play();
    }

    public void Play(Define.ESound type, string key, float pitch = 1.0f)
    {
        // ResourceManager owns the cache. Never retain null or a stale clip locally.
        AudioClip clip = string.IsNullOrEmpty(key) ? null : Managers.Resource.Load<AudioClip>(key);
        if (clip == null)
        {
            Debug.LogWarning($"Sound clip not found: {key}.");
            return;
        }
        Play(type, clip, pitch);
    }

    public void Play(Define.ESound type, AudioClip audioClip, float pitch = 1.0f)
    {
        if (audioClip == null)
        {
            Debug.LogWarning("Cannot play a null sound clip.");
            return;
        }
        AudioSource source = GetSource(type);
        if (source == null) return;
        source.pitch = pitch;
        if (type == Define.ESound.Bgm)
        {
            source.Stop();
            source.clip = audioClip;
            source.Play();
        }
        else
        {
            source.PlayOneShot(audioClip);
        }
    }

    public void Stop(Define.ESound type)
    {
        AudioSource source = GetSource(type);
        if (source != null) source.Stop();
    }
}
