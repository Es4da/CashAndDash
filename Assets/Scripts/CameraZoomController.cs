using UnityEngine;
using Cinemachine; // Cinemachineをスクリプトから使うために必要

public class CameraZoomController : MonoBehaviour
{
    public float zoomSpeed = 2f;
    public float minZoomRadius = 2f;
    public float maxZoomRadius = 10f;

    private CinemachineFreeLook freeLookCamera;

    void Start()
    {
        // このスクリプトがアタッチされているオブジェクトからCinemachineFreeLookを取得
        freeLookCamera = GetComponent<CinemachineFreeLook>();
    }

    void Update()
    {
        // マウスホイールのスクロール量を取得 (-1から1の値)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        // スクロールされた場合のみ処理
        if (scrollInput != 0)
        {
            // 3つのRigすべてに対して同じ処理を行う
            for (int i = 0; i < 3; i++)
            {
                // 現在のRadiusから、スクロール量に応じた値を引く（奥にスクロールでマイナス値になるため）
                freeLookCamera.m_Orbits[i].m_Radius -= scrollInput * zoomSpeed;

                // Radiusが最小値・最大値を超えないように制限する
                freeLookCamera.m_Orbits[i].m_Radius = Mathf.Clamp(freeLookCamera.m_Orbits[i].m_Radius, minZoomRadius, maxZoomRadius);
            }
        }
    }
}