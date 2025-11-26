using UnityEngine;
using TMPro;

public class QuestUIItem : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_Text questNameText;
    public TMP_Text progressText;
    public TMP_Text statusText;
    
    private Quest currentQuest;
    
    public void SetupQuest(Quest quest)
    {
        currentQuest = quest;
        UpdateUI();
    }
    
    public void UpdateProgress(Quest quest)
    {
        currentQuest = quest;
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (currentQuest == null) return;
        
        if (questNameText != null)
        {
            questNameText.text = currentQuest.questName;
        }
        
        if (progressText != null)
        {
            string progressString = "";
            
            switch (currentQuest.questType)
            {
                case QuestType.KillEnemy:
                    progressString = $"Kill {currentQuest.enemyNameToKill}: {currentQuest.currentKillCount}/{currentQuest.enemiesToKill}";
                    break;
                    
                case QuestType.CollectItem:
                    progressString = $"Collect {currentQuest.itemNameToCollect}: {currentQuest.currentCollectCount}/{currentQuest.itemsToCollect}";
                    break;
                    
                case QuestType.EscortNPC:
                    if (!currentQuest.hasFoundNPC)
                    {
                        progressString = $"Find {currentQuest.npcToEscortName}";
                    }
                    else if (!currentQuest.npcReachedDestination)
                    {
                        progressString = $"Return to {currentQuest.escortDestinationNPCName}";
                    }
                    else
                    {
                        progressString = $"Talk to {currentQuest.escortDestinationNPCName}";
                    }
                    break;
            }
            
            progressText.text = progressString;
        }
        
        if (statusText != null)
        {
            switch (currentQuest.status)
            {
                case QuestStatus.InProgress:
                    statusText.text = currentQuest.IsComplete() ? "<color=black>Complete! กลับไปส่งภารกิจ</color>" : "<color=black>In Progress...</color>";
                    break;
                case QuestStatus.Completed:
                    statusText.text = "<color=green>Complete! Talk to NPC</color>";
                    break;
                default:
                    statusText.text = "";
                    break;
            }
        }
    }
}