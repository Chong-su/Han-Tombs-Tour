using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI触碰高亮 : MonoBehaviour, IPointerEnterHandler,IPointerExitHandler
{
    private Button button;
    public Sprite open_Im;
    public Sprite close_Im;
    private void Start()
    {
        button = GetComponent<Button>();   
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        button.image.sprite = open_Im;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        button.image.sprite = close_Im;   
    }
}
