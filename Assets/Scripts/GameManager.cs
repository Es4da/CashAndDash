using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Money Settings")]
    public int currentMoney = 0;
    public int deliveredMoney = 0;
    public int moneyGoal = 500;

    [Header("UI References (Scene-Specific)")]
    // PlayerControllerから直接アクセスできるようにpublicに戻す
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI deliveredMoneyText;
    public TextMeshProUGUI healthText;
    public CanvasGroup missionCompleteScreenCanvasGroup;

    [Header("Win Condition")]
    public float timeSlowdownFactor = 0.2f;
    public float fadeDuration = 1.5f;
    public float waitBeforeReturn = 3.0f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // シーンがロードされるたびに呼ばれる関数
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 新しいシーンでUI要素を毎回探しに行く
        GameObject moneyTextObject = GameObject.Find("MoneyText");
        if (moneyTextObject != null) moneyText = moneyTextObject.GetComponent<TextMeshProUGUI>();

        GameObject deliveredMoneyTextObject = GameObject.Find("DeliveredMoneyText");
        if (deliveredMoneyTextObject != null) deliveredMoneyText = deliveredMoneyTextObject.GetComponent<TextMeshProUGUI>();

        GameObject healthTextObject = GameObject.Find("HP");
        if (healthTextObject != null) healthText = healthTextObject.GetComponent<TextMeshProUGUI>();

        GameObject missionCompleteScreenObject = GameObject.Find("MissionCompleteScreen");
        if (missionCompleteScreenObject != null)
        {
            missionCompleteScreenCanvasGroup = missionCompleteScreenObject.GetComponent<CanvasGroup>();
            if (missionCompleteScreenCanvasGroup != null)
            {
                missionCompleteScreenCanvasGroup.alpha = 0;
            }
        }
        UpdateAllUI();
    }
    
    // このスクリプトが破棄される時に呼ばれる
    private void OnDestroy()
    {
        // イベントの登録を解除（メモリリークを防ぐための作法）
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateAllUI();
    }
    
    public void DeliverMoney()
    {
        deliveredMoney += currentMoney;
        currentMoney = 0;
        UpdateAllUI();
        Debug.Log("納品完了！ 総額: " + deliveredMoney);

        if (deliveredMoney >= moneyGoal)
        {
            StartCoroutine(WinSequenceCoroutine());
        }
    }

    private IEnumerator WinSequenceCoroutine()
    {
        Debug.Log("ミッションコンプリート！");
        Time.timeScale = timeSlowdownFactor;

        float timer = 0;
        while (timer < fadeDuration)
        {
            if (missionCompleteScreenCanvasGroup != null)
            {
                missionCompleteScreenCanvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeDuration);
            }
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        if (missionCompleteScreenCanvasGroup != null) missionCompleteScreenCanvasGroup.alpha = 1;

        yield return new WaitForSecondsRealtime(waitBeforeReturn);
        
        Time.timeScale = 1f;
        SceneManager.LoadScene("Garage");
    }

    public void GameOver()
    {
        Debug.Log("ゲームオーバー！");
        Time.timeScale = 1f; // 時間の停止を解除してからシーンをロードする
        SceneManager.LoadScene("Garage"); // ゲームオーバーになったら拠点に戻る
    }

    // UI更新系をまとめる
    public void UpdateAllUI()
    {
        if (moneyText != null) moneyText.text = "Carrying: " + currentMoney.ToString();
        if (deliveredMoneyText != null) deliveredMoneyText.text = "Delivered: " + deliveredMoney.ToString();
        
        // HP更新はPlayerControllerが自分で行うので、ここからは削除
    }
}