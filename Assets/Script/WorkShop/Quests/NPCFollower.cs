using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCFollower : Identity, IInteractable
{
    [Header("NPC Info")]
    public string npcName = "NPC4";
    
    [Header("Follow Settings")]
    public float followDistance = 3f;
    public float stopDistance = 2f;
    
    [Header("Quest Settings")]
    public Quest relatedQuest;
    
    [Header("Dialogs")]
    [TextArea(3, 5)] public string beforeQuestDialog = "สวัสดีครับ";
    [TextArea(3, 5)] public string afterRescueDialog = "ขอบคุณที่มาช่วย! ฉันจะตามคุณไป";
    [TextArea(3, 5)] public string followingDialog = "ฉันกำลังตามคุณอยู่นะ";
    
    [Header("Visual Indicator")]
    public GameObject questIndicator;

    private NavMeshAgent agent;
    private Transform playerTransform;
    private bool isFollowing = false;
    
    public bool isInteractable { get; set; } = true;

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void Update()
    {
        if (isFollowing && playerTransform != null)
        {
            FollowPlayer();
        }

        if (isFollowing && IsEscortQuest())
        {
            CheckIfReachedDestination();
        }

        UpdateIndicator();
        HandleDebugInput();
    }

    private void Initialize()
    {
        SetUP();
        SetupNavMeshAgent();
        SubscribeToEvents();
        UpdateIndicator();
    }

    private void SetupNavMeshAgent()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void SubscribeToEvents()
    {
        if (QuestManager.Instance != null && relatedQuest != null)
        {
            QuestManager.Instance.OnQuestTurnedIn += OnQuestTurnedIn;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (QuestManager.Instance != null && relatedQuest != null)
        {
            QuestManager.Instance.OnQuestTurnedIn -= OnQuestTurnedIn;
        }
    }

    public void Interact(Player player)
    {
        if (!ValidateQuest())
        {
            ShowDialog(beforeQuestDialog);
            return;
        }

        if (IsQuestNotStarted())
        {
            ShowDialog(beforeQuestDialog);
        }
        else if (CanStartFollowing())
        {
            StartFollowingPlayer(player.transform);
        }
        else if (isFollowing)
        {
            ShowDialog(followingDialog);
        }
    }

    private bool ValidateQuest()
    {
        return relatedQuest != null;
    }

    private bool IsQuestNotStarted()
    {
        return relatedQuest.status == QuestStatus.NotStarted;
    }

    private bool CanStartFollowing()
    {
        return relatedQuest.status == QuestStatus.InProgress && !relatedQuest.hasFoundNPC;
    }

    private bool IsEscortQuest()
    {
        return relatedQuest != null && relatedQuest.questType == QuestType.EscortNPC;
    }

    private void StartFollowingPlayer(Transform target)
    {
        StartFollowing(target);
        relatedQuest.hasFoundNPC = true;
        ShowDialog(afterRescueDialog);
        NotifyQuestProgress("found");
    }

    private void StartFollowing(Transform target)
    {
        isFollowing = true;
        playerTransform = target;

        if (agent != null)
        {
            agent.stoppingDistance = stopDistance;
        }
    }

    public void StopFollowing()
    {
        isFollowing = false;
        playerTransform = null;

        if (agent != null)
        {
            agent.ResetPath();
        }
    }

    private void FollowPlayer()
    {
        if (agent == null || playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (ShouldFollow(distance))
        {
            agent.SetDestination(playerTransform.position);
        }
        else if (ShouldStop(distance))
        {
            agent.ResetPath();
        }
    }

    private bool ShouldFollow(float distance)
    {
        return distance > followDistance;
    }

    private bool ShouldStop(float distance)
    {
        return distance <= stopDistance;
    }

    private void CheckIfReachedDestination()
    {
        if (relatedQuest.npcReachedDestination) return;

        NPCQuestGiver destinationNPC = FindDestinationNPC();

        if (destinationNPC != null)
        {
            float distance = Vector3.Distance(transform.position, destinationNPC.transform.position);
            LogDistance(distance);

            if (IsCloseEnoughToDestination(distance))
            {
                HandleArrivalAtDestination();
            }
        }
    }

    private NPCQuestGiver FindDestinationNPC()
    {
        return FindNPCByName(relatedQuest.escortDestinationNPCName);
    }

    private void LogDistance(float distance)
    {
    }

    private bool IsCloseEnoughToDestination(float distance)
    {
        return distance <= 5f;
    }

    private void HandleArrivalAtDestination()
    {
        relatedQuest.npcReachedDestination = true;
        relatedQuest.status = QuestStatus.Completed;

        StopFollowing();
        NotifyQuestProgress("completed");
    }

    private NPCQuestGiver FindNPCByName(string targetNPCName)
    {
        NPCQuestGiver[] allNPCs = FindObjectsOfType<NPCQuestGiver>();

        LogNPCSearch(targetNPCName, allNPCs.Length);

        foreach (var npc in allNPCs)
        {
            if (npc.npcName == targetNPCName)
            {
                return npc;
            }
        }

        return null;
    }

    private void LogNPCSearch(string targetNPCName, int totalNPCs)
    {
    }

    private void OnQuestTurnedIn(Quest quest)
    {
        if (quest == relatedQuest)
        {
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    private void NotifyQuestProgress(string status)
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.UpdateEscortQuestProgress(relatedQuest.questID, status);
        }
    }

    private IEnumerator FadeOutAndDestroy()
    {
        yield return new WaitForSeconds(1f);

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float fadeTime = 1f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = CalculateFadeAlpha(elapsed, fadeTime);

            ApplyAlphaToRenderers(renderers, alpha);

            yield return null;
        }

        DestroyNPC();
    }

    private float CalculateFadeAlpha(float elapsed, float fadeTime)
    {
        return 1f - (elapsed / fadeTime);
    }

    private void ApplyAlphaToRenderers(Renderer[] renderers, float alpha)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material.HasProperty("_Color"))
            {
                Color color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
            }
        }
    }

    private void DestroyNPC()
    {
        Destroy(gameObject);
    }

    private void UpdateIndicator()
    {
        if (questIndicator == null || relatedQuest == null) return;

        bool shouldShow = ShouldShowIndicator();
        questIndicator.SetActive(shouldShow);
    }

    private bool ShouldShowIndicator()
    {
        return relatedQuest.status == QuestStatus.InProgress && !relatedQuest.hasFoundNPC;
    }

    private void ShowDialog(string message)
    {
        if (DialogSystem.Instance != null)
        {
            DialogSystem.Instance.StartDialog(
                npcName,
                new System.Collections.Generic.List<string> { message },
                showChoice: false
            );
        }
    }

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.T) && relatedQuest != null)
        {
            LogDebugInfo();
        }
    }

    private void LogDebugInfo()
    {
        if (isFollowing)
        {
            CheckIfReachedDestination();
        }
    }

    private void OnDrawGizmosSelected()
    {
        DrawFollowDistanceGizmo();
        DrawStopDistanceGizmo();
    }

    private void DrawFollowDistanceGizmo()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, followDistance);
    }

    private void DrawStopDistanceGizmo()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}