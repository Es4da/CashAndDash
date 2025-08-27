using UnityEngine;

public class Van : MonoBehaviour
{
    // プレイヤーから呼び出される関数
    public void OnInteract()
    {
        // GameManagerの納品機能を呼び出す
        FindObjectOfType<GameManager>().DeliverMoney();
    }
}