using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 策问UI控制器：挂在策问_Canvas根物体上（初始不激活）
/// 由DialogueTrigger在本组对话结束后激活并调用Begin()
/// 流程：出题→点选项→答对变绿进下一题/答错变红可重选→全部答完→
///       把NPC的对话组ID切换为nextDialogueId（接任务系统）→自动关闭
/// </summary>
public class CeWenUIController : MonoBehaviour
{
    [Serializable]
    public class CeWenTi
    {
        [TextArea(2, 4)]
        public string tiMu;          //题目
        public string[] xuanXiang;   //选项文本（数量不多于选项按钮数）
        public int correctIndex;     //正确选项下标（从0开始）
    }

    [Header("UI引用")]
    public Text questionText;          //题目文本
    public Button[] optionButtons;     //选项按钮（按钮子物体Text自动填选项文本）

    [Header("策问题目")]
    public CeWenTi[] questions;

    [Header("答完后NPC切换到这组对话，接任务系统")]
    public int nextDialogueId = 1006;

    [Header("答题反馈")]
    public float feedbackTime = 0.5f;  //答对/答错的颜色停留时间

    /// <summary>策问是否正在进行（供DialogueTrigger判断，防止策问中按F又开对话）</summary>
    public static bool IsQuizOpen { get; private set; }

    private static bool quizFinished;  //本次Play内已答完（场景重载后不会重复出题）
    private DialogueTrigger owner;      //触发这次策问的NPC
    private int currentIndex;           //当前题目下标
    private bool answering;             //反馈播放中，防连点

    private void Start()
    {
        // 首次激活时给所有选项按钮自动挂点击事件
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i; //闭包捕获下标
            if (optionButtons[i] != null)
                optionButtons[i].onClick.AddListener(() => OnOptionClicked(index));
            //注意这里不能直接OnOptionClicked(i)，因为i是循环变量，每次循环都指向最后一个i
        }
    }

    /// <summary>对话结束后由DialogueTrigger调用，开始策问</summary>
    public void Begin(DialogueTrigger trigger)
    {
        // 已答完（如场景重载后再触发）：跳过策问，但保证NPC对话已切到下一段
        if (quizFinished)
        {
            if (trigger != null) trigger.dialogueId = nextDialogueId;
            gameObject.SetActive(false);
            return;
        }
        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("策问没有配置题目，请检查策问UI控制器上的questions数组");
            gameObject.SetActive(false);
            return;
        }

        owner = trigger;
        currentIndex = 0;
        IsQuizOpen = true;

        // 广播策问开始：Move监听后锁视角+鼠标显示（与对话锁定同一套）
        EventCenter.Trigger<int>("OnCeWenStarted", nextDialogueId);

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        answering = false;
        CeWenTi q = questions[currentIndex];
        if (questionText != null) questionText.text = q.tiMu;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) continue;
            optionButtons[i].interactable = true; //答错后重选时，按钮会变成不可交互，这里恢复
            Text label = optionButtons[i].GetComponentInChildren<Text>();
            if (label != null && q.xuanXiang != null && i < q.xuanXiang.Length)//给按钮的子物体文本赋值
                label.text = q.xuanXiang[i];
        }
    }

    /// <summary>选项点击（按钮事件在Start里自动挂接，也可手动绑）</summary>
    public void OnOptionClicked(int index)
    {
        if (answering || questions == null || currentIndex >= questions.Length) return;
        answering = true;
        CeWenTi q = questions[currentIndex];
        if (index == q.correctIndex)
            StartCoroutine(CorrectFlow(index));
        else
            StartCoroutine(WrongFlow(index));
    }

    private IEnumerator CorrectFlow(int index) //答对：变绿→下一题或结束
    {
        SetOptionColor(index, Color.green);
        yield return new WaitForSeconds(feedbackTime);
        SetOptionColor(index, Color.black);
        currentIndex++;
        if (currentIndex >= questions.Length) FinishCeWen();
        else ShowQuestion();
    }

    private IEnumerator WrongFlow(int index) //答错：变红→恢复可重选
    {
        SetOptionColor(index, Color.red);
        foreach (Button b in optionButtons) if (b != null) b.interactable = false;
        yield return new WaitForSeconds(feedbackTime);
        SetOptionColor(index, Color.black);
        foreach (Button b in optionButtons) if (b != null) b.interactable = true;
        answering = false;
    }

    private void SetOptionColor(int index, Color c)
    {
        if (optionButtons == null || index < 0 || index >= optionButtons.Length || optionButtons[index] == null) return;
        Text label = optionButtons[index].GetComponentInChildren<Text>();
        if (label != null) label.color = c;
    }

    private void FinishCeWen()
    {
        quizFinished = true; //答完题后，下次再触发策问时直接跳过
        IsQuizOpen = false; //策问结束，解锁视角+鼠标回到锁定隐藏

        // 关键：NPC对话组ID切换→再对话就是1006组→对话完成上报OnDialogFinished(1006)→任务系统接上
        if (owner != null) owner.dialogueId = nextDialogueId;
        EventCenter.Trigger<int>("StartDialogue", nextDialogueId);
        // 广播策问结束：Move解锁视角、鼠标回到锁定隐藏
        //EventCenter.Trigger<int>("OnCeWenEnded", nextDialogueId);

        gameObject.SetActive(false);
        EventCenter.Trigger<string>("OnJiGuanSolved", "策问");
        Debug.Log($"策问完成：NPC对话组已切换为{nextDialogueId}，再与其对话即可推进任务");
    }
}
