using UnityEngine;
using UnityEngine.UI;

public class ObjectiveMarker : MonoBehaviour
{
    public GameObject markerPrefab;
    private Image markerImage;
    private Transform player;
    private Camera mainCamera;
    private Renderer targetRenderer; // ★追加: 宝箱の表示状態を確認するため

    void Start()
    {
        player = FindObjectOfType<PlayerController>().transform;
        mainCamera = Camera.main;
        targetRenderer = GetComponent<Renderer>(); // ★追加

        Canvas canvas = FindObjectOfType<Canvas>();
        if(canvas != null)
        {
            GameObject markerInstance = Instantiate(markerPrefab, canvas.transform);
            markerImage = markerInstance.GetComponent<Image>();
        }
    }

    void LateUpdate() // ★変更: UpdateからLateUpdateへ
    {
        if (markerImage == null || targetRenderer == null) return;

        // ★追加: 宝箱が表示されている時だけマーカーを更新する
        if (targetRenderer.enabled)
        {
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(transform.position);
            if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < Screen.width && screenPoint.y > 0 && screenPoint.y < Screen.height)
            {
                markerImage.enabled = true;
                markerImage.transform.position = screenPoint;
            }
            else
            {
                markerImage.enabled = false;
            }
        }
        else // ★追加: 宝箱が非表示ならマーカーも非表示
        {
            markerImage.enabled = false;
        }
    }

    void OnDestroy()
    {
        if (markerImage != null)
        {
            Destroy(markerImage.gameObject);
        }
    }
}