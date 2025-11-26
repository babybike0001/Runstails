using UnityEngine;

public class Herb : Identity, IInteractable
{
    [Header("Herb Settings")]
    public string herbName = "Herb";
    public GameObject visualModel;
    
    [Header("Interactable")]
    public bool isInteractable { get; set; } = true;
    
    private bool isCollected = false;
    
    public override void SetUP()
    {
        base.SetUP();
        Name = herbName;
    }
    
    public void Interact(Player player)
    {
        if (!isCollected)
        {
            CollectHerb();
        }
    }
    
    private void CollectHerb()
    {
        isCollected = true;
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.UpdateQuestProgress("quest_collect_herbs", herbName, 1);
        }

        if (visualModel != null)
        {
            visualModel.SetActive(false);
        }

        Destroy(gameObject, 0.1f);
    }
}