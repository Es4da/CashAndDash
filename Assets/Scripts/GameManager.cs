using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering; // Rendering関連の機能を使うために必要

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static int currentRound = 1;
    public static int totalScore = 0;

    [Header("Player Stats (Persistent)")] // ★追加
    public int playerMaxHealth = 100;    // ★追加
    public int playerCurrentHealth;

    [Header("Mission Gameplay")]
    public int currentMoney = 0;
    public int deliveredMoney = 0;
    public int moneyGoal;

    [Header("UI References (Auto-Found)")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI deliveredMoneyText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI totalScoreText;
    public CanvasGroup missionCompleteScreenCanvasGroup;
    public CanvasGroup gameOverScreenCanvasGroup;

    [Header("Win/Loss Settings")]
    public float timeSlowdownFactor = 0.2f;
    public float fadeDuration = 1.5f;
    public float waitBeforeReturn = 3.0f;
    public string missionSceneName = "Mission";
    public string hubSceneName = "Garage";

    [Header("Mission Settings")]
    public float initialUnlockDelay = 5f;
    public float subsequentUnlockInterval = 30f;
    private List<TreasureBox> allTreasureBoxes;
    private Coroutine unlockCoroutine;
    [Header("Mission Setup")]
    public GameObject vanPrefab; // ★追加: バンのプレハブ
    private List<Transform> startPoints; // ★追加: スタート地点のリスト

    [Header("Audio")]
    public AudioClip missionCompleteSfx;
    public AudioClip gameOverSfx;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            playerCurrentHealth = playerMaxHealth;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioManager.instance.SetCutsceneMode(false);
        if (scene.name == missionSceneName)
        {
            // ★変更: 宝箱より先にスタート地点を決める
            InitializeStartPoint(); 
            SetMissionGoal();
            InitializeTreasureBoxes();
        }
        FindSceneUI(scene.name);
        DynamicGI.UpdateEnvironment();
    }

    void FindSceneUI(string sceneName)
    {
        if (sceneName == missionSceneName)
        {
            GameObject moneyTextObject = GameObject.Find("MoneyText");
            if (moneyTextObject != null) moneyText = moneyTextObject.GetComponent<TextMeshProUGUI>();

            GameObject deliveredMoneyTextObject = GameObject.Find("DeliveredMoneyText");
            if (deliveredMoneyTextObject != null) deliveredMoneyText = deliveredMoneyTextObject.GetComponent<TextMeshProUGUI>();

            GameObject healthTextObject = GameObject.Find("HP");
            if (healthTextObject != null) healthText = healthTextObject.GetComponent<TextMeshProUGUI>();

            GameObject mcsObject = GameObject.Find("MissionCompleteScreen");
            if (mcsObject != null) missionCompleteScreenCanvasGroup = mcsObject.GetComponent<CanvasGroup>();

            deliveredMoney = 0;
            currentMoney = 0;
            GameObject goScreenObject = GameObject.Find("GameOverScreen"); // ★追加
            if (goScreenObject != null) gameOverScreenCanvasGroup = goScreenObject.GetComponent<CanvasGroup>();
        }
        else if (sceneName == hubSceneName)
        {
            GameObject totalScoreObject = GameObject.Find("TotalScoreText");
            if (totalScoreObject != null) totalScoreText = totalScoreObject.GetComponent<TextMeshProUGUI>();
        }

        UpdateAllUI(sceneName);
    }
    void InitializeStartPoint()
    {
        // "SpawnLocation"という名前が含まれる親オブジェクトを全て探す
        var spawnLocations = GameObject.FindObjectsOfType<GameObject>()
            .Where(g => g.name.Contains("SpawnLocation")).ToList();

        if (spawnLocations.Count > 0)
        {
            // ランダムなスポーングループを選ぶ
            GameObject selectedLocation = spawnLocations[Random.Range(0, spawnLocations.Count)];

            // そのグループの中から、PlayerとVanのスポーン地点を探す
            Transform playerSpawn = selectedLocation.transform.Find("PlayerSpawn");
            Transform vanSpawn = selectedLocation.transform.Find("VanSpawn");

            if (playerSpawn != null && vanSpawn != null)
            {
                // プレイヤーを配置
                PlayerController player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    player.GetComponent<CharacterController>().enabled = false;
                    player.transform.position = playerSpawn.position;
                    player.transform.rotation = playerSpawn.rotation;
                    player.GetComponent<CharacterController>().enabled = true;
                }

                // バンを配置
                if (vanPrefab != null)
                {
                    Instantiate(vanPrefab, vanSpawn.position, vanSpawn.rotation);
                }
            }
        }
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

    void InitializeTreasureBoxes()
    {
        if (unlockCoroutine != null)
        {
            StopCoroutine(unlockCoroutine);
        }
        allTreasureBoxes = FindObjectsOfType<TreasureBox>().ToList();
        foreach (var box in allTreasureBoxes)
        {
            box.SetInactive();
        }
        unlockCoroutine = StartCoroutine(UnlockTreasureBoxesCoroutine());
    }

    private IEnumerator UnlockTreasureBoxesCoroutine()
    {
        yield return new WaitForSeconds(initialUnlockDelay);
        List<TreasureBox> inactiveBoxes = allTreasureBoxes.Where(box => !box.GetComponent<Collider>().enabled).ToList();
        while (inactiveBoxes.Count > 0)
        {
            int randomIndex = Random.Range(0, inactiveBoxes.Count);
            inactiveBoxes[randomIndex].Activate();
            Debug.Log(inactiveBoxes[randomIndex].name + " がアンロックされました！");
            inactiveBoxes.RemoveAt(randomIndex);

            if (inactiveBoxes.Count > 0)
            {
                yield return new WaitForSeconds(subsequentUnlockInterval);
            }
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateAllUI(SceneManager.GetActiveScene().name);
    }

    public void DeliverMoney()
    {
        deliveredMoney += currentMoney;
        currentMoney = 0;
        UpdateAllUI(SceneManager.GetActiveScene().name);

        if (deliveredMoney >= moneyGoal)
        {
            StartCoroutine(WinSequenceCoroutine());
        }
    }
    public void DoHitStop(float duration)
    {
        StartCoroutine(HitStopCoroutine(duration));
    }
    private IEnumerator HitStopCoroutine(float duration)
    {
        Time.timeScale = 0.1f; // 時間をスローに
        yield return new WaitForSecondsRealtime(duration); // 指定時間、現実時間で待つ
        // 勝利演出中でなければ、時間を元に戻す
        if (Time.timeScale < 0.5f) // スロー演出中でないことを確認
        {
             Time.timeScale = 1f;
        }
    }


    private IEnumerator WinSequenceCoroutine()
    {
        Debug.Log("ミッションコンプリート！");
        AudioManager.instance.StopBgm();
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.StopFootsteps();
        }
        AudioManager.instance.SetCutsceneMode(true);
        AudioManager.instance.PlayCutsceneSfx(missionCompleteSfx);
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

        totalScore += deliveredMoney;
        currentRound++;
        Time.timeScale = 1f;
        SceneManager.LoadScene(hubSceneName);
    }

    public void GameOver()
    {
        StartCoroutine(GameOverSequenceCoroutine());
    }

    public void UpdateAllUI(string sceneName)
    {
        if (sceneName == missionSceneName)
        {
            if (moneyText != null) moneyText.text = "Carrying: " + currentMoney.ToString();

            // ★変更点: ノルマ表示を追加
            if (deliveredMoneyText != null) deliveredMoneyText.text = "Delivered: " + deliveredMoney.ToString() + " / " + moneyGoal.ToString();

            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null) player.UpdateHealthUI();
        }
        else if (sceneName == hubSceneName)
        {
            if (totalScoreText != null) totalScoreText.text = "Total Score: " + totalScore.ToString();
        }
    }
    private IEnumerator GameOverSequenceCoroutine()
    {
        Debug.Log("ゲームオーバー！");
        FindObjectOfType<PlayerController>()?.TriggerDeathAnimation(); // プレイヤーに死亡アニメ再生を命令

        AudioManager.instance.SetCutsceneMode(true);
        AudioManager.instance.StopBgm();
        AudioManager.instance.PlayCutsceneSfx(gameOverSfx);

        Time.timeScale = timeSlowdownFactor;

        float timer = 0;
        while (timer < fadeDuration)
        {
            if (gameOverScreenCanvasGroup != null)
            {
                gameOverScreenCanvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeDuration);
            }
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        if (gameOverScreenCanvasGroup != null) gameOverScreenCanvasGroup.alpha = 1;

        yield return new WaitForSecondsRealtime(waitBeforeReturn);

        // 進行状況をリセット
        currentRound = 1;
        totalScore = 0;
        playerCurrentHealth = playerMaxHealth;

        Time.timeScale = 1f;
        SceneManager.LoadScene(hubSceneName);
    }
}