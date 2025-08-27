using UnityEngine;

public class TreasureBox : MonoBehaviour
{
    public int moneyValue = 100; // この宝箱の価値

    // プレイヤーから呼び出される関数
    public void OnInteract()
    {
        // シーン内のGameManagerを探して、そのAddMoney関数を呼び出す
        FindObjectOfType<GameManager>().AddMoney(moneyValue);
        // 自分自身を消す
        Destroy(gameObject);
    }
}