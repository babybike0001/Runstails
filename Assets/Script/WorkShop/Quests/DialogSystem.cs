using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogSystem : MonoBehaviour
{
    private static DialogSystem _instance;
    public static DialogSystem Instance
    {
        get
        {
            return _instance;
        }
    }

    [Header("UI References")]
    public GameObject dialogPanel;
    public TMP_Text npcNameText;
    public TMP_Text dialogText;
    public TMP_Text skipText;
    
    [Header("Choice Panel")]
    public GameObject choicePanel;
    public Button acceptButton;
    public Button declineButton;
    public TMP_Text acceptButtonText;
    public TMP_Text declineButtonText;

    [Header("Settings")]
    public float typingSpeed = 0.05f;
    
    [Header("Animation Settings")]
    [SerializeField] private float slideUpDuration = 0.3f;
    [SerializeField] private float slideDownDuration = 0.3f;
    [SerializeField] private AnimationCurve slideUpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve slideDownCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float slideDistance = 200f;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip dialogOpenSound;
    [SerializeField] private AudioClip dialogCloseSound;
    [SerializeField] private AudioClip textTypeSound;
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;

    private Queue<string> dialogQueue = new Queue<string>();
    private Coroutine typingCoroutine;
    private Coroutine animationCoroutine;
    private string currentFullDialog = "";
    
    private bool isTyping = false;
    private bool dialogFullyDisplayed = false;
    private bool hasChoice = false;
    private bool isAnimating = false;
    
    private Action onAcceptCallback;
    private Action onDeclineCallback;
    
    private RectTransform dialogPanelRect;
    private Vector2 originalPosition;
    private Vector2 hiddenPosition;
    private CanvasGroup canvasGroup;
    
    private AudioSource audioSource;

    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        InitializeUI();
        SetupButtons();
        SetupAnimation();
        SetupAudioSource();
    }

    private void Update()
    {
        HandleInput();
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

    private void InitializeUI()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }

        if (skipText != null)
        {
            skipText.gameObject.SetActive(false);
        }
    }
    
    private void SetupAnimation()
    {
        if (dialogPanel != null)
        {
            dialogPanelRect = dialogPanel.GetComponent<RectTransform>();
            if (dialogPanelRect != null)
            {
                originalPosition = dialogPanelRect.anchoredPosition;
                hiddenPosition = originalPosition - new Vector2(0, slideDistance);
            }
            
            canvasGroup = dialogPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = dialogPanel.AddComponent<CanvasGroup>();
            }
        }
    }
    
    private void SetupAudioSource()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = soundVolume;
    }

    private void SetupButtons()
    {
        SetupAcceptButton();
        SetupDeclineButton();
        SetupButtonTexts();
    }

    private void SetupAcceptButton()
    {
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(OnAcceptClicked);
        }
    }

    private void SetupDeclineButton()
    {
        if (declineButton != null)
        {
            declineButton.onClick.RemoveAllListeners();
            declineButton.onClick.AddListener(OnDeclineClicked);
        }
    }

    private void SetupButtonTexts()
    {
        if (acceptButtonText != null)
        {
            acceptButtonText.text = "Accept [Y]";
        }

        if (declineButtonText != null)
        {
            declineButtonText.text = "Decline [N]";
        }
    }

    private void HandleInput()
    {
        if (!IsDialogActive() || isAnimating) return;

        HandleSpacebarInput();
        HandleChoiceInput();
    }

    private bool IsDialogActive()
    {
        return dialogPanel != null && dialogPanel.activeSelf;
    }

    private void HandleSpacebarInput()
    {
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                CompleteCurrentDialog();
            }
            else if (dialogFullyDisplayed && dialogQueue.Count > 0)
            {
                DisplayNextDialog();
            }
            else if (dialogFullyDisplayed && dialogQueue.Count == 0 && hasChoice && !IsChoicePanelActive())
            {
                ShowChoiceButtons();
            }
            else if (dialogFullyDisplayed && dialogQueue.Count == 0 && !hasChoice)
            {
                EndDialog();
            }
        }
    }

    private void HandleChoiceInput()
    {
        if (!IsChoicePanelActive()) return;

        if (Input.GetKeyDown(KeyCode.Y))
        {
            OnAcceptClicked();
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            OnDeclineClicked();
        }
    }

    private bool IsChoicePanelActive()
    {
        return choicePanel != null && choicePanel.activeSelf;
    }

    public void StartDialog(string npcName, List<string> dialogs, bool showChoice = false, Action onAccept = null, Action onDecline = null)
    {
        SetNPCName(npcName);
        LoadDialogs(dialogs);
        SetupChoiceCallbacks(showChoice, onAccept, onDecline);
        HideChoicePanel();
        ShowSkipText();
        FreezePlayer();
        HideInteractionPrompt();
        
        PlayDialogOpenSound();
        StartSlideUpAnimation();
    }

    private void PlayDialogOpenSound()
    {
        if (dialogOpenSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dialogOpenSound, soundVolume);
        }
    }
    
    private void PlayDialogCloseSound()
    {
        if (dialogCloseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dialogCloseSound, soundVolume);
        }
    }
    
    private void PlayTextTypeSound()
    {
        if (textTypeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(textTypeSound, soundVolume * 0.1f);
        }
    }

    private void StartSlideUpAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        animationCoroutine = StartCoroutine(SlideUpAnimation());
    }
    
    private void StartSlideDownAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        animationCoroutine = StartCoroutine(SlideDownAnimation());
    }
    
    private IEnumerator SlideUpAnimation()
    {
        isAnimating = true;
        
        ShowDialogPanel();
        
        if (dialogPanelRect != null && canvasGroup != null)
        {
            dialogPanelRect.anchoredPosition = hiddenPosition;
            canvasGroup.alpha = 0f;
            
            float elapsed = 0f;
            
            while (elapsed < slideUpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / slideUpDuration;
                float curveValue = slideUpCurve.Evaluate(t);
                
                dialogPanelRect.anchoredPosition = Vector2.Lerp(hiddenPosition, originalPosition, curveValue);
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, curveValue);
                
                yield return null;
            }
            
            dialogPanelRect.anchoredPosition = originalPosition;
            canvasGroup.alpha = 1f;
        }
        
        isAnimating = false;
        DisplayNextDialog();
    }
    
    private IEnumerator SlideDownAnimation()
    {
        isAnimating = true;
        
        if (dialogPanelRect != null && canvasGroup != null)
        {
            float elapsed = 0f;
            Vector2 startPosition = dialogPanelRect.anchoredPosition;
            
            while (elapsed < slideDownDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / slideDownDuration;
                float curveValue = slideDownCurve.Evaluate(t);
                
                dialogPanelRect.anchoredPosition = Vector2.Lerp(startPosition, hiddenPosition, curveValue);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, curveValue);
                
                yield return null;
            }
            
            dialogPanelRect.anchoredPosition = hiddenPosition;
            canvasGroup.alpha = 0f;
        }
        
        HideDialogPanel();
        isAnimating = false;
    }

    private void ShowDialogPanel()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
        }
    }

    private void SetNPCName(string npcName)
    {
        if (npcNameText != null)
        {
            npcNameText.text = npcName;
        }
    }

    private void LoadDialogs(List<string> dialogs)
    {
        dialogQueue.Clear();
        foreach (string dialog in dialogs)
        {
            dialogQueue.Enqueue(dialog);
        }
    }

    private void SetupChoiceCallbacks(bool showChoice, Action onAccept, Action onDecline)
    {
        hasChoice = showChoice;
        onAcceptCallback = onAccept;
        onDeclineCallback = onDecline;
    }

    private void DisplayNextDialog()
    {
        if (dialogQueue.Count == 0)
        {
            HandleEndOfDialogs();
            return;
        }

        string dialog = dialogQueue.Dequeue();
        StartTypingDialog(dialog);
    }

    private void HandleEndOfDialogs()
    {
        if (!hasChoice)
        {
            EndDialog();
        }
    }

    private void StartTypingDialog(string dialog)
    {
        currentFullDialog = dialog;
        dialogFullyDisplayed = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeDialog(dialog));
    }

    private IEnumerator TypeDialog(string dialog)
    {
        isTyping = true;
        dialogText.text = "";

        foreach (char letter in dialog.ToCharArray())
        {
            dialogText.text += letter;
            PlayTextTypeSound();
            
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
        dialogFullyDisplayed = true;
    }

    private void CompleteCurrentDialog()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogText.text = currentFullDialog;
        isTyping = false;
        dialogFullyDisplayed = true;
    }

    private void ShowChoiceButtons()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
        }

        HideSkipText();
        ValidateButtons();
    }

    private void HideChoicePanel()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
    }

    private void ValidateButtons()
    {
    }

    private void OnAcceptClicked()
    {
        onAcceptCallback?.Invoke();
        EndDialog();
    }

    private void OnDeclineClicked()
    {
        onDeclineCallback?.Invoke();
        EndDialog();
    }

    private void ShowSkipText()
    {
        if (skipText != null)
        {
            skipText.gameObject.SetActive(true);
        }
    }

    private void HideSkipText()
    {
        if (skipText != null)
        {
            skipText.gameObject.SetActive(false);
        }
    }

    private void EndDialog()
    {
        PlayDialogCloseSound();
        StartSlideDownAnimation();
        
        ClearDialogData();
        HideSkipText();
        UnfreezePlayer();
    }

    private void HideDialogPanel()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }
    }

    private void ClearDialogData()
    {
        dialogQueue.Clear();
        hasChoice = false;
        onAcceptCallback = null;
        onDeclineCallback = null;
        dialogFullyDisplayed = false;
        currentFullDialog = "";
    }

    private void FreezePlayer()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.SetCanMove(false);
        }
    }

    private void UnfreezePlayer()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.SetCanMove(true);
        }
    }

    private void HideInteractionPrompt()
    {
        if (InteractionPromptUI.Instance != null)
        {
            InteractionPromptUI.Instance.ForceHide();
        }
    }
}