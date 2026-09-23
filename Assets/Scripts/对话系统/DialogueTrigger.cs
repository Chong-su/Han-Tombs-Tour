using UnityEngine;

/// <summary>
/// 对话触发器：挂场景物体上（Collider勾IsTrigger），玩家在范围内按交互键开始对话
/// 配置了CeWenUI的：本组对话结束后自动弹出策问界面
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    public int dialogueId;                    //要播放的对话组ID
    public KeyCode interactKey = KeyCode.F;   //交互按键（Inspector里可改成E等）
    public GameObject CeWenUI;               //策问UI：本组对话结束后自动弹出

    private bool playerInRange;   //玩家是否在触发范围内
    private int quizBoundId = int.MinValue; //策问绑定的对话组ID（首次弹出策问的那组对话）
    private Animator animator;
    public string Anim_name;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void OnEnable()
    {
        // 监听对话结束：自己这组对话播完且配了策问UI → 打开策问
        EventCenter.AddListener<int>("OnDialogueEnded", OnDialogueEndedHandler);
    }

    private void OnDisable()
    {
        EventCenter.RemoveListener<int>("OnDialogueEnded", OnDialogueEndedHandler);
    }

    private void OnDialogueEndedHandler(int endedId)
    {

        if (CeWenUI == null || endedId != dialogueId) return;
        if (quizBoundId == int.MinValue) quizBoundId = endedId; //首次绑定（如1005）
        if (endedId != quizBoundId) return; //对话ID被策问切换后（如变成1006），不再弹策问
        CeWenUI.SetActive(true);
        CeWenUI.GetComponent<CeWenUIController>()?.Begin(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
        if(animator != null)
        {
            animator.SetBool(Anim_name, false);
        }
    }

    private void Update()
    {
        // 范围内按下交互键才开对话（策问进行中/其他UI打开时不响应）
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            if (CeWenUIController.IsQuizOpen) return;
            if (Move.Instance != null && Move.Instance.UIopen) return;
            EventCenter.Trigger<int>("StartDialogue", dialogueId);
            if (animator != null)
            {
                animator.SetBool(Anim_name, true);
            }
        }
    }
}
