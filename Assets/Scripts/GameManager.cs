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
    public bool isPlayerInvincible = false;

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
    private RectTransform tutorialPanel;

    private List<TreasureBox> allTreasureBoxes;
    private Coroutine unlockCoroutine;
    private RectTransform notificationPanel; // ★追加
    private TextMeshProUGUI notificationText; // ★追加

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
            if (currentRound == 1)
            {
                StartCoroutine(ShowTutorialCoroutine());
            }
            isPlayerInvincible = false;
            SetMissionGoal();
            InitializeStartPoint();
            InitializeTreasureBoxes();
        }
        
        DynamicGI.UpdateEnvironment();
    }

    void FindSceneUI(string sceneName)
    {
        // まず、シーンに存在するCanvasを探す
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // Canvas自体が見つからなければ、UIは探せない
            Debug.LogWarning("UI Canvas could not be found in the scene.");
            return;
        }

        // Canvasの子供の中から、必要なUIコンポーネントを探し出す
        // transform.Find()は、非アクティブな子オブジェクトも探すことができる
        if (sceneName == missionSceneName)
        {
            moneyText = canvas.transform.Find("MoneyText")?.GetComponent<TextMeshProUGUI>();
            deliveredMoneyText = canvas.transform.Find("DeliveredMoneyText")?.GetComponent<TextMeshProUGUI>();
            healthText = canvas.transform.Find("HP")?.GetComponent<TextMeshProUGUI>();
            missionCompleteScreenCanvasGroup = canvas.transform.Find("MissionCompleteScreen")?.GetComponent<CanvasGroup>();
            gameOverScreenCanvasGroup = canvas.transform.Find("GameOverScreen")?.GetComponent<CanvasGroup>();
            staminaBar = canvas.transform.Find("StaminaBar")?.GetComponent<Slider>();

            deliveredMoney = 0;
            currentMoney = 0;
            Transform notificationTransform = canvas.transform.Find("NotificationPanel");
            if (notificationTransform != null)
            {
                notificationPanel = notificationTransform.GetComponent<RectTransform>();
                notificationText = notificationPanel.GetComponentInChildren<TextMeshProUGUI>();
                notificationPanel.gameObject.SetActive(false); // 確実に非表示から始める
            }
            Transform tutorialTransform = canvas.transform.Find("TutorialPanel");
            if(tutorialTransform != null)
            {
                tutorialPanel = tutorialTransform.GetComponent<RectTransform>();
                tutorialPanel.gameObject.SetActive(false); // 確実に非表示から始める
            }
        }
        else if (sceneName == hubSceneName)
        {
            totalScoreText = canvas.transform.Find("TotalScoreText")?.GetComponent<TextMeshProUGUI>();
        }

        // FadePanelは両方のシーンにある可能性があるので、ここで探す
        fadePanelCanvasGroup = canvas.transform.Find("FadePanel")?.GetComponent<CanvasGroup>();

        // UIの初期表示を更新
        UpdateAllUI(sceneName);
    }

    public void ShowNotification(string message)
    {
        StartCoroutine(NotificationCoroutine(message));
    }
    private IEnumerator ShowTutorialCoroutine()
    {
        if (tutorialPanel == null) yield break; // パネルが見つからなければ何もしない

        // アニメーション用の座標を設定（インスペクターで調整してください）
        Vector2 offScreenPos = tutorialPanel.anchoredPosition; // 現在の画面外の位置
        Vector2 onScreenPos = new Vector2(-50, offScreenPos.y); // 画面内の表示位置
        float slideDuration = 0.5f;

        // --- 1. スライドイン ---
        tutorialPanel.gameObject.SetActive(true);
        float timer = 0f;
        while(timer < slideDuration)
        {
            tutorialPanel.anchoredPosition = Vector2.Lerp(offScreenPos, onScreenPos, timer / slideDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        tutorialPanel.anchoredPosition = onScreenPos;

        // --- 2. 5秒間表示 ---
        yield return new WaitForSeconds(5f);

        // --- 3. スライドアウト ---
        timer = 0f;
        while(timer < slideDuration)
        {
            tutorialPanel.anchoredPosition = Vector2.Lerp(onScreenPos, offScreenPos, timer / slideDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        tutorialPanel.gameObject.SetActive(false);
    }

    // ★追加: 通知の表示・非表示アニメーションを制御するコルーチン
    private IEnumerator NotificationCoroutine(string message)
    {
        if (notificationPanel == null) yield break;

        // --- 1. スライドイン ---
        notificationPanel.gameObject.SetActive(true);
        notificationText.text = message;

        Vector2 startPos = new Vector2(400, 50); // 画面外右下（仮）
        Vector2 onScreenPos = new Vector2(-50, 50); // 画面内右下（仮）
        float timer = 0f;
        while (timer < 0.5f) // 0.5秒かけてスライドイン
        {
            notificationPanel.anchoredPosition = Vector2.Lerp(startPos, onScreenPos, timer / 0.5f);
            timer += Time.deltaTime;
            yield return null;
        }
        notificationPanel.anchoredPosition = onScreenPos;

        // --- 2. 3秒間表示 ---
        yield return new WaitForSeconds(3f);

        // --- 3. スライドアウト ---
        timer = 0f;
        while (timer < 0.5f) // 0.5秒かけてスライドアウト
        {
            notificationPanel.anchoredPosition = Vector2.Lerp(onScreenPos, startPos, timer / 0.5f);
            timer += Time.deltaTime;
            yield return null;
        }
        notificationPanel.gameObject.SetActive(false);
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
        isPlayerInvincible = true;
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
        isPlayerInvincible = true;
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
            ShowNotification("A New Box Has Unlocked!");
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
        if (!AudioManager.instance.isCutsceneMode)
        {
            Time.timeScale = originalTimeScale;
        }
    }
    
}