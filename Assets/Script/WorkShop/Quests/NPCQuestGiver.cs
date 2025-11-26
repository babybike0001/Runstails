using System.Collections.Generic;
using UnityEngine;

public class NPCQuestGiver : Identity, IInteractable
{
    [Header("Quest Settings")]
    public Quest questToGive;
    public bool canGiveQuest = true;
    public bool canTurnInQuest = false;
    
    [Header("Interactable")]
    public bool isInteractable { get; set; } = true;
    
    [Header("Dialog Settings")]
    public string npcName = "NPC";
    [TextArea(3, 5)]
    public List<string> questOfferDialogs = new List<string>();
    [TextArea(3, 5)]
    public string questCompleteDialog = "Thank you for your help!";
    [TextArea(3, 5)]
    public string questInProgressDialog = "You haven't finished the quest yet.";
    [TextArea(3, 5)]
    public string questAlreadyDoneDialog = "Thank you again for your help!";
    
    [Header("Visual Indicators")]
    public GameObject questAvailableIndicator;
    public GameObject questCompleteIndicator;
    
    private void Start()
    {
        SetUP();
        UpdateIndicators();
        
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += OnQuestStatusChanged;
            QuestManager.Instance.OnQuestCompleted += OnQuestStatusChanged;
            QuestManager.Instance.OnQuestTurnedIn += OnQuestStatusChanged;
        }
    }
    
    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= OnQuestStatusChanged;
            QuestManager.Instance.OnQuestCompleted -= OnQuestStatusChanged;
            QuestManager.Instance.OnQuestTurnedIn -= OnQuestStatusChanged;
        }
    }
    
    private void OnQuestStatusChanged(Quest quest)
    {
        if (quest == questToGive)
        {
            UpdateIndicators();
        }
    }
    
    private void UpdateIndicators()
    {
        if (questToGive == null) return;
        
        if (questAvailableIndicator != null)
        {
            questAvailableIndicator.SetActive(
                canGiveQuest && 
                questToGive.status == QuestStatus.NotStarted
            );
        }
        
        if (questCompleteIndicator != null)
        {
            questCompleteIndicator.SetActive(
                canTurnInQuest && 
                questToGive.status == QuestStatus.Completed
            );
        }
    }
    
    public void Interact(Player player)
    {
        if (questToGive == null || QuestManager.Instance == null || DialogSystem.Instance == null)
        {
            return;
        }
        
        if (canGiveQuest && questToGive.status == QuestStatus.NotStarted)
        {
            DialogSystem.Instance.StartDialog(
                npcName,
                questOfferDialogs,
                showChoice: true,
                onAccept: () => {
                    QuestManager.Instance.StartQuest(questToGive);
                    UpdateIndicators();
                },
                onDecline: () => { }
            );
        }
        else if (canTurnInQuest && questToGive.status == QuestStatus.Completed)
        {
            DialogSystem.Instance.StartDialog(
                npcName,
                new List<string> { questCompleteDialog },
                showChoice: false
            );
            
            QuestManager.Instance.TurnInQuest(questToGive);
            UpdateIndicators();
        }
        else if (questToGive.status == QuestStatus.InProgress)
        {
            DialogSystem.Instance.StartDialog(
                npcName,
                new List<string> { questInProgressDialog },
                showChoice: false
            );
        }
        else if (questToGive.status == QuestStatus.Turned_In)
        {
            DialogSystem.Instance.StartDialog(
                npcName,
                new List<string> { questAlreadyDoneDialog },
                showChoice: false
            );
        }
    }
    
    private void Update()
    {
        UpdateIndicators();
    }
}