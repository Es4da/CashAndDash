using UnityEngine;
using System.Collections.Generic;

public class HighlightEffect : MonoBehaviour
{
    public Color outlineColor = Color.white;
    public float outlineThickness = 0.02f; // アウトラインの太さ
    public Material outlineMaterial; // ★追加: アウトライン描画用のマテリアル

    private List<Renderer> targetRenderers = new List<Renderer>();
    private GameObject outlineObject; // アウトライン表示用の複製オブジェクト

    void Start()
    {
        // 自分の子オブジェクトも含めて全てのRendererを取得
        targetRenderers.AddRange(GetComponentsInChildren<Renderer>());

        // アウトライン表示用のオブジェクトを生成
        CreateOutlineObject();
        ToggleHighlight(false); // 最初は非表示
    }

    void CreateOutlineObject()
    {
        outlineObject = new GameObject("Outline_" + gameObject.name);
        outlineObject.transform.SetParent(transform); // 元オブジェクトの子にする
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;

        // 元オブジェクトのメッシュを複製し、アウトライン用のマテリアルを適用
        foreach (Renderer originalRenderer in targetRenderers)
        {
            if (originalRenderer is MeshRenderer meshRenderer)
            {
                // MeshFilterからメッシュを取得
                MeshFilter originalMeshFilter = originalRenderer.GetComponent<MeshFilter>();
                if (originalMeshFilter != null && originalMeshFilter.sharedMesh != null)
                {
                    // 新しい子オブジェクトにMeshFilterとMeshRendererを追加
                    GameObject childOutline = new GameObject(originalRenderer.name + "_OutlineMesh");
                    childOutline.transform.SetParent(outlineObject.transform);
                    childOutline.transform.localPosition = Vector3.zero;
                    childOutline.transform.localRotation = Quaternion.identity;
                    childOutline.transform.localScale = Vector3.one * (1 + outlineThickness); // 少しだけ拡大

                    MeshFilter outlineMeshFilter = childOutline.AddComponent<MeshFilter>();
                    outlineMeshFilter.sharedMesh = originalMeshFilter.sharedMesh;

                    MeshRenderer outlineMeshRenderer = childOutline.AddComponent<MeshRenderer>();
                    outlineMeshRenderer.material = outlineMaterial; // アウトライン専用マテリアルを適用
                }
            }
        }
        outlineObject.SetActive(false); // 最初は非表示
    }

    public void ToggleHighlight(bool active)
    {
        if (outlineObject != null)
        {
            outlineObject.SetActive(active);
        }
    }
}