using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 游戏流程配置表，在编辑器右键创建
/// 配置：开场视频 + 机关触发视频与场景映射
/// </summary>
[CreateAssetMenu(fileName = "GameFlowConfig", menuName = "流程系统/游戏流程配置")]
public class GameFlowConfigSO : ScriptableObject
{
    [Header("开场流程")]
    [Tooltip("点击开始游戏后播放的视频")]
    public VideoClip introVideo;
    [Tooltip("开场视频结束后自动加载的场景名")]
    public string introTargetScene = "正式场景"; 

    [Header("机关触发视频列表")]
    [Tooltip("当特定机关被触发时，播放对应视频并跳转场景")]
    public List<JiGuanVideoMapping> jiguanVideoMappings = new List<JiGuanVideoMapping>();
}

/// <summary>
/// 机关→视频→场景 的单条映射
/// </summary>
[Serializable]
public class JiGuanVideoMapping
{
    [Tooltip("机关ID，与任务系统中的ID对应")]
    public int jiguanId;
    [Tooltip("描述（方便策划识别）")]
    public string description;
    [Tooltip("触发后播放的视频")]
    public VideoClip videoClip;
    [Tooltip("视频结束后跳转的场景名（留空则不跳转场景）")]
    public string targetScene;
}