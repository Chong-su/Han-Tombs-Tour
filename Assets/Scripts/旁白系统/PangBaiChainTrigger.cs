using UnityEngine;

/// <summary>
/// 旁白链触发器：玩家走进Collider → 触发指定groupId的旁白 → 关闭自己 → 激活链上的下一个触发器
/// 与PangBaiManager通过现有事件对接：EventCenter.Trigger<int>("DuiHua", groupId)，无需修改PangBaiManager
///
/// 用法：
/// 1. 沿路摆几个空物体，各自加BoxCollider（不用手动勾IsTrigger，脚本会自动补勾），挂本脚本
/// 2. 只有链头勾选 isChainHead；其余的不勾——Awake时它们会自动把自己关掉，保证全链只有链头活着
/// 3. 每个触发器填 groupId（对应PangBaiManager里PangBaiGroups的组ID）和 nextTrigger（指向下一个，最后一个留空）
/// 4. 注意：相邻两个触发器的Collider范围不要叠在一起（玩家还在上一个圈内时下一个被激活，会立即连触发）
/// </summary>
public class PangBaiChainTrigger : MonoBehaviour
{
    [Header("触发哪组旁白（PangBaiManager里的groupId）")]
    public int groupId;

    [Header("下一个触发器（最后一个留空 = 链到此结束）")]
    public PangBaiChainTrigger nextTrigger;

    [Header("是否为链头（只有链头保持激活，其余Awake时自动关闭）")]
    public bool isChainHead = false;

    private bool hasTriggered;   //防重复触发
    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true; //忘勾IsTrigger时自动补救，防止把玩家物理挡住

        if (!isChainHead)
            gameObject.SetActive(false); //非链头：场景加载即自我关闭，等上一个触发器来激活
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;
        if (PangBaiManager.Instance == null) return; //保险:旁白管理器不在场景/未激活时不报错
        if(PangBaiManager.Instance.isPlayingGroup) return;
        hasTriggered = true;

        //触发旁白（PangBaiManager在OnEnable里监听"DuiHua"）
        EventCenter.Trigger<int>("DuiHua", groupId);

        //打开下一个，关闭自己
        if (nextTrigger != null)
            nextTrigger.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    //编辑器里可视化触发范围（链头青色，其余黄色）
    private void OnDrawGizmos()
    {
        Gizmos.color = isChainHead ? Color.cyan : Color.yellow;
        if (TryGetComponent(out BoxCollider box))
            Gizmos.DrawWireCube(transform.position + box.center, box.size);
    }
}
