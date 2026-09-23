using System;
using UnityEngine;
using UnityEngine.UI;
public class JiGuan : MonoBehaviour
{
    public Text m_TiWen;//提问
    public GameObject m_Tushi;//图示
    public GameObject JiGuanUI; //机关UI总对象（最总）
    public Button[] m_XuanXiangText;//选项
    public Move m_move;//移动脚本

    public GameObject jiguan;//机关选项总UI
    public GameObject CorrectResult;//正确结果总UI
    public Image ResultImage;//正确结果图片
    public GameObject IncorrectResult;//错误结果总UI
    public Text ResultText;//错误结果文本

    public bool isOptionSelected = false; // 是否已经选择了选项
    public TaskUIController m_taskUIController; // 任务UI控制器引用

    public TaskManager m_taskManager; // 任务管理器引用
    [Serializable]
    public class JiGuanData
    {
        public string name;
        public string TiWen;
        public GameObject Tushi;
        public Button CorrectOption;

        public Sprite CorrectOptionUI;
        public string IncorrectOptionUI;
        public GameObject DestroyGameObject;
        public int taskID;
        [Serializable]
        public class XuanXiangText
        {
            public string Text;
        }
        public XuanXiangText[] xuanXiangTexts;
    }
    public JiGuanData[] jiguandata;

    private void Start()
    {
        foreach (var item in m_XuanXiangText)
        {
            item.onClick.AddListener(() => OnOptionSelected(item));
        }
    }

    public void OnOptionSelected(Button selectedOption)//选项选择事件
    {
        jiguan.SetActive(false);
        isOptionSelected = true;
        foreach (var item in jiguandata)
        {
            if (item.name == m_move.m_name)
            {
                if (selectedOption == item.CorrectOption)
                {
                    ResultImage.sprite = item.CorrectOptionUI;
                    CorrectResult.SetActive(true);
                    EventCenter.Trigger<int, int>("OnItemCollected",m_move.currentTaskId, 1);
                    EventCenter.Trigger<string>("OnJiGuanSolved", item.name); //机关解密完成：小白点消失+奖励面板弹出
                   // EventCenter.Trigger<int>("OnJiGuanTriggered", m_taskManager.CurrentTaskID);可能无用

                    item.DestroyGameObject.SetActive(false);
                    m_move.Keycode.SetActive(false);

                }
                else
                {
                    IncorrectResult.SetActive(true);
                    ResultText.text = item.IncorrectOptionUI;
                }
            }
            
        }
    }

}
