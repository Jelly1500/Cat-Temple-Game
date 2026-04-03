using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    private AudioSource[] _audioSources = new AudioSource[(int)Define.ESound.MaxCount];
    private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

    private Transform _soundRoot;
    public Transform SoundRoot { get { return Utils.GetRootTransform(ref _soundRoot, "@SoundRoot"); } }

    void Awake()
    {
        Init();
    }

    public void Init()
    {
        string[] soundTypeNames = System.Enum.GetNames(typeof(Define.ESound));
        for (int i = 0; i < soundTypeNames.Length - 1; i++)
        {
            GameObject go = new GameObject { name = soundTypeNames[i] };
            _audioSources[i] = go.AddComponent<AudioSource>();
            go.transform.SetParent(SoundRoot);
        }

        _audioSources[(int)Define.ESound.Bgm].loop = true;
    }

    private AudioClip GetAudioClip(string key)
    {
        if (_audioClips.ContainsKey(key) == false)
        {
            AudioClip audioClip = ResourceManager.Instance.Get<AudioClip>(key);
            _audioClips.Add(key, audioClip);
        }

        return _audioClips[key];
    }

    public void Play2D(Define.ESound type, string key, float pitch = 1.0f)
    {
        AudioClip audioClip = GetAudioClip(key);
        Play2D(type, audioClip, pitch);
    }

    public void Play2D(Define.ESound type, AudioClip audioClip, float pitch = 1.0f)
    {
        AudioSource audioSource = _audioSources[(int)type];

        if (type == Define.ESound.Bgm)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();

            audioSource.pitch = pitch;
            audioSource.clip = audioClip;
            audioSource.Play();
        }
        else
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(audioClip);
        }
    }

    public void Play3D(string key, GameObject soundObject, float minDistance = 1.0f, float maxDistance = 20.0f, float pitch = 1.0f)
    {
        AudioClip audioClip = GetAudioClip(key);
        Play3D(audioClip, soundObject, minDistance, maxDistance, pitch);
    }

    public void Play3D(AudioClip audioClip, GameObject soundObject, float minDistance = 1.0f, float maxDistance = 20.0f, float pitch = 1.0f)
    {
        AudioSource audioSource = soundObject.GetOrAddComponent<AudioSource>();

        audioSource.clip = audioClip;
        audioSource.spatialBlend = 1.0f;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.pitch = pitch;
        audioSource.Play();
    }

    public void Stop(Define.ESound type)
    {
        AudioSource audioSource = _audioSources[(int)type];
        audioSource.Stop();
    }

    public void Clear()
    {
        foreach (AudioSource audioSource in _audioSources)
            audioSource.Stop();

        _audioClips.Clear();
    }

    public void SetVolume(Define.ESound type, float volume)
    {
        if (_audioSources[(int)type] != null)
            _audioSources[(int)type].volume = volume;
    }

    public float GetVolume(Define.ESound type)
    {
        if (_audioSources[(int)type] != null)
            return _audioSources[(int)type].volume;
        return 1f;
    }

    public void PlayBGMWithFade(string key, float fadeDuration = 2.0f)
    {
        AudioClip nextClip = GetAudioClip(key);
        if (nextClip == null) return;

        AudioSource bgmSource = _audioSources[(int)Define.ESound.Bgm];

        // 이미 같은 곡이 재생 중이면 무시
        if (bgmSource.clip == nextClip && bgmSource.isPlaying) return;

        StartCoroutine(CoCrossFadeBGM(bgmSource, nextClip, fadeDuration));
    }

    private IEnumerator CoCrossFadeBGM(AudioSource source, AudioClip nextClip, float duration)
    {
        float startVolume = source.volume;

        // 1. Fade Out (현재 곡 볼륨 줄이기)
        if (source.isPlaying)
        {
            float timer = 0f;
            while (timer < duration / 2)
            {
                timer += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, timer / (duration / 2));
                yield return null;
            }
        }

        source.Stop();
        source.clip = nextClip;
        source.Play();

        // 2. Fade In (새 곡 볼륨 키우기)
        float timerIn = 0f;
        while (timerIn < duration / 2)
        {
            timerIn += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, 1f, timerIn / (duration / 2)); // 목표 볼륨이 1f가 아니라면 startVolume 사용
            yield return null;
        }

        source.volume = 1f; // 최종 볼륨 보정
    }

    public bool IsBgmPlaying()
    {
        if (_audioSources[(int)Define.ESound.Bgm] != null)
            return _audioSources[(int)Define.ESound.Bgm].isPlaying;
        return false;
    }

    // BGM 반복 재생 여부를 설정하는 함수 (순차 재생을 위해 필요)
    public void SetBgmLoop(bool isLoop)
    {
        if (_audioSources[(int)Define.ESound.Bgm] != null)
            _audioSources[(int)Define.ESound.Bgm].loop = isLoop;
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    // [신규] 현재 마스터 볼륨 가져오기
    public float GetMasterVolume()
    {
        return AudioListener.volume;
    }
}
