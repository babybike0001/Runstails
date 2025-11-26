using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    private static SceneLoader _instance;
    public static SceneLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("SceneLoader");
                _instance = obj.AddComponent<SceneLoader>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }
    
    [Header("Loading Screen (Optional)")]
    [SerializeField] private GameObject loadingScreenPrefab;
    private GameObject currentLoadingScreen;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
    
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }
    
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        ShowLoadingScreen();
        
        yield return null;
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        
        while (asyncLoad.progress < 0.9f)
        {
            float progress = asyncLoad.progress / 0.9f;
            yield return null;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        asyncLoad.allowSceneActivation = true;
        
        HideLoadingScreen();
    }
    
    public void LoadMainMenu()
    {
        LoadScene("MainMenu");
    }
    
    public void RestartCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadScene(currentScene);
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void ShowLoadingScreen()
    {
        if (loadingScreenPrefab != null && currentLoadingScreen == null)
        {
            currentLoadingScreen = Instantiate(loadingScreenPrefab);
            DontDestroyOnLoad(currentLoadingScreen);
        }
    }
    
    private void HideLoadingScreen()
    {
        if (currentLoadingScreen != null)
        {
            Destroy(currentLoadingScreen);
            currentLoadingScreen = null;
        }
    }
}