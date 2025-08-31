using UnityEngine;
using UnityEngine.SceneManagement;
// using System.Collections; // ← 不要なので削除してOK

public class MissionGiver : MonoBehaviour
{
    public string sceneNameToLoad = "Mission";

    public void OnInteract()
    {
        Debug.Log("ミッションを受注した！");
        // GameManagerの新しい関数を呼び出す
        GameManager.instance.LoadSceneWithFade(sceneNameToLoad);
    }
}