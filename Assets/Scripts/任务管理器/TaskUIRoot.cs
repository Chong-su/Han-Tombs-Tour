using UnityEngine;

/// <summary>
/// 任务栏UI常驻脚本 - 跨场景不销毁
/// 挂在与TaskUIController同一个物体上（Canvas/任务栏）
/// </summary>
public class TaskUIRoot : MonoBehaviour
{
    public static TaskUIRoot Instance { get; private set; }

    public static void Mether()
    {
        
    }
    public static int inde;
    private void Awake()
    {
        
        // 单例去重：回到正式场景时销毁新副本，避免出现两份任务栏
        if (Instance != null && Instance != this)
        {
            //Destroy(gameObject);
            return;
        }
        Instance = this;
        
        //DontDestroyOnLoad只对根物体有效，先脱离原Canvas
        /*        transform.SetParent(null);

                 //脱离Canvas后UI不会渲染，补一个和原Canvas一致的配置
                if (GetComponent<Canvas>() == null)
                {
                    Canvas canvas = gameObject.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 10;// 保证盖在其他场景UI上，可按需调

                    UnityEngine.UI.CanvasScaler scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                }*/

        DontDestroyOnLoad(gameObject);
    }
}