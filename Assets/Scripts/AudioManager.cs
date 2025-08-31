using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    public AudioSource sfxSource; // 効果音用のAudioSource
    public AudioSource bgmSource; // BGM用のAudioSource
    [Header("Audio Mixer")]
    public AudioMixer mainMixer;
    [Header("BGM Clips")]
    public AudioClip garageBgm;   // ガレージシーンで流すBGM
    public AudioClip missionBgm;  // ミッションシーンで流すBGM
    public bool isCutsceneMode = false;

    private Slider bgmSlider;
    private Slider sfxSlider;
    

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
    void Start()
    {
        float bgmVol = PlayerPrefs.GetFloat("BgmVolume", 0.5f);
        SetBgmVolume(bgmVol);
        
        float sfxVol = PlayerPrefs.GetFloat("SfxVolume", 0.5f);
        SetSfxVolume(sfxVol);
    }
    public void SetBgmVolume(float volume)
    {
        // スライダーの値(0.0001～1)を、デシベル(-80～0)に変換してAudioMixerに設定
        mainMixer.SetFloat("BgmVolume", Mathf.Log10(volume) * 20);
        // 設定をPlayerPrefsに保存
        PlayerPrefs.SetFloat("BgmVolume", volume);
    }
    
    // SFX音量を設定する公開関数
    public void SetSfxVolume(float volume)
    {
        mainMixer.SetFloat("SfxVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SfxVolume", volume);
    }
    public void FindAndSetSliders()
    {
        GameObject bgmSliderObject = GameObject.Find("BgmSlider");
        if (bgmSliderObject != null)
        {
            bgmSlider = bgmSliderObject.GetComponent<Slider>();
            bgmSlider.value = PlayerPrefs.GetFloat("BgmVolume", 0.5f);
        }

        GameObject sfxSliderObject = GameObject.Find("SfxSlider");
        if (sfxSliderObject != null)
        {
            sfxSlider = sfxSliderObject.GetComponent<Slider>();
            sfxSlider.value = PlayerPrefs.GetFloat("SfxVolume", 0.5f);
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