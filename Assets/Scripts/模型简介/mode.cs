using UnityEngine;

public class mode : MonoBehaviour
{
    public GameObject mod;
    public float RotateSpeed = 5f;
    //public float PingyiSpeed = 1f;
    private float SuofangSpeed = 30f;

    private float mouseX;
    private float mouseY;
    private float mouseScroll = 1f;

    public Move m_move;

    private void Start()
    {
        mouseX = mod.gameObject.transform.position.y;
        mouseY = mod.gameObject.transform.position.x;
    }

    private void Update()
    {
        if (m_move.UIopen == true && m_move.isTriggered_WenWu)
        {
            ModelController(mod);
        }

    }
    public void ModelController(GameObject mod)
    {
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
        //旋转
        if (Input.GetMouseButton(0))
        {
            mod.transform.Rotate(-mouseY * RotateSpeed, -mouseX * RotateSpeed, 0, Space.World);
            //mod.transform.Rotate(-Vector3.up*mouseX*RotateSpeed-Vector3.right*mouseY*RotateSpeed, Space.World);
        }
        //平移
/*        if (Input.GetMouseButton(2))
        {
            Vector3 pingyi = new Vector3(-mouseX * PingyiSpeed * Time.deltaTime, mouseY * PingyiSpeed * Time.deltaTime, 0);
            mod.transform.Translate(Vector3.up*mouseY*PingyiSpeed*Time.deltaTime-Vector3.right*mouseX*PingyiSpeed*Time.deltaTime, Space.World);
            mod.transform.Translate(pingyi, Space.World);
            Vector3 vector = mod.transform.position;
            vector.x = Mathf.Clamp(vector.x, -1f, 1f);
            vector.z = Mathf.Clamp(vector.z, -15f, -10f);
            vector.y = Mathf.Clamp(vector.y, 6f, 7.5f);
            mod.transform.position = vector;

        }*/
        //缩放
        mouseScroll += Input.GetAxis("Mouse ScrollWheel") * SuofangSpeed * Time.deltaTime;
        mouseScroll = Mathf.Clamp(mouseScroll, 0.5f, 2.5f);
        mod.transform.localScale = mouseScroll * Vector3.one;
    }
}
