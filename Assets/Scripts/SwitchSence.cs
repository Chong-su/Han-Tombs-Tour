using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchSence : MonoBehaviour
{
    public int sceneIndex; // 要切换到的场景索引
    public void Switchsence()
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
