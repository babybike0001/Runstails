using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    private static QuestManager _instance;
    public static QuestManager Instance => _instance;

    [Header("Quest Data")]
    public List<Quest> allQuests = new List<Quest>();
    public List<Quest> activeQuests = new List<Quest>();
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip questAcceptSound;
    [SerializeField] private AudioClip questCompleteSound;
    [SerializeField] private AudioClip questTurnInSound;
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;

    public delegate void QuestUpdated(Quest quest);
    public event QuestUpdated OnQuestStarted;
    public event QuestUpdated OnQuestProgressUpdated;
    public event QuestUpdated OnQuestCompleted;
    public event QuestUpdated OnQuestTurnedIn;

    private AudioSource audioSource;

    private void Awake()
    {
        InitializeSingleton();
    }
    
    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void InitializeSingleton()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSource();
            ResetAllQuests();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetAllQuests()
    {
        foreach (Quest quest in allQuests) quest.ResetQuest();
        activeQuests.Clear();
    }

    public void StartQuest(Quest quest)
    {
        if (quest.status == QuestStatus.NotStarted)
        {
            quest.status = QuestStatus.InProgress;
            activeQuests.Add(quest);
            PlayQuestAcceptSound();
            OnQuestStarted?.Invoke(quest);
        }
    }

    public void UpdateQuestProgress(string questID, string targetName, int amount = 1)
    {
        Quest quest = activeQuests.FirstOrDefault(q => q.questID == questID && q.status == QuestStatus.InProgress);

        if (quest != null)
        {
            bool shouldUpdate = CheckIfShouldUpdate(quest, targetName);

            if (shouldUpdate)
            {
                quest.UpdateProgress(amount);
                OnQuestProgressUpdated?.Invoke(quest);

                if (quest.IsComplete())
                {
                    PlayQuestCompleteSound();
                    OnQuestCompleted?.Invoke(quest);
                }
            }
        }
    }

    private bool CheckIfShouldUpdate(Quest quest, string targetName)
    {
        if (quest.questType == QuestType.KillEnemy && quest.enemyNameToKill == targetName) return true;
        if (quest.questType == QuestType.CollectItem && quest.itemNameToCollect == targetName) return true;
        return false;
    }

    public void UpdateEscortQuestProgress(string questID, string status)
    {
        Quest quest = activeQuests.FirstOrDefault(q => q.questID == questID && q.questType == QuestType.EscortNPC);

        if (quest != null)
        {
            if (status == "found") HandleEscortFound(quest);
            else if (status == "completed") HandleEscortCompleted(quest);
        }
    }

    private void HandleEscortFound(Quest quest)
    {
        quest.hasFoundNPC = true;
        OnQuestProgressUpdated?.Invoke(quest);
    }

    private void HandleEscortCompleted(Quest quest)
    {
        quest.npcReachedDestination = true;
        quest.status = QuestStatus.Completed;
        PlayQuestCompleteSound();
        OnQuestProgressUpdated?.Invoke(quest);
        OnQuestCompleted?.Invoke(quest);
    }

    public void TurnInQuest(Quest quest)
    {
        if (quest.status == QuestStatus.Completed)
        {
            quest.status = QuestStatus.Turned_In;
            activeQuests.Remove(quest);
            PlayQuestTurnInSound();
            OnQuestTurnedIn?.Invoke(quest);
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = soundVolume;
        }
    }

    private void PlayQuestAcceptSound() => PlaySound(questAcceptSound, "Quest Accept");
    private void PlayQuestCompleteSound() => PlaySound(questCompleteSound, "Quest Complete");
    private void PlayQuestTurnInSound() => PlaySound(questTurnInSound, "Quest Turn In");

    private void PlaySound(AudioClip clip, string soundName)
    {
        if (clip != null)
        {
            EnsureAudioSource();
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    public Quest GetQuestByID(string questID) => allQuests.FirstOrDefault(q => q.questID == questID);
    public bool HasActiveQuest(string questID) => activeQuests.Any(q => q.questID == questID);
}