using UnityEngine;

public interface Idestoryable
{
    int health { get; set; }
    int maxHealth { get; set; }
    void TakeDamage(int damageAmount) { 
        health -= damageAmount;
    }
    event System.Action<Idestoryable> OnDestory;
}