using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class WenWuManager : MonoBehaviour
{
    public Text m_TiMu;
    public Text m_Jianjie;
    public GameObject m_Model;

    public GameObject JianJieUI;

    [Serializable]
    public class WenWu
    {
        public string name;
        [TextArea]
        public string WenWuJianJie;
        public GameObject Model;
        public int index;
        public int taskID;
        public VideoClip video;

    }

    public WenWu[] WenWuData;

}
