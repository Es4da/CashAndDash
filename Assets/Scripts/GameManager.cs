using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static int currentRound = 1;
    public static int totalScore = 0;

    [Header("Player Stats (Persistent)")]
    public int playerMaxHealth = 100;
    public int playerCurrentHealth;

    [Header("Mission Gameplay")]
    public int currentMoney = 0;
    public int deliveredMoney = 0;
    public int moneyGoal;

    [Header("Settings")]
    public float timeSlowdownFactor = 0.2f;
    public float fadeDuration = 1f;
    public float waitBeforeReturn = 3.0f;
    public string missionSceneName = "Mission";
    public string hubSceneName = "Garage";
    public float initialUnlockDelay = 5f;
    public float subsequentUnlockInterval = 30f;
    public GameObject vanPrefab;
    public AudioClip missionCompleteSfx;
    public AudioClip gameOverSfx;
    
    // UI参照はスクリプト内部で管理（private）
    private TextMeshProUGUI moneyText;
    private TextMeshProUGUI deliveredMoneyText;
    public TextMeshProUGUI healthText;
    private TextMeshProUGUI totalScoreText;
    private CanvasGroup missionCompleteScreenCanvasGroup;
    private CanvasGroup gameOverScreenCanvasGroup;
    private Slider staminaBar;
    private CanvasGroup fadePanelCanvasGroup;

    private List<TreasureBox> allTreasureBoxes;
    private Coroutine unlockCoroutine;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            playerCurrentHealth = playerMaxHealth;
        }
        else
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
    void SetMissionGoal()
{
    switch (currentRound)
    {
        case 1: moneyGoal = 100; break;
        case 2: moneyGoal = 200; break;
        case 3: moneyGoal = 300; break;
        case 4: moneyGoal = 400; break;
        default: moneyGoal = 500; break; // 5ラウンド以上は500
    }
    Debug.Log("Round " + currentRound + " Start! Goal: " + moneyGoal);
}

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ★★★ 修正点 ★★★
        // 1. まず、シーン内のUIを全て探しに行く
        FindSceneUI(scene.name);

        // 2. UIを探した後で、フェードアウトを開始する
        StartCoroutine(Fade(0f));

        // 3. その他の設定を行う
        AudioManager.instance.SetCutsceneMode(false);
        
        if (scene.name == missionSceneName)
        {
            SetMissionGoal();
            InitializeStartPoint();
            InitializeTreasureBoxes();
        }
        
        DynamicGI.UpdateEnvironment();
    }

    void FindSceneUI(string sceneName)
    {
        // まず、どのシーンにも共通で存在する可能性があるものを探す
        fadePanelCanvasGroup = GameObject.Find("FadePanel")?.GetComponent<CanvasGroup>();

        // シーン名に応じて、探す対象を分岐させる
        if (sceneName == missionSceneName)
        {
            // --- Missionシーンにしか存在しないUI ---
            moneyText = GameObject.Find("MoneyText")?.GetComponent<TextMeshProUGUI>();
            deliveredMoneyText = GameObject.Find("DeliveredMoneyText")?.GetComponent<TextMeshProUGUI>();
            healthText = GameObject.Find("HP")?.GetComponent<TextMeshProUGUI>();
            missionCompleteScreenCanvasGroup = GameObject.Find("MissionCompleteScreen")?.GetComponent<CanvasGroup>();
            gameOverScreenCanvasGroup = GameObject.Find("GameOverScreen")?.GetComponent<CanvasGroup>();
            staminaBar = GameObject.Find("StaminaBar")?.GetComponent<Slider>();
            
            // ミッション開始時にスコアをリセット
            deliveredMoney = 0;
            currentMoney = 0;
        }
        else if (sceneName == hubSceneName)
        {
            // --- Garageシーンにしか存在しないUI ---
            totalScoreText = GameObject.Find("TotalScoreText")?.GetComponent<TextMeshProUGUI>();
        }

        // 最後に、見つかったUIだけを更新する
        UpdateAllUI(sceneName);
    }
    
    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        yield return StartCoroutine(Fade(1f)); // フェードイン
        SceneManager.LoadScene(sceneName);
    }

    public IEnumerator Fade(float targetAlpha)
    {
        if (fadePanelCanvasGroup == null)
        {
            Debug.LogWarning("FadePanel in scene could not be found.");
            yield break;
        }

        fadePanelCanvasGroup.gameObject.SetActive(true);
        float startAlpha = fadePanelCanvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        fadePanelCanvasGroup.alpha = targetAlpha;
        if (targetAlpha == 0)
        {
            fadePanelCanvasGroup.gameObject.SetActive(false);
        }
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
    
    private IEnumerator WinSequenceCoroutine()
    {
        Debug.Log("ミッションコンプリート！");
        AudioManager.instance.StopBgm();
        FindObjectOfType<PlayerController>()?.StopFootsteps();
        AudioManager.instance.SetCutsceneMode(true);
        AudioManager.instance.PlayCutsceneSfx(missionCompleteSfx);
        Time.timeScale = timeSlowdownFactor;

        yield return StartCoroutine(Fade(1f, missionCompleteScreenCanvasGroup)); // 画面をフェード

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

    private IEnumerator GameOverSequenceCoroutine()
    {
        Debug.Log("ゲームオーバー！");
        FindObjectOfType<PlayerController>()?.TriggerDeathAnimation();

        AudioManager.instance.SetCutsceneMode(true);
        AudioManager.instance.StopBgm();
        AudioManager.instance.PlayCutsceneSfx(gameOverSfx);
        Time.timeScale = timeSlowdownFactor;

        yield return StartCoroutine(Fade(1f, gameOverScreenCanvasGroup)); // 画面をフェード

        yield return new WaitForSecondsRealtime(waitBeforeReturn);
        
        currentRound = 1;
        totalScore = 0;
        playerCurrentHealth = playerMaxHealth;

        Time.timeScale = 1f;
        SceneManager.LoadScene(hubSceneName);
    }
    
    // フェード処理を、特定のCanvasGroupに対して行えるようにオーバーロード
    private IEnumerator Fade(float targetAlpha, CanvasGroup group)
    {
        if (group == null) yield break;
        group.gameObject.SetActive(true);
        float startAlpha = group.alpha;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }
        group.alpha = targetAlpha;
    }
    
    // --- 以下の関数は、あなたのコードから変更・整理したものです ---
    public void UpdateStaminaUI(float currentStamina, float maxStamina)
    {
        if (staminaBar != null)
        {
            staminaBar.maxValue = maxStamina;
            staminaBar.value = currentStamina;
        }
    }

    public void UpdateAllUI(string sceneName)
    {
        if (sceneName == missionSceneName)
        {
            if (moneyText != null) moneyText.text = "Carrying: " + currentMoney.ToString();
            if (deliveredMoneyText != null) deliveredMoneyText.text = "Delivered: " + deliveredMoney.ToString() + " / " + moneyGoal.ToString();
            
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null) player.UpdateHealthUI();
        }
        else if (sceneName == hubSceneName)
        {
            if (totalScoreText != null) totalScoreText.text = "Total Score: " + totalScore.ToString();
        }
    }
    
    // --- 以下の関数はあなたのコードには無かったものや、整理が必要なものです ---
    void InitializeStartPoint()
    {
        var spawnLocations = GameObject.FindObjectsOfType<GameObject>()
            .Where(g => g.name.Contains("SpawnLocation")).ToList();

        if (spawnLocations.Count > 0)
        {
            GameObject selectedLocation = spawnLocations[Random.Range(0, spawnLocations.Count)];
            Transform playerSpawn = selectedLocation.transform.Find("PlayerSpawn");
            Transform vanSpawn = selectedLocation.transform.Find("VanSpawn");

            if (playerSpawn != null && vanSpawn != null)
            {
                PlayerController player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    player.GetComponent<CharacterController>().enabled = false;
                    player.transform.position = playerSpawn.position;
                    player.transform.rotation = playerSpawn.rotation;
                    player.GetComponent<CharacterController>().enabled = true;
                }

                if (vanPrefab != null)
                {
                    Instantiate(vanPrefab, vanSpawn.position, vanSpawn.rotation);
                }
            }
        }
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
            inactiveBoxes.RemoveAt(randomIndex);
            
            if(inactiveBoxes.Count > 0)
            {
                yield return new WaitForSeconds(subsequentUnlockInterval);
            }
        }
    }

    public void DoHitStop(float duration)
    {
        if(Time.timeScale > 0.5f) // 既にスローの時は実行しない
            StartCoroutine(HitStopCoroutine(duration));
    }
    
    private IEnumerator HitStopCoroutine(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.1f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = originalTimeScale;
    }
}