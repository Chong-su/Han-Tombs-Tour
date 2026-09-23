using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景转场：黑屏从中心扩散显现场景
/// 放到目标场景任意 GameObject 上，等待 GameFlowManager 发出事件后播放
/// </summary>
public class SceneTransition : MonoBehaviour
{
    [Header("过渡设置")]
    [Tooltip("过渡持续时间（秒）")]
    public float duration = 1.5f;

    [Tooltip("边缘羽化程度（0=硬边，越大越柔和）")]
    [Range(0f, 0.5f)]
    public float feather = 0.08f;

    [Tooltip("遮罩颜色")]
    public Color maskColor = Color.black;

    private Material transitionMat;
    private GameObject canvasObj;

    
    private void Awake()
    {
        // 先检查 shader 是否可用，不可用则不创建任何 UI
        Shader shader = Shader.Find("UI/TransitionMask");
        if (shader == null)
        {
            Debug.LogError("找不到 UI/TransitionMask Shader！转场功能不可用。");
            enabled = false;
            return;
        }

        // 创建 Canvas
        canvasObj = new GameObject("TransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();
        canvasObj.transform.SetParent(transform);

        // 创建全屏 Image
        GameObject imageObj = new GameObject("TransitionMask");
        Image maskImage = imageObj.AddComponent<Image>();
        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.SetParent(canvasObj.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 创建材质
        transitionMat = new Material(shader);
        transitionMat.SetFloat("_Radius", 0f);
        transitionMat.SetFloat("_Feather", feather);
        transitionMat.SetColor("_Color", maskColor);
        maskImage.material = transitionMat;
        maskImage.color = Color.white;
        maskImage.raycastTarget = false;
    }

    private void OnEnable()// 注册事件监听器
    {
        
        EventCenter.AddListener("HeiPingZhankai", HeiPingZhankai);
        EventCenter.AddListener("HeiPingShousuo", HeiPingShousuo);
        
    }

    private void OnDisable()// 取消事件监听器
    {
        EventCenter.RemoveListener("HeiPingZhankai", HeiPingZhankai);
        EventCenter.RemoveListener("HeiPingShousuo", HeiPingShousuo);
    }
    private void Start()
    {
        if (transitionMat == null) return;
        transitionMat.SetFloat("_Radius", 1f);
    }
    private void HeiPingZhankai()
    {
        StartCoroutine(PlayReveal());
    }
    private void HeiPingShousuo()
    {
        StartCoroutine(FadeToBlack());
    }
    /// <summary>
    /// 从黑屏扩散显现场景
    /// </summary>
    private IEnumerator PlayReveal()
    {
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            // Ease Out Cubic：先快后慢
            t = 1 - Mathf.Pow(1 - t, 3);
            transitionMat.SetFloat("_Radius", t);
            yield return null;
        }

        transitionMat.SetFloat("_Radius", 1f);
        canvasObj.SetActive(false);
    }

    /// <summary>
    /// 从场景收缩变黑（用于离开场景时调用）
    /// </summary>
    public IEnumerator FadeToBlack(float fadeDuration = -1)
    {
        if (canvasObj == null || transitionMat == null) yield break;
        canvasObj.SetActive(true);
        float dur = fadeDuration > 0 ? fadeDuration : duration;
        transitionMat.SetFloat("_Radius", 1f);

        float timer = 0;
        while (timer < dur)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / dur);
            // Ease In Cubic：先慢后快
            t = Mathf.Pow(t, 3);
            transitionMat.SetFloat("_Radius", 1f - t);
            yield return null;
        }

        transitionMat.SetFloat("_Radius", 0f);
    }
}
