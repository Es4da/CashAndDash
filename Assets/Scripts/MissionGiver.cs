using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionGiver : MonoBehaviour
{
    public string sceneNameToLoad = "Mission"; // ここにミッションシーンの名前を入れる

    public void OnInteract()
    {
        Debug.Log("ミッションを受注した！");
        // 指定されたシーンを読み込む
        SceneManager.LoadScene(sceneNameToLoad);
    }
}