using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>全局音频管理器：BGM与UI音效统一入口（BGM跨场景不中断）</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Serializable]
    public class SceneBgm
    {
        [Tooltip("场景名，必须和Build Settings里的完全一致")]
        public string sceneName;
        public AudioClip bgmClip;
    }

    [Header("各场景BGM：场景加载时自动播放")]
    public List<SceneBgm> sceneBgmList = new List<SceneBgm>();

    [Header("通用点击音效")]
    public AudioClip clickClip;

    [Header("音量(0~1,设置界面改这两个)")]
    [Range(0, 1)] public float musicVolume = 0.5f;
    [Range(0, 1)] public float sfxVolume = 0.8f;

    [Header("BGM切换淡入淡出秒数")]
    public float fadeTime = 1f;

    private AudioSource musicSource; //背景音乐专用(循环)
    private AudioSource sfxSource;   //音效专用(PlayOneShot叠加)
    private bool musicOff;           //BGM被手动关闭中（StopMusic后为true）

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); //跨场景BGM不断

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
    }

    private void Start()
    {
        // 第一个场景(AudioManager所在的场景)不会触发sceneLoaded，这里补播一次
        PlaySceneBgm(SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; //每次加载场景自动查表换BGM
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneBgm(scene.name);
    }

    private void PlaySceneBgm(string sceneName)
    {
        foreach (var m in sceneBgmList)
        {
            if (m.sceneName == sceneName)
            {
                PlayMusic(m.bgmClip); //同曲自动忽略，换曲自动淡入淡出
                return;
            }
        }
        // 没配映射的场景：保持当前BGM继续放，不算错误
    }

    /// <summary>切换背景音乐：自动淡出旧的、淡入新的（同曲忽略）</summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && !musicOff) return; //同曲且未被手动关闭→忽略
        StopAllCoroutines();
        musicOff = false;                                  //任何PlayMusic都会重新开启BGM
        StartCoroutine(SwitchMusic(clip));
    }

    private IEnumerator SwitchMusic(AudioClip clip)
    {
        if (musicSource.isPlaying)
            yield return Fade(musicSource, 0f);      //淡出旧曲
        musicSource.clip = clip;
        musicSource.Play();
        yield return Fade(musicSource, musicVolume); //淡入新曲
    }

    private IEnumerator Fade(AudioSource source, float target)//淡入淡出
    {
        while (!Mathf.Approximately(source.volume, target))
        {
            source.volume = Mathf.MoveTowards(source.volume, target, Time.deltaTime / fadeTime);
            yield return null;
        }
    }

    /// <summary>关闭背景音乐：淡出到0后停止（之后任何PlayMusic/切场景配了BGM都会重新淡入开启）</summary>
    public void StopMusic()
    {
        StopAllCoroutines();
        StartCoroutine(StopMusicFade());
    }

    /// <summary>重新开启背景音乐：对当前曲子淡入（当前没有曲子则无效果）</summary>
    public void ResumeMusic()
    {
        if (musicSource.clip == null) return;
        StopAllCoroutines();
        musicOff = false;
        if (!musicSource.isPlaying) musicSource.Play();
        StartCoroutine(Fade(musicSource, musicVolume));
    }

    /// <summary>一键切换：关↔开，都带淡入淡出（返回切换后是否开启）</summary>
    public bool ToggleMusic()
    {
        if (musicOff) ResumeMusic(); else StopMusic();
        return !musicOff;
    }

    private IEnumerator StopMusicFade()
    {
        musicOff = true;
        yield return Fade(musicSource, 0f); //淡出到0
        musicSource.Stop();
    }


    /// <summary>播放点击音效（可拖进任意按钮的OnClick）</summary>
    public void PlayClick()
    {
        if (clickClip != null) sfxSource.PlayOneShot(clickClip);
    }

    /// <summary>播放任意一次性音效</summary>
    public void PlaySfx(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
    }

    /// <summary>设置界面调用：音乐音量</summary>
    public void SetMusicVolume(float v) { musicVolume = v; musicSource.volume = v; }

    /// <summary>设置界面调用：音效音量</summary>
    public void SetSfxVolume(float v) { sfxVolume = v; sfxSource.volume = v; }
}
