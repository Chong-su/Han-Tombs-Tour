using UnityEngine;
using UnityEngine.UI;

public class TaskUIController : MonoBehaviour
{
    [Header("主线任务名称")]
    public Text mainTaskNameText;

    [Header("任务完成条件")]
    public Text taskNameText;

    private void OnEnable()
    {
        EventCenter.AddListener("OnInitTaskFinished", OnInitTaskFinishedHandler);
        EventCenter.AddListener<int>("OnTaskActivated", OnTaskActivatedHandler);
        EventCenter.AddListener<int>("OnChapterCompleted", OnChapterCompletedHandler);
    }

    private void OnDisable()
    {
        EventCenter.RemoveListener("OnInitTaskFinished", OnInitTaskFinishedHandler);
        EventCenter.RemoveListener<int>("OnTaskActivated", OnTaskActivatedHandler);
        EventCenter.RemoveListener<int>("OnChapterCompleted", OnChapterCompletedHandler);
    }

    private void Start()
    {
        RefreshDisplay();
    }

    private void OnInitTaskFinishedHandler()
    {
        RefreshDisplay();
    }

    private void OnTaskActivatedHandler(int taskId)
    {
        RefreshDisplay(taskId);
    }

    private void OnChapterCompletedHandler(int chapterId)
    {
        RefreshDisplay();
    }

    private void RefreshDisplay(int? taskId = null)//括号中?表示可以为空
    {
        if (TaskManager.Instance == null) return;

        ChapterData currentChapter = TaskManager.Instance.GetCurrentChapter();//获取当前章节数据
        if (currentChapter != null)
        {
            mainTaskNameText.text = currentChapter.chapterName;
            Debug.Log("当前章节名称：" + currentChapter.chapterName);
        }

        int displayTaskId = taskId ?? TaskManager.Instance.CurrentTaskID;
        TaskData taskData = TaskManager.Instance.GetTaskDataById(displayTaskId);
        if (taskData != null)
        {
            taskNameText.text = taskData.taskName;
            Debug.Log($"当前任务：{taskData.taskName}");
        }
    }
}
