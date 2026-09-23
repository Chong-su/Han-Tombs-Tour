using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 开始场景的"开始游戏"按钮
/// 挂在按钮上，点击后调用 GameFlowManager.StartGame()
/// </summary>
[RequireComponent(typeof(Button))]
public class StartSceneButton : MonoBehaviour
{
    private Button startButton;
    private bool hasClicked = false;
    public VideoPlayer video;//UI背景视频
    public AudioClip clip;
    
    private void Start()
    {
        startButton = GetComponent<Button>();
        startButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        /*        if (hasClicked) return;
                hasClicked = true;*/
        AudioManager.Instance.PlayClick();
        StartCoroutine(WaitScends());
        

    }
    IEnumerator Startgame()
    {
        yield return new WaitForSeconds(1f);
        GameFlowManager.Instance.StartGame();
        
        startButton.interactable = false; // 恢复按钮可点击状态
        
    }

    IEnumerator WaitScends()//按开始键后等待一秒
    {
        
        if (GameFlowManager.Instance != null && hasClicked == false)
        {
            yield return new WaitForSeconds(1.5f);
            EventCenter.Trigger("HeiPingShousuo");
            //yield return new WaitForSeconds(1f);   
           // StartCoroutine(JiaZaiHeiPing());
            video.Stop();
            StartCoroutine(Startgame());//提示弹窗点击后
            hasClicked = true;
        }
        
    }
    IEnumerator JiaZaiHeiPing()
    {
        yield return new WaitForSeconds(1f);
        GameFlowManager.Instance.JiaZaiUI.gameObject.SetActive(true);
    }
    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(OnClick);
    }
}