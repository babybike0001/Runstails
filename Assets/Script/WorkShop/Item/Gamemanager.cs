using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

public sealed class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    public static GameManager Instance
    {
        get
        {
            return _instance;
        }
    }

    [Header("Game State")]
    public bool isGamePaused = false;

    [Header("UI Game")]
    public GameObject pauseMenuUI;
    public Slider HPBar;
    
    [Header("HP Bar Animation Settings")]
    [SerializeField] private float hpAnimationSpeed = 5f;
    [SerializeField] private AnimationCurve hpAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Pause Menu Buttons")]
    public Button yesButton;
    public Button noButton;

    private float targetHPValue;
    private float currentDisplayHPValue;
    private Coroutine hpAnimationCoroutine;

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
    
    private void Start()
    {
        SetupButtons();
        ValidateUISetup();
        SetupHealthBarAnimation();
    }
    
    private void SetupHealthBarAnimation()
    {
        if (HPBar != null)
        {
            currentDisplayHPValue = HPBar.value;
            targetHPValue = HPBar.value;
        }
    }

    private void ValidateUISetup()
    {
        if (pauseMenuUI != null)
        {
            Canvas canvas = pauseMenuUI.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            }
        }
    }

    private void SetupButtons()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(OnYesButton_ExitToMainMenu);
            yesButton.interactable = true;
            
            Image buttonImage = yesButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = true;
            }
        }
        
        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(OnNoButton_ResumeGame);
            noButton.interactable = true;
            
            Image buttonImage = noButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = true;
            }
        }
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (HPBar != null)
        {
            HPBar.maxValue = maxHealth;
            targetHPValue = currentHealth;
            
            if (hpAnimationCoroutine != null)
            {
                StopCoroutine(hpAnimationCoroutine);
            }
            hpAnimationCoroutine = StartCoroutine(AnimateHealthBar());
        }
    }
    
    private IEnumerator AnimateHealthBar()
    {
        float startValue = currentDisplayHPValue;
        float elapsed = 0f;
        
        float distance = Mathf.Abs(targetHPValue - startValue);
        float duration = distance / (HPBar.maxValue * hpAnimationSpeed);
        duration = Mathf.Max(duration, 0.1f);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float curveValue = hpAnimationCurve.Evaluate(t);
            
            currentDisplayHPValue = Mathf.Lerp(startValue, targetHPValue, curveValue);
            HPBar.value = currentDisplayHPValue;
            
            yield return null;
        }
        
        currentDisplayHPValue = targetHPValue;
        HPBar.value = targetHPValue;
    }

    public void TogglePause()
    {
        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0f : 1f;
        
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(isGamePaused);
        }
        
        if (isGamePaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            EnsureEventSystemActive();
            EnsureCanvasRaycaster();
            
            if (InteractionPromptUI.Instance != null)
            {
                InteractionPromptUI.Instance.ForceHide();
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    private void EnsureEventSystemActive()
    {
        EventSystem eventSystem = EventSystem.current;
        
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
        else
        {
            eventSystem.enabled = true;
        }
    }
    
    private void EnsureCanvasRaycaster()
    {
        if (pauseMenuUI != null)
        {
            Canvas canvas = pauseMenuUI.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                }
                raycaster.enabled = true;
            }
        }
    }
    
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            TogglePause();
        }
        
        if (isGamePaused && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = Input.mousePosition;
                
                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);
                
                foreach (var result in results)
                {
                    if (result.gameObject == yesButton.gameObject)
                    {
                        OnYesButton_ExitToMainMenu();
                        return;
                    }
                    else if (result.gameObject == noButton.gameObject)
                    {
                        OnNoButton_ResumeGame();
                        return;
                    }
                }
            }
        }
        
        if (isGamePaused)
        {
            if (Input.GetKeyDown(KeyCode.Y))
            {
                OnYesButton_ExitToMainMenu();
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                OnNoButton_ResumeGame();
            }
        }
    }

    public void OnYesButton_ExitToMainMenu()
    {
        Time.timeScale = 1f;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        isGamePaused = false;
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        
        StopAllSounds();
        
        SceneManager.sceneLoaded += OnMainMenuLoaded;
        SceneManager.LoadScene("MainMenu");
    }
    
    private void OnMainMenuLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            SceneManager.sceneLoaded -= OnMainMenuLoaded;
            DestroyAllPersistentObjects();
        }
    }
    
    public void OnNoButton_ResumeGame()
    {
        TogglePause();
    }
    
    private void StopAllSounds()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAllAudio();
        }
    }
    
    private void DestroyAllPersistentObjects()
    {
        if (QuestManager.Instance != null)
        {
            Destroy(QuestManager.Instance.gameObject);
        }
        
        if (SoundManager.Instance != null)
        {
            Destroy(SoundManager.Instance.gameObject);
        }
        
        _instance = null;
        Destroy(gameObject);
    }
    
    private void CleanupBeforeSceneChange()
    {
        isGamePaused = false;
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
    }
}