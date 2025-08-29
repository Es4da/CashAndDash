using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Money Settings")]
    public int currentMoney = 0;
    public int deliveredMoney = 0;
    public int moneyGoal = 500;

    [Header("UI Settings")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI deliveredMoneyText;
    public TextMeshProUGUI healthText;
    public GameObject missionCompleteScreen;

    void Start()
    {
        missionCompleteScreen.SetActive(false); // 追加: ゲーム開始時にクリア画面を非表示
        Time.timeScale = 1f; // 追加: ゲームの時間を通常速度に
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
        if (deliveredMoney >= moneyGoal)
        {
            WinGame();
        }
    }

    // UIのテキストを更新する関数
    void UpdateMoneyUI()
    {
        moneyText.text = "Carrying: " + currentMoney.ToString();
        deliveredMoneyText.text = "Delivered: " + deliveredMoney.ToString();
    }
    void WinGame()
    {
        Debug.Log("ミッションコンプリート！");
        missionCompleteScreen.SetActive(true); // クリア画面を表示
        Time.timeScale = 0f; // ゲームの時間を停止
        Cursor.lockState = CursorLockMode.None; // カーソルロックを解除
        Cursor.visible = true; // カーソルを表示
    }
    public void GameOver()
    {
        Debug.Log("ゲームオーバー！");
        // 現在アクティブなシーンをもう一度読み込む
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        healthText.text = "HP: " + currentHealth.ToString() + " / " + maxHealth.ToString();
    }
}