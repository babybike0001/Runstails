using System;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Character : Identity, Idestoryable
{
    int _health;
    public int health {
        get { return _health; }
        set { _health = Mathf.Clamp(value, 0, _maxHealth); }
    }
    public int maxHealth { get => _maxHealth; set => _maxHealth = value; }
    
    [SerializeField] private int _maxHealth = 100;
    public int Damage = 10;
    public int Deffent = 10;
    public float movementSpeed;
    protected Animator animator;
    protected Rigidbody rb;
    Quaternion newRotation;

    public event Action<Idestoryable> OnDestory;
    
    [SerializeField] protected bool destroyOnDeath = true;

    public override void SetUP()
    {
        base.SetUP();
        health = maxHealth;
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }
    
    public virtual void TakeDamage(int amount) 
    {
        amount = Mathf.Clamp(amount - Deffent, 1, amount);
        health -= amount;
        if (health <= 0) 
        {
            TriggerDeathEvent();
        }
    }
    
    protected virtual void TriggerDeathEvent()
    {
        OnDestory?.Invoke(this);
        
        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
    
    public virtual void Heal(int amount)
    {
        health += amount;
    }
    
    protected virtual void Turn(Vector3 direction)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, Time.deltaTime * 15f);

        if (rb.linearVelocity.magnitude < 0.1f || direction == Vector3.zero) return;
        newRotation = Quaternion.LookRotation(direction);
    }
    
    protected virtual void Move(Vector3 direction)
    {
        rb.linearVelocity = new Vector3(direction.x * movementSpeed, rb.linearVelocity.y, direction.z * movementSpeed);
        animator.SetFloat("Speed", rb.linearVelocity.magnitude);
    }
}