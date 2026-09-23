using UnityEngine;

/// <summary>
/// NPC巡航控制器：按路径点自动走动（直线巡航，无需NavMesh）
/// 自带：平滑转向、到点停留、循环/折返两种模式、走路动画控制
/// 对话/策问期间自动暂停（NPC站住等你聊完再继续走）
/// 用法：NPC挂本脚本 → 场景建空物体当路径点按顺序拖入waypoints
/// 前提：Animator配好IsWalking参数且取消勾选Apply Root Motion
/// </summary>
[RequireComponent(typeof(Animator))]
public class NpcPatrol : MonoBehaviour
{
    [Header("巡航路径点（空物体，按行走顺序拖入）")]
    public Transform[] waypoints;

    [Header("移动设置")]
    public float moveSpeed = 1.2f;       //移动速度
    public float turnSpeed = 360f;      //转身速度（度/秒）
    public float arriveDistance = 0.3f; //到达路径点的判定距离
    public float waitAtPoint = 1.5f;    //到点后停留秒数（0=不停留）
    public bool pingPong = false;      //false=首尾循环，true=到头折返

    [Header("动画")]
    public string walkParam = "IsWalking"; //Animator里的bool参数名

    private Animator anim;
    private int index;       //当前目标路径点下标
    private bool forward;     //折返模式的行进方向
    private bool paused;      //对话/策问中暂停
    private float waitTimer;  //到点停留倒计时

    private void Awake() { anim = GetComponent<Animator>(); }

    private void OnEnable()
    {
        // 对话/策问开始→NPC站住，结束→继续走（走EventCenter，同项目UI自刷新模式）
        EventCenter.AddListener<int>("OnDialogueStarted", Pause);
        EventCenter.AddListener<int>("OnDialogueEnded", Resume);
        EventCenter.AddListener<int>("OnCeWenStarted", Pause);
        EventCenter.AddListener<int>("OnCeWenEnded", Resume);
    }

    private void OnDisable()
    {
        EventCenter.RemoveListener<int>("OnDialogueStarted", Pause);
        EventCenter.RemoveListener<int>("OnDialogueEnded", Resume);
        EventCenter.RemoveListener<int>("OnCeWenStarted", Pause);
        EventCenter.RemoveListener<int>("OnCeWenEnded", Resume);
    }

    private void Pause(int id)
    {
        paused = true;
        if (anim != null) anim.SetBool(walkParam, false); //站住播待机
    }
    private void Resume(int id) { paused = false; }

    private void Update()
    {
        if (paused || waypoints == null || waypoints.Length == 0) return;

        if (waitTimer > 0) //路径点停留中，播待机
        {
            waitTimer -= Time.deltaTime;
            if (anim != null) anim.SetBool(walkParam, false);
            return;
        }

        Transform target = waypoints[index];
        Vector3 dir = target.position - transform.position;
        dir.y = 0; //只看水平方向

        if (dir.magnitude <= arriveDistance) //到达→停留并选下一个点,magnitude用于计算出向量的长度
        {
            waitTimer = waitAtPoint;
            NextPoint();
            return;
        }

        // 平滑转身面向目标，再朝自己的前方走
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
        transform.position += transform.forward * (moveSpeed * Time.deltaTime);

        if (anim != null) anim.SetBool(walkParam, true); //走路动画
    }

    private void NextPoint()
    {
        if (pingPong)
        {
            if (forward)
            {
                index++;
                if (index >= waypoints.Length) { index = Mathf.Max(0, waypoints.Length - 2); forward = false; }
            }
            else
            {
                index--;
                if (index < 0) { index = Mathf.Min(1, waypoints.Length - 1); forward = true; }
            }
        }
        else index = (index + 1) % waypoints.Length;
    }
}