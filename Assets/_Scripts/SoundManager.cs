using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volumes")]
    [Range(0f, 1f)] public float bgmVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;

    // 옵션 저장 키
    private const string KEY_BGM = "OPT_BGM";
    private const string KEY_SFX = "OPT_SFX";

    // ====== SFX Library ======
    public enum SfxId
    {
        UIClick,
        UIBack,

        PlayerAttack,
        PlayerDash,
        PlayerHit,

        BubblePop,
        BonusPickup,
        CoinPickup,

        EnemyHit,
        EnemyDie,
    }

    [Serializable]
    public struct SfxEntry
    {
        public SfxId id;
        public AudioClip clip;
        [Range(0f, 2f)] public float volumeScale; // 1=기본, 더 크게/작게
    }

    [Header("SFX Clips (여기에 전부 등록)")]
    [SerializeField] private SfxEntry[] sfxEntries;

    private Dictionary<SfxId, SfxEntry> sfxMap;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        AutoAssignSourcesIfNeeded();
        BuildSfxMap();
        ApplySavedOptions();
    }

    private void AutoAssignSourcesIfNeeded()
    {
        if (bgmSource == null || sfxSource == null)
        {
            var sources = GetComponents<AudioSource>();
            if (sources.Length >= 2)
            {
                if (bgmSource == null) bgmSource = sources[0];
                if (sfxSource == null) sfxSource = sources[1];
            }
        }

        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    private void BuildSfxMap()
    {
        sfxMap = new Dictionary<SfxId, SfxEntry>();

        if (sfxEntries == null) return;

        for (int i = 0; i < sfxEntries.Length; i++)
        {
            var e = sfxEntries[i];
            if (e.clip == null) continue;

            // 중복 id 방지: 마지막 값을 우선(원하면 경고 로그로 바꿔도 됨)
            sfxMap[e.id] = e;
        }
    }

    private void ApplySavedOptions()
    {
        int bgmOn = PlayerPrefs.GetInt(KEY_BGM, 1);
        int sfxOn = PlayerPrefs.GetInt(KEY_SFX, 1);

        bgmSource.volume = bgmVolume;
        sfxSource.volume = sfxVolume;

        bgmSource.mute = (bgmOn == 0);
        sfxSource.mute = (sfxOn == 0);
    }

    // ===== 옵션 토글 =====
    public void SetBgmMute(bool mute)
    {
        bgmSource.mute = mute;
        PlayerPrefs.SetInt(KEY_BGM, mute ? 0 : 1);
    }

    public void SetSfxMute(bool mute)
    {
        sfxSource.mute = mute;
        PlayerPrefs.SetInt(KEY_SFX, mute ? 0 : 1);
    }

    // ===== BGM =====
    public void PlayBGM(AudioClip clip, bool restartIfSame = false)
    {
        if (clip == null) return;
        if (!restartIfSame && bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    // ===== SFX: ID로 재생 =====
    public void PlaySfx(SfxId id, float extraVolumeScale = 1f)
    {
        if (sfxSource == null) return;
        if (sfxMap == null) BuildSfxMap();

        if (!sfxMap.TryGetValue(id, out var e) || e.clip == null) return;

        float vol = Mathf.Clamp01(sfxVolume * e.volumeScale * extraVolumeScale);
        sfxSource.PlayOneShot(e.clip, vol);
    }

    // 필요하면 직접 clip도 재생 가능하게
    public void PlaySfxClip(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxSource == null || clip == null) return;
        float vol = Mathf.Clamp01(sfxVolume * volumeScale);
        sfxSource.PlayOneShot(clip, vol);
    }
}
