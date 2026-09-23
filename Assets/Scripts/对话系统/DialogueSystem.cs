using System.Collections;
using UnityEngine;

/// <summary>
/// 对话系统 - 核心逻辑层
/// 职责：按配置播放对话、管理状态、广播对话事件
/// 原则：不直接操作UI，所有外部表现通过事件通知（同TaskManager模式）
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("对话配置文件")]
    [SerializeField] private DialogueConfigSo dialogueConfig;

    // 运行时状态
    private DialogueNumber currentDialogue; //当前对话组
    private int currentDialogueId;          //当前对话组ID
    private int currentIndex;               //当前条目下标
    private bool isPlaying;                 //是否正在播放对话

    /// <summary>对外只读：是否正在播放对话</summary>
    public bool IsPlaying => isPlaying;
    public GameObject CeWenUI; //策问UI

    [Header("自动播放设置")]
    public bool autoAdvance = true;    //对话开始后自动逐句推进直到结束（鼠标点击仍可提前推进）
    public float autoNextDelay = 1.5f; //每句文字显示完后停留的秒数（无配音时的节奏）

    private Coroutine autoAdvanceCo; //自动推进协程
    private float voiceEndTime;      //当前句配音预计播完的时刻（Time.time基准）
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // 任意脚本可无引用启动对话：EventCenter.Trigger<int>("StartDialogue", 1001);
        EventCenter.AddListener<int>("StartDialogue", StartDialogue);
    }

    private void OnDisable()
    {
        EventCenter.RemoveListener<int>("StartDialogue", StartDialogue);
    }

    private void Update()
    {
        // 播放中：按任意键/点鼠标 推进对话（想改成E键就换Input.GetKeyDown(KeyCode.E)）
        if (isPlaying)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Next();
            }
        }

    }

    /// <summary>开始播放一组对话</summary>
    public void StartDialogue(int dialogueId)
    {
        if (isPlaying) return; //同一时间只播一组，防止重复触发

        DialogueNumber dialogue = dialogueConfig.GetDialogueById(dialogueId);
        if (dialogue == null || dialogue.dialogueDatas.Count == 0)
        {
            Debug.LogWarning($"对话组 {dialogueId} 不存在或没有内容，检查对话配置文件");
            return;
        }

        currentDialogue = dialogue;
        currentDialogueId = dialogueId;
        currentIndex = 0;
        isPlaying = true;

        EventCenter.Trigger<int>("OnDialogueStarted", dialogueId);
        ShowCurrentEntry();
    }

    /// <summary>推进到下一条对话（按任意键/继续按钮都调这里）</summary>
    public void Next()
    {
        if (!isPlaying) return;

        // 第一下点击：若UI还在打字，先跳过打字显示全文，不推进
        if (DialogueUIController.IsTyping)
        {
            EventCenter.Trigger("OnDialogueSkipTyping");
            return;
        }

        // 计算下一条：nextDialogueNum>0时跳转到对应序号，否则顺序+1
        DialogueData current = currentDialogue.dialogueDatas[currentIndex];
        int nextIndex = -1;
        if (current.nextDialogueNum > 0)
        {
            nextIndex = currentDialogue.dialogueDatas.FindIndex(d => d.dialogueNum == current.nextDialogueNum);
            if (nextIndex < 0)
                Debug.LogWarning($"对话{currentDialogueId}：找不到序号{current.nextDialogueNum}的条目，改为顺序播放");
        }
        if (nextIndex < 0) nextIndex = currentIndex + 1;

        // 没有下一条 → 结束
        if (nextIndex >= currentDialogue.dialogueDatas.Count)
        {
            EndDialogue();
            return;
        }

        currentIndex = nextIndex;
        ShowCurrentEntry();
    }

    /// <summary>显示当前条目（广播给UI）</summary>
    private void ShowCurrentEntry()
    {
        DialogueData entry = currentDialogue.dialogueDatas[currentIndex];
        EventCenter.Trigger<string, string>("OnDialogueEntryChanged", entry.dialogueName, entry.dialogueContent);
        // 广播这句的配音：null=没有配音，UI会停掉当前语音
        EventCenter.Trigger<AudioClip>("OnDialogueVoice", entry.duihuaAudio);

        // 记录本句配音预计播完的时刻，供自动推进等待
        voiceEndTime = entry.duihuaAudio != null ? Time.time + entry.duihuaAudio.length : 0f;

        // 自动播放：每句重新起一个等待（手动点击推进时旧的会被替换）
        if (autoAdvance)
        {
            if (autoAdvanceCo != null) StopCoroutine(autoAdvanceCo);
            autoAdvanceCo = StartCoroutine(AutoAdvanceRoutine());
        }
    }

    /// <summary>自动推进：等打字机完成 → 等配音播完/固定停留 → 下一句</summary>
    private IEnumerator AutoAdvanceRoutine()
    {
        // 1. 等打字机把这句显示完
        while (DialogueUIController.IsTyping) yield return null;

        // 2. 有配音：等语音播完；同时保证至少停留autoNextDelay方便读字，取较久者
        float wait = Mathf.Max(voiceEndTime - Time.time, autoNextDelay);
        yield return new WaitForSeconds(wait);

        // 3. 推进到下一句（最后一句会走EndDialogue结束整组对话）
        if (isPlaying) Next();
    }

    /// <summary>结束当前对话</summary>
    private void EndDialogue()
    {
        if (autoAdvanceCo != null) { StopCoroutine(autoAdvanceCo); autoAdvanceCo = null; } //停掉自动推进
        isPlaying = false;

        EventCenter.Trigger<int>("OnDialogueEnded", currentDialogueId);
        // 上报任务系统：上报自身dialogueId（铁律：绝不报TaskManager.CurrentTaskID）
        EventCenter.Trigger<int>("OnDialogFinished", currentDialogueId);
        Debug.Log($"对话{currentDialogueId}播放完毕");
        currentDialogue = null;
        if (currentDialogueId == 1005)
        {
            if (CeWenUI != null)
            {
                CeWenUI.SetActive(true);
            }
            currentDialogueId = 1006;
        }
    }
}