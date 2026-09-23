using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    public Button 文物展示_button;
    public Button 文物简介_button;
    public RectTransform 文物简介内容;
    public RectTransform 文物展示内容;

    public Sprite 文物展示_open;
    public Sprite 文物展示_close;

    public Sprite 文物简介_open;
    public Sprite 文物简介_close;

  



    private void Start()
    {
        文物展示_button.onClick.AddListener(文物展示_button_onClick);
        文物简介_button.onClick.AddListener(() => 文物简介_button_onClick());
    }

    private void 文物展示_button_onClick()
    {
        文物展示内容.DOAnchorPos3DX(0, 0.5f);
        文物简介内容.DOAnchorPos3DX(1700, 0.5f);
/*        文物展示_button.image.sprite = 文物展示_open;
        文物简介_button.image.sprite = 文物简介_close;*/
    }
    private void 文物简介_button_onClick()
    {
        文物简介内容.DOAnchorPos3DX(0, 0.5f);
        文物展示内容.DOAnchorPos3DX(-1700, 0.5f);
/*        文物简介_button.image.sprite = 文物简介_open;
        文物展示_button.image.sprite = 文物展示_close;*/
    }
}
