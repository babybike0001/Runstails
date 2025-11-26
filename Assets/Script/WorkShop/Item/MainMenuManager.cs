using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    
    [Header("Main Menu Music")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] [Range(0f, 1f)] private float menuMusicVolume = 0.5f;
    
    private AudioSource audioSource;
    
    private void Start()
    {
        InitializeButtons();
        SetCursorState();
        SetupAudioSource();
        PlayMainMenuMusic();
    }
    
    private void InitializeButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
    }
    
    private void SetupAudioSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = menuMusicVolume;
    }
    
    private void PlayMainMenuMusic()
    {
        if (mainMenuMusic != null && audioSource != null)
        {
            audioSource.clip = mainMenuMusic;
            audioSource.Play();
        }
    }
    
    private void OnPlayButtonClicked()
    {
        LoadGameScene();
    }
    
    private void OnQuitButtonClicked()
    {
        QuitGame();
    }
    
    private void LoadGameScene()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        
        Time.timeScale = 1f;
        
        SceneManager.LoadScene(gameSceneName);
    }
    
    private void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void SetCursorState()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}