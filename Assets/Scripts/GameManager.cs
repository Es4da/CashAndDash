using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static int currentRound = 1;

    [Header("Money Settings")]
    public int currentMoney = 0;
    public int deliveredMoney = 0;
    public int moneyGoal; // ラウンド毎に自動設定

    [Header("UI References (Scene-Specific)")]
    // これらはシーン移動時に自動で検索されます
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

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindSceneUI();

        // ミッションシーンがロードされた時だけ、目標を設定
        if (scene.name == "SampleScene") // あなたのミッションシーンの名前に合わせてください
        {
            SetMissionGoal();
        }
    }

    void FindSceneUI()
    {
        // UI要素を毎回探しに行く
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
    
    void SetMissionGoal()
    {
        switch (currentRound)
        {
            case 1: moneyGoal = 100; break;
            case 2: moneyGoal = 200; break;
            case 3: moneyGoal = 300; break;
            case 4: moneyGoal = 400; break;
            default: moneyGoal = 500; break;
        }
        Debug.Log("Round " + currentRound + " Start! Goal: " + moneyGoal);
    }
    
    private void OnDestroy()
    {
        // イベントの登録を解除
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
        
        currentRound++; // ラウンド数を増やす
        Time.timeScale = 1f;
        SceneManager.LoadScene("Garage");
    }

    public void GameOver()
    {
        Debug.Log("ゲームオーバー！拠点に戻ります...");
        Time.timeScale = 1f;
        // ゲームオーバー時はラウンド数をリセットするなどの処理もここに追加できる
        // currentRound = 1; 
        SceneManager.LoadScene("Garage");
    }

    public void UpdateAllUI()
    {
        if (moneyText != null) moneyText.text = "Carrying: " + currentMoney.ToString();
        if (deliveredMoneyText != null) deliveredMoneyText.text = "Delivered: " + deliveredMoney.ToString();
        
        // PlayerControllerを探してHP更新を依頼する
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.UpdateHealthUI();
        }
    }
}