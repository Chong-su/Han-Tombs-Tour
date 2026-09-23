using System;
using System.Collections.Generic;
using UnityEngine;

public enum TaskState  //任务状态
{
    Inactive, //未激活
    Active,   //进行中
    Completed, //已完成
    Rewarded  //已领取奖励
}
public enum  CollectType
{
    JiGuan,
    WenWu,
    dialogId,
    kong
}
public enum TaskType  //任务类型
{
    Collect, //收集类任务
    Kill,    //击杀类任务
    AreaExplore, //区域探索类任务
    dialogId,    //对话类任务
    Puzzle   //解谜类任务
}
[Serializable]
public class TaskData //任务数据
{
    public int taskID; //任务ID
    [TextArea]
    public string taskName; //任务名称
    public TaskType taskType; //任务类型（枚举）
    public  CollectType collectType; //收集类型（枚举）
    public int belongChapterID; //所属章节ID
    public List<int> preTaskIds; //前置任务ID列表（全部完成才解锁）
    public List<int> nextTaskIds; //后续任务ID列表（完成后解锁）
    public string finishCondition; //完成条件描述(Text)
    [Header("触发区域类任务")]
    public String Area;//触发区域类任务
}
[Serializable]
public class ChapterData  //章节数据
{
    public int chapterID; //章节ID
    public string chapterName; //章节名称
    public List<int> taskIDs; //章节包含的任务ID列表
    public int nextChapterID; //下一章节ID(-1表示无后续)
    public int rewardStarJade; //完成章节奖励的数量
}
