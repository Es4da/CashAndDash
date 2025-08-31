using UnityEngine;
using UnityEngine.SceneManagement; // ゲーム終了（ビルド時）に必要

public class PauseMenuManager : MonoBehaviour
{
    public GameObject pauseMenuUI; // インスペクターからポーズ画面のPanelを設定
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
    }

    public void QuitGame()
    {
        Debug.Log("ゲームを終了します...");
        Application.Quit(); // ビルドしたゲームを終了する命令
    }
}