using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 任务配置资产，在编辑器右键创建
/// </summary>
[CreateAssetMenu(fileName = "TaskConfig", menuName = "任务系统/任务配置文件")]
public class TaskConfigSO : ScriptableObject
{
    [Header("所有任务列表")]
    public List<TaskData> allTaskList = new List<TaskData>();

    [Header("所有章节列表")]
    public List<ChapterData> allChapterList = new List<ChapterData>();
}