using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 字幕控制器：逐字显示文字，支持事件触发和直接调用
/// </summary>
public class SubtitleController : MonoBehaviour
{
    public string kg = "\u3000";//空格
    [Header("UI引用")]
    public Text subtitleText;          // 显示文字的 Text 组件
    public GameObject subtitlePanel;    // 字幕背景面板（可选）
    

    [Header("打字设置")]
    public float charInterval = 0.05f;  // 每个字出现的间隔（秒）
    public float holdTime = 2f;         // 全部显示完后停留时间（秒）
    public float fadeOutTime = 0.5f;    // 淡出时间（秒）

    [Header("要出现在字幕文本")]
    [TextArea]
    public string titleText;

    [Header("可选：打字音效")]
    public AudioSource typeAudioSource; // 打字音效（可选）
    public float typeSoundInterval = 3; // 每隔几个字播放一次音效

    public Coroutine currentCoroutine; // 当前正在进行的协程
    public bool TextOver = false;//字幕播放是否结束

    public AudioSource audioPeiYin;//字幕配音

    private void OnEnable()
    {
        EventCenter.AddListener<string>("ShowSubtitle", ShowSubtitleHandler);
        EventCenter.AddListener("SkipTyping", Skip);
    }

    private void OnDisable()
    {
        EventCenter.RemoveListener<string>("ShowSubtitle", ShowSubtitleHandler);
        EventCenter.RemoveListener("SkipTyping", Skip);
    }


    /// <summary>
    /// 通过事件显示字幕
    /// </summary>
    private void ShowSubtitleHandler(string text)
    {
        Show(text);
    }

    /// <summary>
    /// 直接调用显示字幕
    /// </summary>
    public void Show(string text)
    {
        TextOver = false;
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(TypewriterRoutine(text));
    }

    private IEnumerator TypewriterRoutine(string fullText)
    {
        // 显示面板
        /* if (subtitlePanel != null)
             subtitlePanel.SetActive(true);*/

        subtitleText.text = "";
        subtitleText.color = new Color(subtitleText.color.r, subtitleText.color.g, subtitleText.color.b, 1f);//设置透明度为1
        typeAudioSource.PlayOneShot(typeAudioSource.clip);
        audioPeiYin.Play();
        // 逐字显示
        // int charCount = 0;//记录字符个数
        StartCoroutine(ChangingRandomNum());
        foreach (char c in fullText)
        {
            subtitleText.text += c;//逐字显示

            // 打字音效
            /*            if (typeAudioSource != null && charCount % typeSoundInterval == 0 && !char.IsWhiteSpace(c))
                        {

                        }
                        charCount++;*/

            yield return new WaitForSeconds(charInterval);//每个字显示间隔
        }
        typeAudioSource.Stop();
        // 停留
        yield return new WaitForSeconds(holdTime);//全部显示完后停留时间
        EventCenter.Trigger("HeiPingShousuo");
        yield return new WaitForSeconds(1.5f);
        GameFlowManager.Instance.JiaZaiUI.SetActive(true);
        // 淡出
        float timer = 0f;
        Color color = subtitleText.color;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;//计时器
            color.a = 1f - (timer / fadeOutTime);
            subtitleText.color = color;
            yield return null;
        }

        // 隐藏
        subtitleText.text = "";
        color.a = 1f;
        subtitleText.color = color;

        TextOver = true;
        
        /*        if (subtitlePanel != null)
                    subtitlePanel.SetActive(false);*/

        currentCoroutine = null;//结束协程
    }

    /// <summary>
    /// 跳过打字效果，直接显示全文
    /// </summary>
    public void Skip()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
            TextOver = true;
        }
    }

    IEnumerator ChangingRandomNum()//让数字在两个固定数字之间变化
    {
        while (TextOver != true)
        {
            charInterval = charInterval == 0.1f ? 0.3f : 0.1f;
            
            yield return new WaitForSeconds(2f);
        }

    }
}
