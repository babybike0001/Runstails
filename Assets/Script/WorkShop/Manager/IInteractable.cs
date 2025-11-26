using UnityEngine;
using TMPro;
public interface IInteractable
{
    bool isInteractable { get; set; }
    void Interact(Player player);


}
