using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerArea : MonoBehaviour
{
    public TaskManager m_TaskManager;
    [Header("控制要播放第几个视频")]
    public int jiGuanID;
    public void OnEnable()
    {
        m_TaskManager = GameObject.Find("任务管理器").GetComponent<TaskManager>();
    }

    //public int areaId;

    public bool isTriggerOnce = true; // 是否只触发一次

    private bool _hasTriggered; // 是否已经触发过
    public int taskID;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (isTriggerOnce && _hasTriggered)
            return;

        _hasTriggered = true;

        EventCenter.Trigger("OnJiGuanTriggered", jiGuanID);

        EventCenter.Trigger<int>("OnPlayerEnterArea", taskID);
        // m_TaskManager.TaskControl(m_TaskManager.CurrentTaskID);
        Destroy(gameObject);
        
    }
}
