using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Sprite open;
    public Sprite close;

    private bool isClicked = false;
    public int index;
    private void Start()
    {
        gameObject.GetComponent<Button>().onClick.AddListener(() => OnClick());
        if(index == 1)
        {
            gameObject.GetComponent<Image>().sprite = open;
            isClicked = true;
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        gameObject.GetComponent<Image>().sprite = open;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isClicked == false)
        {
            gameObject.GetComponent<Image>().sprite = close;
        }
    }
    private void OnClick()
    {
        gameObject.GetComponent<Image>().sprite = open;
        isClicked = true;
        ButtonHighlight[] button = GameObject.FindObjectsByType<ButtonHighlight>(FindObjectsSortMode.None);
        foreach (var item in button)
        {
            if (item != this)
            {
                item.gameObject.GetComponent<Image>().sprite = item.close;
                item.isClicked = false;
            }
        }
    }
}
