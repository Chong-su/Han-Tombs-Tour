using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class No_button : MonoBehaviour
{
    private Button button_No;
    public Button button_Yes;
    // Start is called before the first frame update
    void Start()
    {
        button_No = GetComponent<Button>();
        button_No.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        RectTransform rectTransform = button_Yes.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(366, -8.5f);
        button_No.gameObject.SetActive(false);
    }
    // Update is called once per frame

}
