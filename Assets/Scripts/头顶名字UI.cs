using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 头顶名字UI：挂在人物身上，让其头顶名字牌一直面向相机，玩家离得太远（超过showDistance）就自动隐藏
/// </summary>
public class 头顶名字UI : MonoBehaviour
{
    [Header("人物名字（显示在头顶）")]
    public string characterName = "";

    [Header("名字文本（头顶名字_Canvas下的Text）")]
    public Text nameText;

    [Header("名字牌Canvas（世界空间）")]
    public Canvas nameCanvas;

    [Header("显示距离：玩家与人物超过此距离就隐藏名字")]
    public float showDistance = 15f;

    [Header("名字颜色（默认与对话UI里NPC名字颜色一致）")]
    public Color nameColor = new Color32(0, 222, 255, 255);

    private Transform cam;

    private void Start()
    {
        var main = Camera.main;
        cam = main != null ? main.transform : null;
        RefreshName();
    }

    private void Update()
    {
        if (cam == null) //相机还没出现（场景切换后）就再找一次
        {
            var main = Camera.main;
            if (main == null) return;
            cam = main.transform;
        }
        if (nameCanvas == null) return;

        //距离显隐：相机（玩家）离人物太远就隐藏名字牌
        float dist = Vector3.Distance(cam.position, nameCanvas.transform.position);
        bool show = dist <= showDistance;
        if (nameCanvas.gameObject.activeSelf != show)
            nameCanvas.gameObject.SetActive(show);

        //一直面向相机（与Lookat.cs同款做法）
        if (show)
            nameCanvas.transform.forward = cam.forward;
    }

    private void OnValidate() //在编辑器模式下自动执行验证或初始化逻辑
    {
        RefreshName(); //Inspector里改名字/颜色时实时刷新
    }

    private void RefreshName()
    {
        if (nameText == null) return;
        nameText.text = characterName;
        nameText.color = nameColor;
    }
}
