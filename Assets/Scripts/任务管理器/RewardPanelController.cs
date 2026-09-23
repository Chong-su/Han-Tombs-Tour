using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 奖励面板：三种来源都可触发（机关解密 / 对话完成 / 区域触发）
/// 淡入→停留（模型自转）→淡出；每获得一个奖励，左下角realdyReward按collectSprites顺序换图
/// 挂在Canvas上（常驻激活）
/// </summary>
public class RewardPanelController : MonoBehaviour
{
    [Serializable]
    public class RewardData
    {
        [Header("来源（三种任填一种即可）")]
        [Tooltip("来源1-机关解密：机关名（与JiGuanData.name一致）")]
        public string fromJiGuanName;
        [Tooltip("来源2-对话完成：对话组ID（-1=不用）")]
        public int fromDialogueId = -1;
        [Tooltip("来源3-区域触发：区域ID（-1=不用）")]
        public int fromAreaId = -1;
        [Header("奖励内容")]
        [Tooltip("面板展示区要换上的模型预制体（留空=保留当前展示模型）")]
        public GameObject modelPrefab;
        [Tooltip("获得此奖励后，左下角UI换成这张图（留空=不换图）")]
        public Sprite collectSprite;
    }

    [Header("UI引用")]
    public GameObject rewardPanel;   //获得玉璧碎片提示_panel
    public GameObject modelHolder;   //展示区模型挂点（相机正对着它的物体）
    public Image realdyReward;       //左下角收集UI（每得一个奖励换一张图）

    [Header("奖励配置")]
    public List<RewardData> rewards = new List<RewardData>();

    [Header("参数")]
    public float fadeTime = 0.5f;    //淡入淡出秒数
    public float showTime = 2.5f;    //停留秒数
    public float rotateSpeed = 30f;  //展示时模型自转速度

    private CanvasGroup panelGroup;  //面板透明度（淡入淡出用）
    private int collectedCount;      //已获得奖励数（运行时重进场景会清零）
    private bool showing;            //面板展示中（防重复触发）

    private void Start()
    {
        if (rewardPanel != null)
        {
            panelGroup = rewardPanel.GetComponent<CanvasGroup>();
            if (panelGroup == null) panelGroup = rewardPanel.AddComponent<CanvasGroup>();
            rewardPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 三种奖励来源，各听各的事件
        EventCenter.AddListener<string>("OnJiGuanSolved", OnJiGuanSolvedHandler);   //机关解密
        EventCenter.AddListener<int>("OnDialogFinished", OnDialogueFinishedHandler); //对话完成
        EventCenter.AddListener<int>("OnPlayerEnterArea", OnPlayerEnterAreaHandler); //区域触发
    }

    private void OnDisable()
    {
        EventCenter.RemoveListener<string>("OnJiGuanSolved", OnJiGuanSolvedHandler);
        EventCenter.RemoveListener<int>("OnDialogFinished", OnDialogueFinishedHandler);
        EventCenter.RemoveListener<int>("OnPlayerEnterArea", OnPlayerEnterAreaHandler);
    }

    // ───── 三种来源的入口，统一转成"找到匹配的奖励就弹面板" ─────

    private void OnJiGuanSolvedHandler(string jiGuanName)
    {
        if (showing) return;
        foreach (var r in rewards)
        {
            if (!string.IsNullOrEmpty(r.fromJiGuanName) && r.fromJiGuanName == jiGuanName)
            {
                StartCoroutine(ShowReward(r));
                if(r.fromJiGuanName == "方形门")
                {
                    EventCenter.Trigger<int>("DuiHua", 2);
                }
                if(r.fromJiGuanName == "拱形门")
                {
                    EventCenter.Trigger<int>("DuiHua", 4);
                }
                return;
            }
        }
    }

    private void OnDialogueFinishedHandler(int dialogueId)
    {
        if (showing) return;
        foreach (var r in rewards)
        {
            if (r.fromDialogueId == dialogueId) //默认-1不会撞上真实ID
            {
                StartCoroutine(ShowReward(r));
                return;
            }
        }
    }

    private void OnPlayerEnterAreaHandler(int areaId)
    {
        if (showing) return;
        foreach (var r in rewards)
        {
            if (r.fromAreaId == areaId)
            {
                StartCoroutine(ShowReward(r));
                return;
            }
        }
    }

    // ───── 面板展示 ─────

    private IEnumerator ShowReward(RewardData reward)
    {
        showing = true;
        if (rewardPanel != null) rewardPanel.SetActive(true);

        // 左下角收集UI换图（奖励条目里配了collectSprite才换）
        if (realdyReward != null && reward.collectSprite != null)
        {
            realdyReward.gameObject.SetActive(true);
            realdyReward.sprite = reward.collectSprite;
        }
        collectedCount++;

        // 替换展示模型（先实例化新的再删旧的，展示区不空窗）
        if (reward.modelPrefab != null && modelHolder != null)
        {
            GameObject newModel = Instantiate(reward.modelPrefab, modelHolder.transform);
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localScale = Vector3.one;
            if (modelHolder.transform.childCount > 1)
                Destroy(modelHolder.transform.GetChild(0).gameObject);
        }

        // 淡入
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            yield return Fade(1f);
        }

        // 停留（期间展示模型自转）
        float t = 0f;
        while (t < showTime)
        {
            t += Time.deltaTime;
            if (modelHolder != null)
                modelHolder.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
            yield return null;
        }

        // 淡出后关闭
        if (panelGroup != null) yield return Fade(0f);
        if (rewardPanel != null) rewardPanel.SetActive(false);
        showing = false;
    }

    private IEnumerator Fade(float target)
    {
        while (!Mathf.Approximately(panelGroup.alpha, target))
        {
            panelGroup.alpha = Mathf.MoveTowards(panelGroup.alpha, target, Time.deltaTime / fadeTime);
            yield return null;
        }
        panelGroup.alpha = target;
    }
}
