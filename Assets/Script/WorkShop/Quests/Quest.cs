using System;
using UnityEngine;

[Serializable]
public enum QuestType
{
    KillEnemy,
    CollectItem,
    EscortNPC
}

[Serializable]
public enum QuestStatus
{
    NotStarted,
    InProgress,
    Completed,
    Turned_In
}

[CreateAssetMenu(fileName = "New Quest", menuName = "Quest System/Quest")]
public class Quest : ScriptableObject
{
    public string questID;
    public string questName;
    public string description;
    
    public QuestType questType;
    public QuestStatus status = QuestStatus.NotStarted;
    
    public string enemyNameToKill;
    public int enemiesToKill;
    public int currentKillCount;
    
    public string itemNameToCollect;
    public int itemsToCollect;
    public int currentCollectCount;
    
    public string npcToEscortName;
    public string escortDestinationNPCName;
    public bool hasFoundNPC = false;
    public bool npcReachedDestination = false;
    
    public string questGiverNPCName;
    public string questTurnInNPCName;
    
    public bool IsComplete()
    {
        switch (questType)
        {
            case QuestType.KillEnemy:
                return currentKillCount >= enemiesToKill;
            case QuestType.CollectItem:
                return currentCollectCount >= itemsToCollect;
            case QuestType.EscortNPC:
                return npcReachedDestination;
            default:
                return false;
        }
    }
    
    public void UpdateProgress(int amount = 1)
    {
        switch (questType)
        {
            case QuestType.KillEnemy:
                currentKillCount += amount;
                if (currentKillCount > enemiesToKill)
                    currentKillCount = enemiesToKill;
                break;
            case QuestType.CollectItem:
                currentCollectCount += amount;
                if (currentCollectCount > itemsToCollect)
                    currentCollectCount = itemsToCollect;
                break;
            case QuestType.EscortNPC:
                break;
        }
        
        if (IsComplete())
        {
            status = QuestStatus.Completed;
        }
    }
    
    public void ResetQuest()
    {
        status = QuestStatus.NotStarted;
        currentKillCount = 0;
        currentCollectCount = 0;
        hasFoundNPC = false;
        npcReachedDestination = false;
    }
}