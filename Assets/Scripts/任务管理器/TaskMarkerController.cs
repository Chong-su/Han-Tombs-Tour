using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 任务指引小白点（顺序模式）：
/// 任务激活时只显示"当前步骤"的小白点；某机关解密完成后它的点永久熄灭、下一个点出现
/// markers按解密顺序排列（第1个先亮，解密后亮第2个……）
/// 挂在常驻激活的物体上（挂在Canvas上）
/// </summary>
public class TaskMarkerController : MonoBehaviour
{
    [Serializable]
    public class MarkerEntry
    {
        public GameObject marker;        //提示小白点
        [Tooltip("该任务激活时开始显示此小白点")]
        public int showOnTaskId;         //如1002=解开所有密室
        [Tooltip("该机关解密完成后此点永久熄灭并显示下一个（与JiGuanData.name一致）")]
        public string solvedJiGuanName;  //如"方形门"

        [NonSerialized] public bool solved; //运行时：该机关已解密（不进Inspector、不序列化）
    }

    public List<MarkerEntry> markers = new List<MarkerEntry>();

    private void Start()
    {
        // 初始全部隐藏、未解密状态清零
        foreach (var m in markers)
        {
            if (m.marker != null) m.marker.SetActive(false);
            m.solved = false;
        }

        // 兜底：进场时某任务已经是进行中（中途离开再回来），补显示"当前步骤"的小白点
        ShowCurrentStep(-1);
    }

    private void OnEnable()
    {
        EventCenter.AddListener<int>("OnTaskActivated", OnTaskActivatedHandler);
        EventCenter.AddListener<int>("OnTaskCompleted", OnTaskCompletedHandler);
        EventCenter.AddListener<string>("OnJiGuanSolved", OnJiGuanSolvedHandler);
    }

    private void OnDisable()
    {
        EventCenter.RemoveListener<int>("OnTaskActivated", OnTaskActivatedHandler);
        EventCenter.RemoveListener<int>("OnTaskCompleted", OnTaskCompletedHandler);
        EventCenter.RemoveListener<string>("OnJiGuanSolved", OnJiGuanSolvedHandler);
    }

    private void OnTaskActivatedHandler(int taskId)
    {
        // 任务激活：只显示该任务下第一个未解密机关的小白点（顺序指引）
        ShowCurrentStep(taskId);
    }

    private void OnTaskCompletedHandler(int taskId)
    {
        // 任务整体完成时兜底隐藏（防止跳关后残留）
        foreach (var m in markers)
            if (m.showOnTaskId == taskId && m.marker != null)
                m.marker.SetActive(false);
    }

    private void OnJiGuanSolvedHandler(string jiGuanName)
    {
        foreach (var m in markers)
        {
            if (m.solvedJiGuanName == jiGuanName && m.marker != null)
            {
                m.solved = true;               //记住：这个已解密，之后不再点亮
                m.marker.SetActive(false);     //当前点熄灭
                ShowNextStep(m.showOnTaskId);  //亮起该任务下一个未解密的点
                return;
            }
        }
    }

    /// <summary>显示该任务下第一个"未解密"的小白点（taskId=-1时不过滤任务）</summary>
    private void ShowCurrentStep(int taskId)
    {
        foreach (var m in markers)
        {
            if (m.marker == null || m.solved) continue; //已解密的不算"当前步骤"
            if (taskId != -1 && m.showOnTaskId != taskId) continue;
            if (TaskManager.Instance != null && TaskManager.Instance.GetTaskState(m.showOnTaskId) != TaskState.Active) continue;

            // 找到当前步骤：同任务的其他点全部熄灭，只亮这一个
            foreach (var other in markers)
                if (other.marker != null && other.showOnTaskId == m.showOnTaskId && !ReferenceEquals(other, m))
                    other.marker.SetActive(false);
            m.marker.SetActive(true);
            return;
        }
    }

    /// <summary>解密完一个点后，显示同一任务下一个"未解密且未亮"的点（没有了就全灭）</summary>
    private void ShowNextStep(int taskId)
    {
        foreach (var m in markers)
        {
            if (m.marker == null || m.solved || m.showOnTaskId != taskId) continue; //已解密的跳过
            if (m.marker.activeSelf) continue; //已亮着的跳过
            if (TaskManager.Instance != null && TaskManager.Instance.GetTaskState(taskId) != TaskState.Active) continue;
            m.marker.SetActive(true);
            return;
        }
    }
}
