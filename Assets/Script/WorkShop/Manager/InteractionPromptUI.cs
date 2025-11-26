using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    private static InteractionPromptUI _instance;
    public static InteractionPromptUI Instance
    {
        get
        {
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;
    
    [Header("Settings")]
    [SerializeField] private string defaultPromptText = "Press [E] to Interact";
    [SerializeField] private float fadeSpeed = 5f;

    private CanvasGroup canvasGroup;
    private bool shouldShow = false;
    private string currentPromptText;

    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        SetupUI();
    }

    private void Update()
    {
        UpdateFade();
    }

    private void InitializeSingleton()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupUI()
    {
        if (promptPanel != null)
        {
            canvasGroup = promptPanel.GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = promptPanel.AddComponent<CanvasGroup>();
            }
            
            canvasGroup.alpha = 0f;
            promptPanel.SetActive(true);
        }
        
        if (promptText != null)
        {
            promptText.text = defaultPromptText;
        }
        
        currentPromptText = defaultPromptText;
    }

    public void ShowPrompt(string customText = null)
    {
        if (IsDialogActive())
        {
            shouldShow = false;
            return;
        }
        
        shouldShow = true;
        
        if (!string.IsNullOrEmpty(customText))
        {
            currentPromptText = customText;
        }
        else
        {
            currentPromptText = defaultPromptText;
        }
        
        if (promptText != null)
        {
            promptText.text = currentPromptText;
        }
    }

    public void HidePrompt()
    {
        shouldShow = false;
    }

    public void SetPromptText(string text)
    {
        currentPromptText = text;
        if (promptText != null)
        {
            promptText.text = text;
        }
    }
    
    public void ForceHide()
    {
        shouldShow = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void UpdateFade()
    {
        if (canvasGroup == null) return;

        if (IsDialogActive())
        {
            shouldShow = false;
        }

        float targetAlpha = shouldShow ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }
    
    private bool IsDialogActive()
    {
        if (DialogSystem.Instance != null && DialogSystem.Instance.dialogPanel != null)
        {
            return DialogSystem.Instance.dialogPanel.activeSelf;
        }
        return false;
    }
}