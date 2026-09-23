using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Yes_button : MonoBehaviour
{
    private Button button;
    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();    
        button.onClick.AddListener(OnClick);
    }


    private void OnClick()
    {
        GameFlowManager.Instance.LoadeNewScene();

    }

}
