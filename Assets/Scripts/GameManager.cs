using UnityEngine;
using TMPro; // TextMeshProを使うために必要

public class GameManager : MonoBehaviour
{
    public int currentMoney = 0; // 現在の所持金
    public TextMeshProUGUI moneyText; // UIテキストへの参照

    void Start()
    {
        // ゲーム開始時にUIを更新
        UpdateMoneyUI();
    }

    // お金を追加する関数（他のスクリプトから呼び出す）
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateMoneyUI();
        Debug.Log("所持金が " + currentMoney + " になった！");
    }

    // UIのテキストを更新する関数
    void UpdateMoneyUI()
    {
        moneyText.text = "Money: " + currentMoney.ToString();
    }
}