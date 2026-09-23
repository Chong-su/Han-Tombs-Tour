using UnityEngine;

/// <summary>
/// BGM切换触发器：玩家进入Collider → 该Collider自动关闭 → 背景音乐切换成targetClip
/// 与AudioManager（音频控制器）结合：统一走PlayMusic入口——
/// 同曲自动忽略、换曲自动淡入淡出，单一musicSource播放，永不重叠
/// 用法：空物体 + BoxCollider（IsTrigger没勾也没关系，脚本自动补勾）+ 挂本脚本 + 拖入targetClip
/// </summary>
public class BgmSwitchTrigger : MonoBehaviour
{
    [Header("进入后切换到的背景音乐")]
    public AudioClip targetClip;

    private void Awake()
    {
        // 忘勾IsTrigger时自动补救：必须是Trigger才能收到OnTriggerEnter，也不会物理挡住玩家
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (targetClip == null)
        {
            Debug.LogWarning($"{name}：没指定targetClip，触发器未生效（Collider未关闭，配好音乐才会消耗）");
            return;
        }
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"{name}：场景里没有音频控制器（AudioManager），无法切换BGM");
            return;
        }

        AudioManager.Instance.PlayMusic(targetClip);
        GetComponent<Collider>().enabled = false; //只触发一次：关闭自己
    }
}
