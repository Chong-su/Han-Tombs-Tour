using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 全局游戏流程管理器 - 管理视频播放与场景切换
/// 单例 + DontDestroyOnLoad，跨场景不销毁
/// 通过 EventCenter 与任务系统/机关系统联动
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; } // 单例

    [Header("流程配置文件")]
    [SerializeField] private GameFlowConfigSO flowConfig;

    [Header("视频播放组件")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject videoCanvas;      // 全屏Canvas（含RawImage）
    [SerializeField] private RawImage videoRawImage;      // 视频画面显示
    [Header("开始提示弹窗")]
    [SerializeField] private GameObject tipCanvas;      // 提示弹窗Canvas
    [Header("开始字幕窗口")]
    [SerializeField] private GameObject subtitleCanvas; // 字幕窗口
    [SerializeField] private VideoPlayer subVideo; // 字幕控制器
    [Header("开始提示字幕文本")]
    [SerializeField][TextArea] private string tipPanelText;      // 提示字幕

    public GameObject JiaZaiUI; //视频加载界面

    private bool isPlayingVideo = false;
    private bool isStartVideoEnd = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 跨场景不销毁

        if (videoCanvas != null)
            videoCanvas.SetActive(false);
    }

    private void OnEnable()
    {
        EventCenter.AddListener<int>("OnJiGuanTriggered", OnJiGuanTriggeredHandler);
    }

    private void OnDisable()
    {
        EventCenter.RemoveListener<int>("OnJiGuanTriggered", OnJiGuanTriggeredHandler);
    }

    /// <summary>
    /// 开始游戏（开始场景的按钮调用）
    /// </summary>
    public void StartGame()
    {
        if (flowConfig == null || flowConfig.introVideo == null)
        {
            Debug.LogWarning("开场视频未配置，直接加载场景");
            if (flowConfig != null && !string.IsNullOrEmpty(flowConfig.introTargetScene))
                SceneManager.LoadScene(flowConfig.introTargetScene);
            return;
        }

        PlayVideoThenLoadScene(flowConfig.introVideo, flowConfig.introTargetScene);
    }

    /// <summary>
    /// 播放视频，结束后加载指定场景
    /// </summary>
    public void PlayVideoThenLoadScene(VideoClip clip, string targetScene)
    {
        if (isPlayingVideo) return; // 防止重复播放

        isPlayingVideo = true;
        videoCanvas.SetActive(true);

        videoPlayer.clip = clip; // 设置视频
        videoPlayer.targetTexture = null; // 清空目标纹理

        // 准备视频
        videoPlayer.Prepare();//异步执行，不会阻塞主线程

        // 使用协程等待视频准备完成
        StartCoroutine(PlayVideoCoroutine(targetScene));
    }

    private IEnumerator PlayVideoCoroutine(string targetScene)
    {
        JiaZaiUI.SetActive(true);
        // 等待视频准备完成
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }
        if(JiaZaiUI != null)
        JiaZaiUI.SetActive(false);
        
        // 设置 RawImage 纹理
        if (videoRawImage != null)
        {
            videoRawImage.texture = videoPlayer.texture;
        }
        EventCenter.Trigger("HeiPingZhankai");
        // 开始播放
        videoPlayer.Play();
        AudioManager.Instance.StopMusic();
        Debug.Log($"开始播放视频: {videoPlayer.clip?.name}");

        // 等待视频播放结束
        while (videoPlayer.isPlaying)
        {
            // 按ESC跳过视频
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                videoPlayer.Stop();
                break;
            }
            yield return null;
        }

        Debug.Log("视频播放结束");

        isPlayingVideo = false;

        // 显示提示弹窗
        if(isStartVideoEnd == false)
        {
            StartCoroutine(StartLoadSceneWithVideoCover(targetScene));
        }
        else
        {
            StartCoroutine(LoadSceneWithVideoCover(targetScene));
        }
        // 加载目标场景，视频UI保持覆盖直到场景加载完成
        /*        if (!string.IsNullOrEmpty(targetScene))
                {
                    Debug.Log($"加载场景: {targetScene}");

                }*/
    }//用于开始场景视频播放

    public void LoadeNewScene() //提示弹窗出现按Yes按钮后调用
    {
        tipCanvas.SetActive(false);
        subtitleCanvas.SetActive(true);
        subVideo.Prepare();//准备开幕视频
        // 异步加载场景
        StartCoroutine(LoadNewsenceAndText(flowConfig.introTargetScene));
       
        isStartVideoEnd = true;
    }
    /// <summary>
    /// 异步加载场景，视频画面保持显示直到场景加载完成后才关闭
    /// </summary>
    private IEnumerator StartLoadSceneWithVideoCover(string targetScene)//开始场景视频播放结束后加载新场景
    {
        // 视频暂停在最后一帧，保持画面覆盖
        videoPlayer.Pause();
        // 额外等一帧确保新场景渲染完毕
        yield return new WaitForSeconds(0.1f);

        // 关闭视频UI
        videoCanvas.SetActive(false);
        videoPlayer.Stop();
        //Debug.Log("场景加载完成，视频UI已关闭");
        tipCanvas.SetActive(true);//打开提示弹窗
        AudioManager.Instance.ResumeMusic();
        // 通知 SceneTransition 开始扩散转场
        // EventCenter.Trigger("HeiPingZhankai");
    }

    private IEnumerator LoadSceneWithVideoCover(string targetScene)//用于游戏中视频播放结束后加载新场景
    {
        // 视频暂停在最后一帧，保持画面覆盖
        videoPlayer.Pause();
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
/*        // 额外等一帧确保新场景渲染完毕
        yield return null;*/

        // 关闭视频UI
        videoCanvas.SetActive(false);
        videoPlayer.Stop();
        //Debug.Log("场景加载完成，视频UI已关闭");

        EventCenter.Trigger("HeiPingZhankai");
        // 通知 SceneTransition 开始扩散转场
        // EventCenter.Trigger("HeiPingZhankai");
    }
    private IEnumerator LoadNewsenceAndText(string targetScene)
    {

        while (!subVideo.isPrepared)
        {
            yield return null;
        }
        subVideo.Play();
        while (subVideo.isPlaying)
        {
            yield return null;
        }
        
        EventCenter.Trigger("ShowSubtitle", tipPanelText);
        var subtitleController = subtitleCanvas.GetComponent<SubtitleController>();
        
        while (subtitleController.TextOver != true)
        {
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EventCenter.Trigger("SkipTyping");
            }
            yield return null;
        }
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        // 等待场景完全加载
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        JiaZaiUI.SetActive(false);
        EventCenter.Trigger("HeiPingZhankai");       
    }


    
    

/// <summary>
/// 机关触发事件回调：查配置表，播放对应视频并跳转
/// </summary>
private void OnJiGuanTriggeredHandler(int jiguanId)
{
    if (flowConfig == null || flowConfig.jiguanVideoMappings == null)
        return;

    foreach (var mapping in flowConfig.jiguanVideoMappings)
    {
        if (mapping.jiguanId == jiguanId)
        {
            Debug.Log($"机关[{jiguanId}]触发，播放视频: {mapping.videoClip?.name}");
            PlayVideoThenLoadScene(mapping.videoClip, mapping.targetScene);
            return;
        }
    }

    Debug.Log($"机关[{jiguanId}]未在流程配置中找到对应视频，忽略");
}
}
