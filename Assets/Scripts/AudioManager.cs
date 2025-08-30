using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    public AudioSource sfxSource; // 効果音用のAudioSource
    public AudioSource bgmSource; // BGM用のAudioSource
    [Header("BGM Clips")]
    public AudioClip garageBgm;   // ガレージシーンで流すBGM
    public AudioClip missionBgm;  // ミッションシーンで流すBGM
    public bool isCutsceneMode = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
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

    // ★追加: イベントの登録解除
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // ★追加: シーンがロードされるたびに呼ばれる関数
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 現在のシーン名に応じて再生するBGMを決める
        if (scene.name == "Garage")
        {
            PlayBgm(garageBgm);
        }
        else if (scene.name == "Mission")
        {
            PlayBgm(missionBgm);
        }
    }

    // BGMを再生する内部関数
    private void PlayBgm(AudioClip clip)
    {
        if (clip == null) return;
        
        bgmSource.clip = clip;
        bgmSource.loop = true; // BGMはループ再生
        bgmSource.Play();
    }
    
    // ★追加: BGMを停止する関数
    public void StopBgm()
    {
        bgmSource.Stop();
    }

    // どこからでも効果音を再生するための関数
    public void PlaySfx(AudioClip clip)
    {
        if (isCutsceneMode) return; // 演出中なら、この関数からの音は鳴らさない
        if (clip == null) return; // AudioClipが設定されていなければ鳴らさない
        sfxSource.PlayOneShot(clip);
    }
    public void PlayCutsceneSfx(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
    
    public void SetCutsceneMode(bool enabled)
    {
        isCutsceneMode = enabled;
    }
}