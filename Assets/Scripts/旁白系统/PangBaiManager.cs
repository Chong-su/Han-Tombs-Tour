using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 旁白管理器：一次触发播一组多句旁白
/// 触发：EventCenter.Trigger<int>("DuiHua", groupId)
/// 流程：显示对话框→逐句(打字机+配音)→配音播完自动下一句→最后一句播完→淡出→关闭对话框
/// </summary>
public class PangBaiManager : MonoBehaviour
{
    public static PangBaiManager Instance {  get; private set; }

    [Header("UI引用")]
    public Text DuiHuaKuang;        //旁白文本
    public GameObject DuiHuaPanel;  //旁白对话框面板（触发时激活，播完关闭；可留空=只控制文字）

    [Header("配音播放器（每句的配音从这里播）")]
    public AudioSource DuiHuaAudio;

    [Serializable]
    public class PangBaiLine        //单句旁白：文本+配音
    {
        [TextArea(2, 4)]
        public string Content;
        public AudioClip duihuaAudio; //这句的配音（留空=只显示文字，按holdTime停留）
    }

    [Serializable]
    public class PangBaiGroup       //一组旁白（触发一次播多句）
    {
        public int groupId;          //触发ID：EventCenter.Trigger<int>("DuiHua", groupId)
        public PangBaiLine[] lines;  //组内多句话，按顺序播
    }

    [Header("旁白数据")]
    public List<PangBaiGroup> PangBaiGroups = new List<PangBaiGroup>();

    [Header("打字设置")]
    public float charInterval = 0.1f;  //每个字出现的间隔
    public float holdTime = 2f;        //没有配音的句子，显示完的停留时间
    public float fadeOutTime = 0.5f;   //整组播完的淡出时间

    private Coroutine currentCoroutine;//当前播放协程
    public  bool isPlayingGroup;       //是否正在播一组旁白（防止重复触发）

    private void Awake()
    {
        Instance = this;
    }
    private void OnEnable()
    {
        EventCenter.AddListener<int>("DuiHua", TriggerDuiHua);
    }

    private void OnDisable()
    {
        EventCenter.RemoveListener<int>("DuiHua", TriggerDuiHua);
    }

    /// <summary>事件入口：按组ID触发一整组旁白</summary>
    public void TriggerDuiHua(int groupId)
    {
        foreach (var group in PangBaiGroups)
        {
            if (group.groupId == groupId)
            {
                PlayGroup(group);
                return;
            }
        }
        Debug.LogWarning($"旁白：找不到groupId={groupId}，请检查PangBaiGroups配置");
    }

    /// <summary>直接调用：播放一组旁白</summary>
    public void PlayGroup(PangBaiGroup group)
    {
        if (isPlayingGroup) return;      //正在播，忽略重复触发
        if (group == null || group.lines == null || group.lines.Length == 0) return;

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(GroupRoutine(group));
    }

    private IEnumerator GroupRoutine(PangBaiGroup group)
    {
        isPlayingGroup = true;

        // 打开对话框
        if (DuiHuaPanel != null) DuiHuaPanel.SetActive(true);
        if (DuiHuaKuang != null)
            DuiHuaKuang.color = new Color(DuiHuaKuang.color.r, DuiHuaKuang.color.g, DuiHuaKuang.color.b, 1f); //恢复透明度

        foreach (var line in group.lines)
        {
            // ① 打字机显示这句文本（整段参与排版：未显示部分透明占位，居中不跳动）
            if (DuiHuaKuang != null)
            {
                int count = 0;
                while (count < line.Content.Length)
                {
                    count++;
                    DuiHuaKuang.text = line.Content.Substring(0, count) + "<color=#00000000>" + line.Content.Substring(count) + "</color>";
                    yield return new WaitForSeconds(charInterval);
                }
                DuiHuaKuang.text = line.Content; //去掉末尾透明占位标签
            }

            // ② 播这句的配音
            if (DuiHuaAudio != null && line.duihuaAudio != null)
            {
                DuiHuaAudio.clip = line.duihuaAudio;
                DuiHuaAudio.Play();
                // ③ 等配音播完再进下一句
                while (DuiHuaAudio.isPlaying)
                {
                    yield return null;
                }
            }
            else
            {
                // 没有配音的句子：文字显示完停留holdTime
                yield return new WaitForSeconds(holdTime);
            }
        }

        // 整组播完 → 淡出 → 关闭对话框
        if (DuiHuaKuang != null)
        {
            float timer = 0f;
            Color color = DuiHuaKuang.color;
            while (timer < fadeOutTime)
            {
                timer += Time.deltaTime;
                color.a = 1f - (timer / fadeOutTime);
                DuiHuaKuang.color = color;
                yield return null;
            }
            // 恢复颜色供下次使用
            color.a = 1f;
            DuiHuaKuang.color = color;
            DuiHuaKuang.text = "";
        }
        if (DuiHuaPanel != null) DuiHuaPanel.SetActive(false);

        currentCoroutine = null;
        isPlayingGroup = false;
    }

    /// <summary>跳过当前整组旁白（比如ESC调用）</summary>
    public void Skip()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
        isPlayingGroup = false;
        if (DuiHuaAudio != null) DuiHuaAudio.Stop();
        if (DuiHuaKuang != null)
        {
            DuiHuaKuang.color = new Color(DuiHuaKuang.color.r, DuiHuaKuang.color.g, DuiHuaKuang.color.b, 1f);
            DuiHuaKuang.text = "";
        }
        if (DuiHuaPanel != null) DuiHuaPanel.SetActive(false);
    }
}
