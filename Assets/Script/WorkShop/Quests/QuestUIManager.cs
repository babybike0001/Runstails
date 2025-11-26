using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestUIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject questListPanel;
    public Transform questListContainer;
    public GameObject questItemPrefab;
    
    private float slideInDuration = 0.4f;
    private float slideOutDuration = 0.4f;
    private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private float slideDistance = 400f;
    private float questItemDelay = 0.1f;
    
    private Dictionary<string, QuestUIItem> activeQuestUIItems = new Dictionary<string, QuestUIItem>();
    private RectTransform panelRect;
    private Vector2 originalPosition;
    private Vector2 hiddenPosition;
    private CanvasGroup canvasGroup;
    private Coroutine animationCoroutine;
    private bool isAnimating = false;
    
    private void Start()
    {
        SetupAnimation();
        SubscribeToEvents();
    }
    
    private void Update()
    {
        CheckPanelVisibility();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void CheckPanelVisibility()
    {
        if (questListPanel.activeSelf && activeQuestUIItems.Count == 0 && !isAnimating)
        {
            HidePanelWithAnimation();
        }

        if (!questListPanel.activeSelf && activeQuestUIItems.Count > 0 && !isAnimating)
        {
            ShowPanelWithAnimation();
        }
    }
    
    #region Setup
    private void SetupAnimation()
    {
        if (questListPanel != null)
        {
            panelRect = questListPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                originalPosition = panelRect.anchoredPosition;
                hiddenPosition = originalPosition + new Vector2(slideDistance, 0);
            }
            
            canvasGroup = questListPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = questListPanel.AddComponent<CanvasGroup>();
            }
            
            panelRect.anchoredPosition = hiddenPosition;
            canvasGroup.alpha = 0f;
            questListPanel.SetActive(false);
        }
    }
    
    private void SubscribeToEvents()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += OnQuestStarted;
            QuestManager.Instance.OnQuestProgressUpdated += OnQuestProgressUpdated;
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
            QuestManager.Instance.OnQuestTurnedIn += OnQuestTurnedIn;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= OnQuestStarted;
            QuestManager.Instance.OnQuestProgressUpdated -= OnQuestProgressUpdated;
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
            QuestManager.Instance.OnQuestTurnedIn -= OnQuestTurnedIn;
        }
    }
    #endregion
    
    #region Quest Events
    private void OnQuestStarted(Quest quest)
    {
        CreateQuestUIItem(quest);
        
        if (!questListPanel.activeSelf)
        {
            ShowPanelWithAnimation();
        }
    }
    
    private void OnQuestProgressUpdated(Quest quest)
    {
        UpdateQuestUIItem(quest);
    }
    
    private void OnQuestCompleted(Quest quest)
    {
        UpdateQuestUIItem(quest);
    }
    
    private void OnQuestTurnedIn(Quest quest)
    {
        RemoveQuestUIItem(quest);
    }
    #endregion
    
    #region Quest UI Management
    private void CreateQuestUIItem(Quest quest)
    {
        if (questItemPrefab == null || questListContainer == null)
        {
            return;
        }
        
        GameObject questObj = Instantiate(questItemPrefab, questListContainer);
        QuestUIItem uiItem = questObj.GetComponent<QuestUIItem>();
        
        if (uiItem != null)
        {
            uiItem.SetupQuest(quest);
            activeQuestUIItems[quest.questID] = uiItem;
            StartCoroutine(AnimateQuestItemIn(questObj, activeQuestUIItems.Count - 1));
        }
    }
    
    private void UpdateQuestUIItem(Quest quest)
    {
        if (activeQuestUIItems.ContainsKey(quest.questID))
        {
            activeQuestUIItems[quest.questID].UpdateProgress(quest);
        }
    }
    
    private void RemoveQuestUIItem(Quest quest)
    {
        if (activeQuestUIItems.ContainsKey(quest.questID))
        {
            GameObject itemObj = activeQuestUIItems[quest.questID].gameObject;
            StartCoroutine(AnimateQuestItemOut(itemObj, quest.questID));
        }
    }
    #endregion
    
    #region Panel Animation
    private void ShowPanelWithAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        animationCoroutine = StartCoroutine(SlideInAnimation());
    }
    
    private void HidePanelWithAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        animationCoroutine = StartCoroutine(SlideOutAnimation());
    }
    
    private IEnumerator SlideInAnimation()
    {
        isAnimating = true;
        questListPanel.SetActive(true);
        
        if (panelRect != null && canvasGroup != null)
        {
            panelRect.anchoredPosition = hiddenPosition;
            canvasGroup.alpha = 0f;
            
            float elapsed = 0f;
            
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideInDuration;
                float curveValue = slideInCurve.Evaluate(t);
                
                panelRect.anchoredPosition = Vector2.Lerp(hiddenPosition, originalPosition, curveValue);
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, curveValue);
                
                yield return null;
            }
            
            panelRect.anchoredPosition = originalPosition;
            canvasGroup.alpha = 1f;
        }
        
        isAnimating = false;
    }
    
    private IEnumerator SlideOutAnimation()
    {
        isAnimating = true;
        
        if (panelRect != null && canvasGroup != null)
        {
            Vector2 startPosition = panelRect.anchoredPosition;
            float elapsed = 0f;
            
            while (elapsed < slideOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideOutDuration;
                float curveValue = slideOutCurve.Evaluate(t);
                
                panelRect.anchoredPosition = Vector2.Lerp(startPosition, hiddenPosition, curveValue);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, curveValue);
                
                yield return null;
            }
            
            panelRect.anchoredPosition = hiddenPosition;
            canvasGroup.alpha = 0f;
        }
        
        questListPanel.SetActive(false);
        isAnimating = false;
    }
    #endregion
    
    #region Quest Item Animation
    private IEnumerator AnimateQuestItemIn(GameObject questItem, int index)
    {
        yield return new WaitForSeconds(questItemDelay * index);
        
        RectTransform itemRect = questItem.GetComponent<RectTransform>();
        CanvasGroup itemCanvas = questItem.GetComponent<CanvasGroup>();
        
        if (itemCanvas == null)
        {
            itemCanvas = questItem.AddComponent<CanvasGroup>();
        }
        
        if (itemRect != null && itemCanvas != null)
        {
            Vector2 originalPos = itemRect.anchoredPosition;
            Vector2 startPos = originalPos + new Vector2(200f, 0);
            
            itemRect.anchoredPosition = startPos;
            itemCanvas.alpha = 0f;
            
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveValue = slideInCurve.Evaluate(t);
                
                itemRect.anchoredPosition = Vector2.Lerp(startPos, originalPos, curveValue);
                itemCanvas.alpha = Mathf.Lerp(0f, 1f, curveValue);
                
                yield return null;
            }
            
            itemRect.anchoredPosition = originalPos;
            itemCanvas.alpha = 1f;
        }
    }
    
    private IEnumerator AnimateQuestItemOut(GameObject questItem, string questID)
    {
        RectTransform itemRect = questItem.GetComponent<RectTransform>();
        CanvasGroup itemCanvas = questItem.GetComponent<CanvasGroup>();
        
        if (itemRect != null && itemCanvas != null)
        {
            Vector2 startPos = itemRect.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(200f, 0);
            
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveValue = slideOutCurve.Evaluate(t);
                
                itemRect.anchoredPosition = Vector2.Lerp(startPos, endPos, curveValue);
                itemCanvas.alpha = Mathf.Lerp(1f, 0f, curveValue);
                
                yield return null;
            }
        }
        
        activeQuestUIItems.Remove(questID);
        Destroy(questItem);
        
        if (activeQuestUIItems.Count == 0)
        {
            yield return new WaitForSeconds(0.2f);
            HidePanelWithAnimation();
        }
    }
    #endregion
}