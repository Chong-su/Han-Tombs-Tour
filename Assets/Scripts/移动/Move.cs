using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class Move : MonoBehaviour
{
    public static Move Instance { get; private set; } // 单例

    public float Movespeed;
    public float Rotatespeed;

    private float Hor;
    private float Ver;
    private float MoveY;

    private float mouseX;
    private float mouseY;


    private CharacterController m_ch;
    public Camera mainCamera;//主摄像机
    [Tooltip("次摄像机(观察者)")]
    public Camera CiCamera;//次摄像机(观察者)
    public Button close_button_WenWu;//关闭UI文物按钮
    public Button close_button_JiGuan;//关闭UI机关按钮
    public WenWuManager m_WenWu;//文物管理器脚本
    public JiGuan m_JiGuan;//机关脚本
    public mode m_mode;//模型控制脚本
    [HideInInspector]
    public string m_name;//传递参数

    private bool isGround;//判断是否在地上
    private bool isCrouching;//是否处于下蹲状态

    private bool mouseLock;//鼠标锁定状态
    public bool isTriggered_WenWu;//是否触发文物交互
    public bool isTriggered_JiGuan;//是否触发机关交互
    public bool UIopen;//UI是否打开
    private bool dialogueLock;//对话中锁定：不移动、不转视角，但鼠标保持显示

    public float currentMoveSpeed; // 当前移动速度（根据下蹲状态切换）
    private TaskManager m_taskManager;

    [Header("重力与跳跃")]
    public float gravity = -9.81f;
    public float jumpHeight = 3f;
    private Vector3 velocity;//速度向量

    [Header("下蹲设置")]
    public float standHeight = 1.8f; // 站立时CharacterController高度
    public float crouchHeight = 1.0f; // 下蹲时高度
    public Vector3 standCenter = new Vector3(0, 0f, 0); // 站立时中心
    public Vector3 crouchCenter = new Vector3(0, -0.4f, 0); // 下蹲时中心（向下偏移保持脚底着地）
    public float standStepOffset = 0.7f; // 站立时台阶偏移
    public float crouchStepOffset = 0.3f; // 下蹲时台阶偏移（必须 ≤ Height + Radius*2）
    public float standCameraHeight = 0.5f; // 站立时相机高度
    public float crouchCameraHeight = 0.1f; // 下蹲时相机高度
    public float cameraLerpSpeed = 10f; // 相机高度过渡速度
    public float checkRadius = 0.3f; // 起身时检测头顶障碍物的半径
    public float CrouchMoveSpeed;//蹲下时的移动速度

    [Header("加速")]
    public float qulickSpeed;

    [Header("走路晃动")]
    public bool enableHeadBob = true;   //总开关
    public float bobFrequency = 9f;      //晃动快慢（越大越碎步）
    public float bobHeight = 0.05f;     //上下颠动幅度（米）
    public float bobSway = 0.03f;       //左右摆动幅度（米）
    public float bobSprintMul = 1.6f;   //疾跑时幅度放大倍数
    public float bobCrouchMul = 0.4f;   //下蹲时幅度缩小倍数
    public float bobSmooth = 6f;       //起步/停步时晃动的淡入淡出速度

    private float bobTimer;      //步态累计时间
    private float bobWeight;     //晃动权重0~1（起停平滑用）
    private float camBaseHeight; //相机基础高度（与晃动偏移分开算，避免lerp把晃动吃掉）
    private float camBaseX;      //相机初始横向偏移（保留你场景里摆的位置）
    private Vector2 bobOffset;   //当前帧晃动偏移(x=左右, y=上下)

    [Header("脚步声")]
    public AudioClip footstepClip;
    private int lastStepCycle;

    [Header("UI")]
    public GameObject Keycode;//显示当前按键

    public GameObject Light;//手电筒
    public bool isLightOn = false;
    public Image LightImage;//手电筒UI
    public Sprite isOpenLight;
    public Sprite isCloseLight;

    public int currentTaskId;

    [Header("文物介绍视频")]
    public VideoPlayer videoplayer;//视频播放器(拖:文物视频展示物体)
    public float fadeDuration = 0.5f;//视频淡入淡出秒数
    private RawImage videoRawImage;//视频画面(淡入淡出改它的alpha)
    private bool isVideoPlaying;//文物视频播放中(防止重复触发叠播)

    [Header("UI淡入淡出")]
    public float uiFadeDuration = 0.5f;//文物简介UI淡入淡出秒数
    private CanvasGroup jianJieCanvasGroup;//简介UI的CanvasGroup(没有会自动补)
    private CanvasGroup JiGuanCanvasGroup;//机关UI的CanvasGroup(没有会自动补)
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        m_ch = GetComponent<CharacterController>();
        SceneManager.sceneLoaded += OnSceneLoaded;//每次场景加载完成时都会触发的事件
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnEnable()
    {
        // 对话开始→锁操作并显示鼠标；对话结束→恢复鼠标锁定状态（由DialogueSystem广播）
        EventCenter.AddListener<int>("OnDialogueStarted", OnDialogueStarted);
        EventCenter.AddListener<int>("OnDialogueEnded", OnDialogueEnded);
        // 策问开始/结束：与对话共用同一套锁定（锁视角、鼠标显示可点选项）
        EventCenter.AddListener<int>("OnCeWenStarted", OnDialogueStarted);
        EventCenter.AddListener<int>("OnCeWenEnded", OnDialogueEnded);
    }

    private void OnDisable()
    {
        EventCenter.RemoveListener<int>("OnDialogueStarted", OnDialogueStarted);
        EventCenter.RemoveListener<int>("OnDialogueEnded", OnDialogueEnded);
        EventCenter.RemoveListener<int>("OnCeWenStarted", OnDialogueStarted);
        EventCenter.RemoveListener<int>("OnCeWenEnded", OnDialogueEnded);
    }

    private void OnDialogueStarted(int dialogueId)
    {
        dialogueLock = true;
        Cursor.lockState = CursorLockMode.None; //解锁鼠标位置
        Cursor.visible = true;                  //鼠标保持显示
    }

    private void OnDialogueEnded(int dialogueId)
    {
        dialogueLock = false;
        Cursor.lockState = CursorLockMode.Locked; //回到游戏常态：锁定并隐藏鼠标
        Cursor.visible = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject spawn = GameObject.Find("PlayerSpawn");
        if (spawn == null) return;

        // CharacterController 必须先禁用再改位置，否则传送会被碰撞体拦截/覆盖
        m_ch.enabled = false;
        transform.position = spawn.transform.position;
        transform.rotation = spawn.transform.rotation;
        m_ch.enabled = true;

        velocity.y = 0;      // 清掉下落速度
        mouseX = transform.eulerAngles.y; // 关键：你的视角存在mouseX里，不重置的话一动鼠标视角就弹回旧朝向
        mouseY = 0;
    }
    private void Start()
    {

        mainCamera = Camera.main;
        velocity.y = 0;
        currentMoveSpeed = Movespeed;
        // 记录相机初始局部位置，作为晃动叠加的基准
        if (mainCamera != null)
        {
            camBaseHeight = mainCamera.transform.localPosition.y;
            camBaseX = mainCamera.transform.localPosition.x;
        }
        if (GameObject.Find("任务管理器") != null)
        {
            m_taskManager = GameObject.Find("任务管理器").GetComponent<TaskManager>();
        }

        mouseX = gameObject.GetComponent<Transform>().eulerAngles.y;
        // 视频画面RawImage引用(物体初始不激活也能取到组件)
        if (videoplayer != null)
            videoRawImage = videoplayer.GetComponent<RawImage>();
        ///关闭UI按钮 监听事件
        if (close_button_JiGuan != null && close_button_WenWu != null)
        {
            close_button_WenWu.onClick.AddListener(() =>
            {
                StartCoroutine(FadeOutJianJieUI());//简介UI淡出后再隐藏
                UIopen = false;
                Cursor.lockState = CursorLockMode.Locked;
            });


            close_button_JiGuan.onClick.AddListener(() =>
            {
                if (m_JiGuan.isOptionSelected == true)
                {
                    m_JiGuan.CorrectResult.SetActive(false);
                    m_JiGuan.IncorrectResult.SetActive(false);
                    m_JiGuan.isOptionSelected = false;
                    m_JiGuan.jiguan.SetActive(true);
                }
                m_JiGuan.JiGuanUI.SetActive(false);
                UIopen = false;
                Cursor.lockState = CursorLockMode.Locked;
            });
        }
    }
    public void Update()
    {
        isGround = m_ch.isGrounded;//检测是否落地
        //落地时重置垂直速度（防止重力持续叠加导致落地后卡顿）
        if (isGround && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        //视角移动（对话中锁定：不移动、不转视角）
        if (mouseLock == true && UIopen == false && dialogueLock == false)
        {
            Hor = Input.GetAxis("Horizontal");
            Ver = Input.GetAxis("Vertical");

            Vector3 move = new Vector3(Hor * currentMoveSpeed * Time.deltaTime, 0, Ver * currentMoveSpeed * Time.deltaTime);
            m_ch.Move(move.x * transform.right + move.z * transform.forward + velocity * Time.deltaTime);

            mouseX += Input.GetAxis("Mouse X") * Rotatespeed;
            mouseY -= Input.GetAxis("Mouse Y") * Rotatespeed;
            mouseY = Mathf.Clamp(mouseY, -60, 60);
            mainCamera.transform.localEulerAngles = new Vector3(mouseY, 0, -bobOffset.x * 60f * bobSway);
            transform.localEulerAngles = new Vector3(0, mouseX, 0);
        }
        //跳跃（对话中禁止，防止按空格推进对话时人物蹦起来）
        if (Input.GetKey(KeyCode.Space) && isGround && dialogueLock == false)
        {
            //跳跃速度公式：v = √(2 * 重力 * 跳跃高度)（物理公式，保证跳跃高度稳定）
            velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
        }
        velocity.y += gravity * Time.deltaTime;
        // 下蹲
        HandleCrouch();
        // 相机高度平滑过渡
        UpdateCameraHeight();
        // 加速
        JiaSu();

        //交互（对话进行中不响应F，避免和NPC对话抢按键）
        bool dialoguePlaying = DialogueSystem.Instance != null && DialogueSystem.Instance.IsPlaying;
        if (Input.GetKeyDown(KeyCode.F) && Keycode.activeSelf && !dialoguePlaying && UIopen == false)//UI打开时F不响应,防止重复开UI/叠播视频
        {
            if (isTriggered_WenWu)
            {
                if (isVideoPlaying)
                {
                    //视频正在播：忽略这次F，防止叠播
                }
                else if (videoplayer != null && videoplayer.clip != null)
                {
                    // 有介绍视频：先播视频(淡入淡出)，播完再开简介UI
                    isVideoPlaying = true;
                    UIopen = true;                           //视频期间锁玩家操作
                    Cursor.lockState = CursorLockMode.None;  //鼠标保持显示
                    videoplayer.gameObject.SetActive(true);
                    SetVideoAlpha(0f);
                    videoplayer.Prepare();
                    StartCoroutine(PlayWenWuVideo());
                }
                else
                {
                    // 没配视频的文物：简介UI淡入显示（兜底原行为）
                    StartCoroutine(FadeInJianJieUI());
                    EventCenter.Trigger<int, int>("OnItemCollected", currentTaskId, 1);
                    if (m_taskManager != null) Debug.Log(m_taskManager.CurrentTaskID);
                    UIopen = true;                            //只有真的打开了UI才置true
                    Cursor.lockState = CursorLockMode.None;
                }
            }

            else if (isTriggered_JiGuan)
            {
                 //m_JiGuan.JiGuanUI.SetActive(true);
                StartCoroutine(FadeInJiGuanUI());//机关UI淡入显示淡
                UIopen = true;                            //只有真的打开了UI才置true
                Cursor.lockState = CursorLockMode.None;
            }
            Keycode.SetActive(false);
            //修复：原来这里无条件UIopen=true——和NPC对话共用F键时，对话那一下F会把
            //UIopen卡在true且没有任何UI可关，对话结束后视角永久锁死、点击也无法恢复
        }

        ShouDianTong();

        //点击锁定鼠标（对话中点击是"推进对话"，不能在这里锁鼠标）
        if (Input.GetMouseButtonDown(0) && UIopen == false && dialogueLock == false)
        {
            Cursor.lockState = CursorLockMode.Locked;
            mouseLock = true;
        }

        if (Input.GetKey(KeyCode.LeftAlt)) //按住左Alt键解锁鼠标（
        {
            Cursor.lockState = CursorLockMode.None;
            mouseLock = false;
        }
    }

    #region 文物介绍打开流程
    /// <summary>
    /// 文物介绍视频流程：等准备好→播放→开头淡入→播到"还剩fadeDuration"时开始淡出
    /// →视频播完瞬间淡出也正好完成→接着打开文物简介UI
    /// </summary>
    private IEnumerator PlayWenWuVideo()
    {
        while (!videoplayer.isPrepared)
        {
            yield return null;
        }
        if (videoRawImage != null && videoRawImage.texture == null)
            videoRawImage.texture = videoplayer.texture; //APIOnly模式下把画面交给RawImage

        videoplayer.isLooping = false; //必须非循环，否则永远等不到"播完"
        videoplayer.Play();

        // 开头淡入(视频已在播，淡的是RawImage透明度)
        yield return VideoFade(0f, 1f, fadeDuration);

        // 播放中：等到"总时长-淡出时长"那一刻再开始淡出，正好和视频结束同步
        double fadeOutStart = videoplayer.length - fadeDuration;
        while (videoplayer.isPlaying && videoplayer.time < fadeOutStart)
        {
            yield return null;
        }
        yield return VideoFade(1f, 0f, fadeDuration);

        videoplayer.Stop();
        videoplayer.gameObject.SetActive(false);
        isVideoPlaying = false;

        // 接着文物简介UI淡入显示(UIopen在按F时已置true)
        yield return FadeInJianJieUI();
        EventCenter.Trigger<int, int>("OnItemCollected", currentTaskId, 1);
        if (m_taskManager != null) Debug.Log(m_taskManager.CurrentTaskID);
    }

    /// <summary>视频画面透明度渐变（RawImage的alpha，targetCameraAlpha对RawImage显示无效）</summary>
    private IEnumerator VideoFade(float from, float to, float duration)
    {
        if (duration <= 0f || videoRawImage == null)
        {
            SetVideoAlpha(to);
            yield break;
        }
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            SetVideoAlpha(Mathf.Lerp(from, to, time / duration));
            yield return null;
        }
        SetVideoAlpha(to);
    }

    private void SetVideoAlpha(float a)
    {
        if (videoRawImage == null) return;
        Color c = videoRawImage.color;
        c.a = a;
        videoRawImage.color = c;
    }

    // ───── 文物简介UI淡入淡出 ─────

    /// <summary>取简介UI的CanvasGroup，没有会自动补挂</summary>
    private CanvasGroup GetJianJieGroup()
    {
        if (jianJieCanvasGroup == null && m_WenWu != null && m_WenWu.JianJieUI != null)
        {
            jianJieCanvasGroup = m_WenWu.JianJieUI.GetComponent<CanvasGroup>();
            if (jianJieCanvasGroup == null)
                jianJieCanvasGroup = m_WenWu.JianJieUI.AddComponent<CanvasGroup>();
        }
        return jianJieCanvasGroup;
    }

    /// <summary>简介UI淡入显示（视频播完后接续的就是它）</summary>
    private IEnumerator FadeInJianJieUI()
    {
        var g = GetJianJieGroup();
        if (g == null) yield break;
        g.alpha = 0f;
        g.blocksRaycasts = false; //淡入过程中先不挡点击
        m_WenWu.JianJieUI.SetActive(true);
        yield return FadeCanvasGroup(g, 0f, 1f, uiFadeDuration);
        g.blocksRaycasts = true;
    }

    /// <summary>简介UI淡出后再隐藏（关闭按钮调用）</summary>
    private IEnumerator FadeOutJianJieUI()
    {
        var g = GetJianJieGroup();
        if (g == null)
        {
            if (m_WenWu != null && m_WenWu.JianJieUI != null) m_WenWu.JianJieUI.SetActive(false);
            yield break;
        }
        g.blocksRaycasts = false;
        yield return FadeCanvasGroup(g, g.alpha, 0f, uiFadeDuration);
        m_WenWu.JianJieUI.SetActive(false);
    }

    /// <summary>CanvasGroup透明度渐变（整块面板含子物体一起淡）</summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup g, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            g.alpha = to;
            yield break;
        }
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }
        g.alpha = to;
    }
    #endregion

    private CanvasGroup GetJiGuanGroup()
    {
        if (JiGuanCanvasGroup == null && m_JiGuan != null && m_JiGuan.JiGuanUI != null)
        {
            JiGuanCanvasGroup = m_JiGuan.JiGuanUI.GetComponent<CanvasGroup>();
            if (JiGuanCanvasGroup == null)
            {
                JiGuanCanvasGroup = m_JiGuan.JiGuanUI.AddComponent<CanvasGroup>();
            }
        }
        return JiGuanCanvasGroup;
    }
     IEnumerator FadeInJiGuanUI()
    {
        var g = GetJiGuanGroup();
        if (g == null) yield break;
        g.alpha = 0;
        g.blocksRaycasts = false;
        m_JiGuan.JiGuanUI.SetActive(true);
        yield return FadeCanvasGroup(g, 0, 1, uiFadeDuration);
        g.blocksRaycasts = true;
    }

    private void ShouDianTong()//手电筒开关逻辑
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (isLightOn)
            {
                isLightOn = false;
                Light.SetActive(false);
                LightImage.sprite = isCloseLight;

            }
            else if (!isLightOn)
            {
                isLightOn = true;
                Light.SetActive(true);
                LightImage.sprite = isOpenLight;
            }
        }
    }

    private void HandleCrouch() //处理下蹲逻辑
    {
        bool crouchInput = Input.GetKey(KeyCode.LeftControl);
        if (crouchInput && !isCrouching)
        {
            isCrouching = true;
            UpdateControllerSize(crouchHeight, crouchCenter, crouchStepOffset);
            currentMoveSpeed = CrouchMoveSpeed;
        }
        else if (!crouchInput && isCrouching)
        {
            // 松开按键，尝试起身（先检测头顶是否有障碍物）
            if (CanStandUp())
            {
                isCrouching = false;
                UpdateControllerSize(standHeight, standCenter, standStepOffset);
                currentMoveSpeed = Movespeed;
            }
            // 若头顶有障碍物，保持下蹲
        }
    }

    private void UpdateCameraHeight()
    {
        // 相机高度 = 基础高度（站/蹲平滑过渡） + 走路晃动偏移
        float targetHeight = isCrouching ? crouchCameraHeight : standCameraHeight;//根据下蹲状态设置目标相机高度
        camBaseHeight = Mathf.Lerp(camBaseHeight, targetHeight, cameraLerpSpeed * Time.deltaTime);
        UpdateHeadBob();//更新相机晃动
        Vector3 camPos = mainCamera.transform.localPosition;
        camPos.y = camBaseHeight + bobOffset.y;
        camPos.x = camBaseX + bobOffset.x;
        mainCamera.transform.localPosition = camPos;
    }

    /// <summary>
    /// 走路晃动：地面移动时相机上下颠+左右摆
    /// 用CharacterController真实速度判断移动（UI打开/对话锁定时人不动，晃动自动停）
    /// </summary>
    private void UpdateHeadBob()
    {
        if (!enableHeadBob || mainCamera == null) { bobOffset = Vector2.zero; return; }

        // 水平速度（剔除垂直分量）
        Vector3 hVel = m_ch.velocity;
        hVel.y = 0;
        bool moving = isGround && hVel.magnitude > 0.2f;

        // 状态幅度：下蹲缩小、疾跑放大
        float stateMul = 1f;
        if (isCrouching) stateMul = bobCrouchMul;
        else if (currentMoveSpeed > Movespeed + 0.1f) stateMul = bobSprintMul;

        // 起步淡入、停步淡出（晃动幅度不突变）
        bobWeight = Mathf.MoveTowards(bobWeight, moving ? 1f : 0f, bobSmooth * Time.deltaTime);
        if (bobWeight <= 0f) { bobOffset = Vector2.zero; return; }

        // 步态计时：走得越快晃得越快（按常规移速归一化）
        float speedRatio = hVel.magnitude / Mathf.Max(Movespeed, 0.1f);
        bobTimer += Time.deltaTime * bobFrequency * speedRatio;

        // 上下整波、左右半频（垂直晃动频率是水平的两倍，接近真实步行节律）
        float amp = bobWeight * stateMul;
        bobOffset = new Vector2(
            Mathf.Cos(bobTimer * 0.5f) * bobSway * amp,
            Mathf.Sin(bobTimer) * bobHeight * amp);

        //脚步声
        if (moving && bobWeight > 0.5f && footstepClip != null && AudioManager.Instance != null)
        {
            int cycle = Mathf.FloorToInt(bobTimer / Mathf.PI); //每过π=迈一步
            if (cycle != lastStepCycle)
            {
                lastStepCycle = cycle;
                AudioManager.Instance.PlaySfx(footstepClip);
            }
        }
    }
    /// <summary>
    /// 更新CharacterController的高度和中心
    /// </summary>
    private void UpdateControllerSize(float height, Vector3 center, float stepOffset)
    {
        m_ch.height = height;
        m_ch.center = center;
        m_ch.stepOffset = stepOffset;
    }

    private bool CanStandUp()//检测起身时头顶是否有障碍物
    {
        // 胶囊体检测：从下蹲头顶到站立头顶的范围
        Vector3 crouchTop = transform.position + crouchCenter + new Vector3(0, crouchHeight * 0.5f, 0);// 下蹲时头顶位置,这里transform.position是角色的脚底（相对于CharacterController）
        Vector3 standTop = transform.position + standCenter + new Vector3(0, standHeight * 0.5f, 0);// 站立时头顶位置

        // 动态获取玩家所在层并排除，避免层名不匹配导致检测失败
        int playerLayerMask = 1 << gameObject.layer;//<<是左移运算符，将1左移gameObject.layer位，得到玩家所在层的掩码
        Collider[] hits = Physics.OverlapCapsule(
            crouchTop,//胶囊体顶部球心
             standTop, //胶囊体底部球心
           checkRadius, //胶囊体半径
           ~playerLayerMask, //排除玩家所在层以外的层,~是按位取反运算符，将所有层都排除，只检测玩家所在层以外的层
           QueryTriggerInteraction.Ignore//忽略触发器碰撞体
           );//OverlapCapsule 会在 crouchTop 到 standTop 之间形成一个胶囊体，检测这个范围内有没有碰撞体。
        return hits.Length == 0;// 若没有碰撞到任何物体，则可以起身
    }

    private void JiaSu()//处理加速逻辑
    {
        if (Input.GetKey(KeyCode.LeftShift) && (Hor != 0 || Ver != 0) && !isCrouching)
        {
            currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, 7f, qulickSpeed * Time.deltaTime);
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 80f, 10f * Time.deltaTime);
        }
        else
        {
            float targetSpeed = isCrouching ? CrouchMoveSpeed : Movespeed;
            currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetSpeed, qulickSpeed * Time.deltaTime);
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 60f, 10f * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other) //处理触发交互逻辑
    {

        if (other.gameObject.tag != "Untagged")
        {
            Keycode.SetActive(true);
        }

        if (other.gameObject.tag == "WenWu")
        {
            isTriggered_WenWu = true;

            foreach (var item in m_WenWu.WenWuData)
            {
                if (other.name == item.name)
                {
                    videoplayer.clip = item.video; //直接赋值(没配视频时为null,防止误放上一个文物的视频)
                    m_WenWu.m_TiMu.text = item.name;
                    m_WenWu.m_Jianjie.text = item.WenWuJianJie;
                    currentTaskId = item.taskID;
                    Destroy(m_WenWu.m_Model.transform.GetChild(0).gameObject);
                    //Vector3 offest = m_WenWu.m_Model.transform.position - CiCamera.transform.position;
                    if (item.index == 0)
                    {
                        GameObject newModel = Instantiate(item.Model, m_WenWu.m_Model.transform.position, item.Model.transform.rotation, m_WenWu.m_Model.transform);
                        m_mode.mod = newModel;
                    }
                    else if (item.index == 1)
                    {
                        GameObject newModel = Instantiate(item.Model, m_WenWu.m_Model.transform.position + new Vector3(2, 0, 3), item.Model.transform.rotation, m_WenWu.m_Model.transform);
                        m_mode.mod = newModel;
                    }
                }
            }
        }
        else if (other.gameObject.tag == "JiGuan")
        {
            isTriggered_JiGuan = true;
            foreach (var item in m_JiGuan.jiguandata)
            {
                if (other.name == item.name)
                {
                    m_JiGuan.m_TiWen.text = item.TiWen;
                    currentTaskId = item.taskID;
                    Destroy(m_JiGuan.m_Tushi.transform.GetChild(0).gameObject);
                    GameObject newModel = Instantiate(item.Tushi, m_JiGuan.m_Tushi.transform);
                    m_JiGuan.ResultImage.sprite = item.CorrectOptionUI;
                    m_JiGuan.ResultText.text = item.IncorrectOptionUI;
                    m_name = item.name;
                    foreach (var xuanxiang in m_JiGuan.m_XuanXiangText)
                    {
                        xuanxiang.GetComponentInChildren<Text>().text = item.xuanXiangTexts[System.Array.IndexOf(m_JiGuan.m_XuanXiangText, xuanxiang)].Text;
                    }

                }
            }
        }

    }

    private void OnTriggerExit(Collider other)
    {
        Keycode.SetActive(false);
        isTriggered_WenWu = false;
        isTriggered_JiGuan = false;
    }


}
