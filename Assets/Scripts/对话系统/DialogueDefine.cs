using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一组对话：一个dialogueId对应一整段对话（如一段剧情、一次NPC交谈）
/// </summary>
[Serializable]
public class DialogueNumber
{
    public int dialogueId; //对话组ID（与任务系统里Dialog类任务的目标ID对应）

    [Header("组内对话条目")]
    public List<DialogueData> dialogueDatas = new List<DialogueData>();
}

/// <summary>
/// 单条对话数据（默认按列表顺序播放；nextDialogueNum填正数可跳转到对应dialogueNum的条目，做分支用）
/// </summary>
[Serializable]
public class DialogueData
{
    public int dialogueNum;      //对话序号
    public string dialogueName;  //对话人物名称
    [TextArea(2, 4)]
    public string dialogueContent;  //对话内容
    public AudioClip duihuaAudio;   //这句的配音（可选：留空=只显示文字）
    public int nextDialogueNum;  //下一个对话序号（顺序播放填-1，填正数则跳转）
    public Color color;        //人物名称颜色（可选：默认白色）

}
