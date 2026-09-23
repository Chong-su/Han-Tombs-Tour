using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话配置资产，编辑器右键 Create > 对话系统 > 对话配置文件 创建
/// </summary>
[CreateAssetMenu(fileName = "DialogueConfig", menuName = "对话系统/对话配置文件")]
public class DialogueConfigSo : ScriptableObject
{
    [Header("所有对话列表")]
    public List<DialogueNumber> allDialogueList = new List<DialogueNumber>();

    /// <summary>
    /// 按对话组ID取一整段对话，找不到返回null
    /// </summary>
    public DialogueNumber GetDialogueById(int id)
    {
        return allDialogueList.Find(d => d.dialogueId == id);
    }
}
