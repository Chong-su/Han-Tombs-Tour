using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 任务完成条件类型常量，统一管理，避免硬编码拼写错误
/// </summary>
public static class TaskConditionType
{
    public const string Dialog = "Dialog";
    public const string KillMonster = "KillMonster";
    public const string EnterArea = "EnterArea";
    public const string Interact = "Interact";
    public const string CollectItem = "CollectItem";
}

/// <summary>
/// 全局任务管理器 - 核心逻辑层
/// 职责：状态管理、条件匹配、链式解锁、章节校验、存档持久化
/// 原则：不直接操作UI与场景物体，所有外部表现通过事件通知
/// </summary>
public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }// 单例实例

    /// <summary>
    /// 当前主线任务变更事件，外部可监听用于UI更新
    /// </summary>
    public static event System.Action<int> OnCurrentTaskChanged;

    [Header("任务配置文件")]
    [SerializeField] private TaskConfigSO taskConfig;

    // 运行时数据容器
    private Dictionary<int, TaskData> taskDict = new Dictionary<int, TaskData>();// 任务ID -> 任务数据
    private Dictionary<int, ChapterData> chapterDict = new Dictionary<int, ChapterData>();// 章节ID -> 章节数据
    private Dictionary<int, TaskState> runtimeTaskState = new Dictionary<int, TaskState>();// 任务ID -> 任务状态
    // 每个任务独立的进度计数器（解决原全局计数器互相干扰的Bug）
    private Dictionary<int, int> taskProgressDict = new Dictionary<int, int>();

    // 当前主线任务ID（仅记录主线，不影响支线并行）
    public int CurrentTaskID;
    public Transform Areafather;

    private void Awake()
    {
        // 单例初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化任务系统
        InitTaskSystem();

        // 注册所有通用业务事件监听
        RegisterEventListeners();
    }

    #region 事件注册与注销
    /// <summary>
    /// 获取当前主线任务所属的章节数据
    /// </summary>
    public ChapterData GetCurrentChapter()
    {
        if (CurrentTaskID <= 0) return null;
        TaskData currentTask = GetTaskDataById(CurrentTaskID);
        if (currentTask == null) return null;
        return GetChapterDataById(currentTask.belongChapterID);
    }
    /// <summary>
    /// 获取指定章节的任务进度（已完成数 / 总任务数）
    /// </summary>
    public (int completedCount, int totalCount) GetChapterProgress(int chapterId)
    {
        if (!chapterDict.TryGetValue(chapterId, out ChapterData chapter))
            return (0, 0);

        int total = chapter.taskIDs.Count;
        int completed = 0;

        foreach (int taskId in chapter.taskIDs)
        {
            if (GetTaskState(taskId) == TaskState.Completed)
                completed++;
        }

        return (completed, total);
    }

    private void RegisterEventListeners()//注册事件监听
    {
        EventCenter.AddListener<int>("OnDialogFinished", OnDialogFinishedHandler);
        EventCenter.AddListener<int>("OnMonsterKilled", OnMonsterKilledHandler);
        EventCenter.AddListener<int>("OnPlayerEnterArea", OnPlayerEnterAreaHandler);
        EventCenter.AddListener<int>("OnInteractFinished", OnInteractFinishedHandler);
        EventCenter.AddListener<int, int>("OnItemCollected", OnItemCollectedHandler);
    }

    private void UnregisterEventListeners()//注销事件监听
    {
        EventCenter.RemoveListener<int>("OnDialogFinished", OnDialogFinishedHandler);
        EventCenter.RemoveListener<int>("OnMonsterKilled", OnMonsterKilledHandler);
        EventCenter.RemoveListener<int>("OnPlayerEnterArea", OnPlayerEnterAreaHandler);
        EventCenter.RemoveListener<int>("OnInteractFinished", OnInteractFinishedHandler);
        EventCenter.RemoveListener<int, int>("OnItemCollected", OnItemCollectedHandler);
    }

    private void OnDestroy()
    {
        UnregisterEventListeners();
    }
    #endregion

    #region 系统初始化
    /// <summary>
    /// 初始化任务系统：加载配置 + 读取存档 + 初始任务激活
    /// </summary>
    private void InitTaskSystem()
    {
        // 1. 加载配置数据到字典，实现O(1)查询
        foreach (var task in taskConfig.allTaskList)
        {
            taskDict.TryAdd(task.taskID, task);//
        }
        foreach (var chapter in taskConfig.allChapterList)
        {
            chapterDict.TryAdd(chapter.chapterID, chapter);
        }

        ChapterData firstChapter = taskConfig.allChapterList[0];// 获取第一个章节
        if (firstChapter.taskIDs.Count > 0)
        {
            int firstTaskId = firstChapter.taskIDs[0];// 获取第一个任务ID
            ActivateTask(firstTaskId);// 激活第一个任务
            UpdateCurrentMainTask(firstTaskId);// 更新当前主线任务
        }

        // 抛出初始化完成事件，UI层监听后自行刷新显示
        EventCenter.Trigger("OnInitTaskFinished");
        EventCenter.Trigger("DuiHua", 0);
        Debug.Log("任务系统初始化完成");
    }
    #endregion

    #region 条件匹配核心逻辑
    /// <summary>
    /// 通用条件匹配：遍历所有进行中任务，匹配对应类型的完成条件
    /// </summary>
    /// <param name="conditionType">条件类型，对应TaskConditionType常量</param>
    /// <param name="targetId">业务目标ID（物品ID/怪物ID/区域ID等）</param>
    /// <param name="addCount">累计进度数，默认1</param>
    private void MatchTaskCondition(string conditionType, int targetId, int addCount = 1)
    {
        foreach (var task in taskDict.Values)
        {
            // 只检测进行中的任务
            if (GetTaskState(task.taskID) != TaskState.Active)
                continue;

            // 解析完成条件配置，增加容错处理（修复原int.Parse无容错崩溃的Bug）
            string[] parts = task.finishCondition.Split(':');//"冒号"成为切割字符串，程序会找到字符串里所有的冒号，在冒号处“剪断”，把一个长字符串变成几个短字符串。
            if (parts.Length < 2) continue;//如果字符串没有冒号，则跳过

            string taskConditionType = parts[0];//获取任务条件类型
            if (!int.TryParse(parts[1], out int taskTargetId))//将parts[1]转换为int类型，如果转换失败(即不是有效的整数)，则跳过
            {
                Debug.LogWarning($"任务{task.taskID}的finishCondition配置错误，目标ID解析失败：{task.finishCondition}");
                continue;
            }

            // 解析目标数量，默认1
            int taskTargetCount = 1;
            if (parts.Length >= 3 && int.TryParse(parts[2], out int count))
            {
                taskTargetCount = count;
            }

            // 类型与目标ID不匹配则跳过
            if (taskConditionType != conditionType || taskTargetId != targetId)
                continue;

            // 单次完成型任务
            if (taskTargetCount <= 1)
            {
                CompleteTask(task.taskID);
                continue;
            }

            // 累计进度型任务：每个任务独立计数
            if (!taskProgressDict.ContainsKey(task.taskID))//如果任务进度字典中没有这个任务，则添加
            {
                taskProgressDict.Add(task.taskID, 0);
            }
            taskProgressDict[task.taskID] += addCount;//累加进度
            Debug.Log($"任务[{task.taskName}]进度：{taskProgressDict[task.taskID]}/{taskTargetCount}");

            // 进度达标则完成任务
            if (taskProgressDict[task.taskID] >= taskTargetCount)
            {
                CompleteTask(task.taskID);
                taskProgressDict.Remove(task.taskID); // 完成后清理进度数据
            }
        }
    }

    // 各业务事件回调
    private void OnDialogFinishedHandler(int dialogId)//对话完成事件回调
    {
        MatchTaskCondition(TaskConditionType.Dialog, dialogId);
    }

    private void OnMonsterKilledHandler(int monsterId)//怪物击杀事件回调
    {
        MatchTaskCondition(TaskConditionType.KillMonster, monsterId);
    }

    private void OnPlayerEnterAreaHandler(int areaId)//玩家进入区域事件回调
    {
        MatchTaskCondition(TaskConditionType.EnterArea, areaId);
    }

    private void OnInteractFinishedHandler(int interactId)//交互完成事件回调
    {
        MatchTaskCondition(TaskConditionType.Interact, interactId);
    }

    private void OnItemCollectedHandler(int itemId, int count)//物品收集事件回调
    {
        MatchTaskCondition(TaskConditionType.CollectItem, itemId, count);
    }
    #endregion

    #region 任务核心流转
    /// <summary>
    /// 完成指定任务（系统核心入口）
    /// </summary>
    /// <param name="taskID">任务ID</param>
    public void CompleteTask(int taskID)
    {
        if (!taskDict.TryGetValue(taskID, out TaskData task))
        {
            Debug.LogWarning($"完成任务失败：任务ID {taskID} 不存在");
            return;
        }

        TaskState currentState = GetTaskState(taskID);
        if (currentState != TaskState.Active)
        {
            Debug.LogWarning($"完成任务失败：任务[{task.taskName}]未处于激活状态");
            return;
        }

        // 1. 标记任务为已完成
        SetTaskState(taskID, TaskState.Completed);
        if (task.nextTaskIds.Count > 0 && taskDict.TryGetValue(task.nextTaskIds[0], out TaskData nextTaskData))
        {
            
            if (nextTaskData.taskType == TaskType.AreaExplore)
            {
                Debug.Log($"任务[{nextTaskData.taskName}]激活,区域触发器已启用");
                GameObject.Find(nextTaskData.Area).GetComponent<Collider>().enabled = true;
            }
        }


        Debug.Log($"任务完成：{task.taskName}");

        // 2. 抛出任务完成事件，UI/奖励/成就等系统自行监听处理
        EventCenter.Trigger<int>("OnTaskCompleted", taskID);

        // 3. 遍历后续任务，校验前置并解锁
        foreach (int nextTaskID in task.nextTaskIds)
        {
            if (CheckTaskCanActivate(nextTaskID))
            {
                ActivateTask(nextTaskID);
                UpdateCurrentMainTask(nextTaskID);
            }
        }

        // 4. 检查所属章节是否全部完成
        if (task.belongChapterID > 0)
        {
            CheckChapterCompleted(task.belongChapterID);
        }
    }

    /// <summary>
    /// 激活任务：将状态改为进行中
    /// </summary>
    private void ActivateTask(int taskID)
    {
        // 修复：非未激活状态禁止激活，防止章节解锁等路径把已完成/进行中的任务拉回Active
        if (GetTaskState(taskID) != TaskState.Inactive)
        {
            Debug.LogWarning($"任务{taskID}当前状态为{GetTaskState(taskID)}，跳过激活（防止重复激活劫持当前主线任务）");
            return;
        }
        SetTaskState(taskID, TaskState.Active);
        taskDict.TryGetValue(taskID, out TaskData task);
        if(task.collectType.ToString() != "kong")
        {
            List<GameObject> GameObjectCollider = GameObject.FindGameObjectsWithTag(task.collectType.ToString()).ToList();
            Debug.Log($"任务[{task.taskName}]激活,收集类型：{task.collectType}");
            foreach (GameObject var in GameObjectCollider)
            {
                var.GetComponent<Collider>().enabled = true;
            }
        } 
        if(task.taskType.ToString() == "Area Explore")
        {

        }
       
        Debug.Log($"任务激活：{taskDict[taskID].taskName}");
        EventCenter.Trigger<int>("OnTaskActivated", taskID);
    }

    /// <summary>
    /// 校验任务是否满足激活条件（所有前置任务已完成）
    /// </summary>
    private bool CheckTaskCanActivate(int taskID)
    {
        if (!taskDict.TryGetValue(taskID, out TaskData task))
            return false;

        if (GetTaskState(taskID) != TaskState.Inactive)
            return false;

        foreach (int preID in task.preTaskIds)
        {
            if (GetTaskState(preID) != TaskState.Completed)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 更新当前主线任务ID并触发变更事件
    /// </summary>
    private void UpdateCurrentMainTask(int taskID)
    {
        CurrentTaskID = taskID;
        OnCurrentTaskChanged?.Invoke(taskID);
    }
    #endregion

    #region 章节结算逻辑
    /// <summary>
    /// 检查章节是否全部完成，完成则触发章节结算并解锁下一章节
    /// </summary>
    private void CheckChapterCompleted(int chapterID)
    {
        if (!chapterDict.TryGetValue(chapterID, out ChapterData chapter))
            return;

        // 遍历章节内所有任务，有一个未完成则直接返回
        foreach (int taskID in chapter.taskIDs)
        {
            if (GetTaskState(taskID) != TaskState.Completed)
            {
                return;
            }
        }

        Debug.Log($"章节完成：{chapter.chapterName}");
        EventCenter.Trigger<int>("OnChapterCompleted", chapterID);

        // 解锁下一章节的第一个任务（修复原章节名索引错误的Bug）
        if (chapter.nextChapterID > 0 && chapterDict.TryGetValue(chapter.nextChapterID, out ChapterData nextChapter))
        {
            if (nextChapter.taskIDs.Count > 0)
            {
                int firstTaskId = nextChapter.taskIDs[0];
                // 修复：加状态校验——章节配置重复/任务已完成时不重新激活，
                // 否则会把已完成的任务拉回Active并劫持CurrentTaskID，UI看起来像"任务没跳转"
                if (CheckTaskCanActivate(firstTaskId))
                {
                    Debug.Log($"解锁下一章节：{nextChapter.chapterName}");
                    ActivateTask(firstTaskId);
                    UpdateCurrentMainTask(firstTaskId);
                }
            }
        }
    }
    #endregion

    #region 状态读写与存档
    /// <summary>
    /// 获取任务状态
    /// </summary>
    public TaskState GetTaskState(int taskID)
    {
        return runtimeTaskState.TryGetValue(taskID, out TaskState state)
            ? state: TaskState.Inactive;
    }

    /// <summary>
    /// 根据ID获取任务配置数据
    /// </summary>
    public TaskData GetTaskDataById(int taskID)
    {
        taskDict.TryGetValue(taskID, out TaskData data);
        return data;
    }

    /// <summary>
    /// 根据ID获取章节配置数据
    /// </summary>
    public ChapterData GetChapterDataById(int chapterID)
    {
        chapterDict.TryGetValue(chapterID, out ChapterData data);
        return data;
    }

    /// <summary>
    /// 设置任务状态
    /// </summary>
    private void SetTaskState(int taskID, TaskState state)
    {
        if (runtimeTaskState.ContainsKey(taskID))
        {
            runtimeTaskState[taskID] = state;
        }
        else
        {
            runtimeTaskState.Add(taskID, state);
            
        }
    }

    #endregion

    #region 调试工具方法
    /// <summary>
    /// 调试用：强制完成指定任务
    /// </summary>
    [ContextMenu("调试/强制完成当前任务")]
    private void DebugCompleteCurrentTask()
    {
        CompleteTask(CurrentTaskID);
    }
    #endregion
}