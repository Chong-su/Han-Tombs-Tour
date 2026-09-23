using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对话UI控制器：监听对话事件刷新显示（同TaskUIController模式）
/// 打字机逐字显示 + 每句配音播放（由DialogueSystem通过OnDialogueVoice事件下发）
/// 注意：挂在Canvas或常驻UI根物体上，不要挂在对话面板自己身上（面板会被隐藏导致收不到事件）
/// </summary>
public class DialogueUIController : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject dialoguePanel;   //对话面板（开始激活、结束隐藏）
    public Text nameText;              //人物名称
    public Text contentText;           //对话内容
    public GameObject continueHint;    //"▼继续"提示（可选）

    [Header("配音播放器（每句的语音从这里播，不拖则自动补一个）")]
    public AudioSource voiceSource;

    [Header("打字机设置")]
    public float charInterval = 0.03f; //每个字出现的间隔（秒）

    /// <summary>是否正在打字中（供DialogueSystem查询，静态全局）</summary>
    public static bool IsTyping { get; private set; }

    private Coroutine typingCoroutine;
    private string fullText;      //当前条的完整文本
    private bool textCompleted;  //当前文本是否已显示完

    private void OnEnable()
    {
        EventCenter.AddListener<int>("OnDialogueStarted", OnDialogueStartedHandler);
        EventCenter.AddListener<string, string>("OnDialogueEntryChanged", OnEntryChangedHandler);
        EventCenter.AddListener<AudioClip>("OnDialogueVoice", OnDialogueVoiceHandler);
        EventCenter.AddListener<int>("OnDialogueEnded", OnDialogueEndedHandler);
        EventCenter.AddListener("OnDialogueSkipTyping", OnSkipTypingHandler);
    }

    private void OnDisable()
    {
        EventCenter.RemoveListener<int>("OnDialogueStarted", OnDialogueStartedHandler);
        EventCenter.RemoveListener<string, string>("OnDialogueEntryChanged", OnEntryChangedHandler);
        EventCenter.RemoveListener<AudioClip>("OnDialogueVoice", OnDialogueVoiceHandler);
        EventCenter.RemoveListener<int>("OnDialogueEnded", OnDialogueEndedHandler);
        EventCenter.RemoveListener("OnDialogueSkipTyping", OnSkipTypingHandler);
        IsTyping = false; //场景切换时对话被中断的保险
    }

    private void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false); //初始隐藏
        if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>(); //自动补配音播放器
    }

    // ───── 事件处理 ─────

    private void OnDialogueStartedHandler(int dialogueId)//对话开始
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
    }

    private void OnDialogueEndedHandler(int dialogueId)//对话结束
    {
        if (typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }
        IsTyping = false;
        if (voiceSource != null) voiceSource.Stop(); //停掉没播完的语音
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private void OnEntryChangedHandler(string speakerName, string content)//每条对话内容变化时触发
    {
        // 每条新对话：名称直接显示，内容走打字机
        if (nameText != null)
        {
            nameText.text = speakerName;
            if(speakerName == "我")
            {
                nameText.color = new Color32(255, 255, 255, 255);
            }
            else
            {
                nameText.color = new Color32(255, 189, 0, 255);
            }
        }

        fullText = content;
        textCompleted = false;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine); //先停掉上一条对话的打字机
        typingCoroutine = StartCoroutine(TypewriterRoutine(content, speakerName));
    }

    private void OnDialogueVoiceHandler(AudioClip clip)//这句的配音（null=没有语音）
    {
        if (voiceSource == null) return;
        voiceSource.Stop(); //换句先停上一句的语音
        if (clip != null)
        {
            voiceSource.clip = clip;
            voiceSource.Play();
        }
    }

    private void OnSkipTypingHandler()//跳过打字机效果
    {
        if (textCompleted) return;
        if (typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }
        FinishText();
    }

    // ───── 打字机效果 ─────

    private IEnumerator TypewriterRoutine(string Content,string Name)
    {
        IsTyping = true;
        textCompleted = false;
        if (continueHint != null) continueHint.SetActive(false);
        if (contentText != null) contentText.text = "";

        // 居中防跳动：整段文本全程参与排版（居中位置固定不动），
        // 未显示部分用透明色占位 → 文字从左往右逐个"显形"，而不是从中间长出来


        if (Name == "我")
        {
            contentText.color = new Color32(255, 255, 255, 255);
        }
        else
        {
            contentText.color = new Color32(255, 207, 134, 255);
        }

        int count = 0;
        while (count < Content.Length)
        {
            count++;
            contentText.text = Content.Substring(0, count) + "<color=#00000000>" + Content.Substring(count) + "</color>";
            yield return new WaitForSeconds(charInterval);
        }
        FinishText();
    }

    private void FinishText()
    {
        if (contentText != null) contentText.text = fullText;
        IsTyping = false;
        textCompleted = true;
        if (continueHint != null) continueHint.SetActive(true); //显示"继续"提示
    }
}
