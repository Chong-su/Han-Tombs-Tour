using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shijiao : MonoBehaviour

{
    [Header("鼠标灵敏度")]
    public float mouseSensitivity = 100f;

    private float xRotation = 0f;

    void Start()
    {
        // 隐藏并锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 获取鼠标上下输入
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 反向旋转（鼠标向上=视角向上）
        xRotation -= mouseY;

        // 限制上下角度，防止翻跟头
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // 应用旋转
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}
