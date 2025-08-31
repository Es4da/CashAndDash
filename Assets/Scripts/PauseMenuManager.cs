using UnityEngine;
using UnityEngine.SceneManagement; // ゲーム終了（ビルド時）に必要
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject pauseMenuUI; // インスペクターからポーズ画面のPanelを設定
    public TextMeshProUGUI highScoreText;
    public static bool isPaused = false;

    void Start()
    {
        // 念のため、ゲーム開始時は必ず非表示＆時間は通常通りに
        if(pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        // Escキーが押されたら
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // 時間を通常に戻す
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked; // カーソルをロック
        Cursor.visible = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // 時間を完全に停止
        isPaused = true;
        Cursor.lockState = CursorLockMode.None; // カーソルロックを解除
        Cursor.visible = true;
        // ★追加: ハイスコアを表示する
        if (highScoreText != null)
        {
            // GameManagerが持つ静的な累計スコアを表示
            highScoreText.text = "High Score: " + GameManager.totalScore.ToString();
        }
        
        // ★追加: AudioManagerにスライダーを探して設定するよう命令
        AudioManager.instance.FindAndSetSliders();
    }

    public void QuitGame()
    {
        Debug.Log("ゲームを終了します...");
        Application.Quit(); // ビルドしたゲームを終了する命令
    }
}