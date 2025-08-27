using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public int currentMoney = 0; // 現在プレイヤーが"所持"しているお金
    public int deliveredMoney = 0; // バンに"納品"した総額

    public TextMeshProUGUI moneyText; // 所持金UIへの参照
    public TextMeshProUGUI deliveredMoneyText; // 納品総額UIへの参照（追加）

    void Start()
    {
        UpdateMoneyUI();
    }

    // お金を追加する（拾う）関数
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateMoneyUI();
    }

    // お金を納品する関数（追加）
    public void DeliverMoney()
    {
        deliveredMoney += currentMoney; // 所持金を納品総額に加算
        currentMoney = 0; // 所持金をリセット
        UpdateMoneyUI(); // UIを更新
        Debug.Log("納品完了！ 総額: " + deliveredMoney);
    }

    // UIのテキストを更新する関数
    void UpdateMoneyUI()
    {
        moneyText.text = "Carrying: " + currentMoney.ToString();
        deliveredMoneyText.text = "Delivered: " + deliveredMoney.ToString();
    }
}