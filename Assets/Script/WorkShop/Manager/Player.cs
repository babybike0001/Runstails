using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    [Header("Hand setting")]
    public Transform RightHand;
    public Transform LeftHand;

    [Header("Camera Reference")]
    public Transform cameraTransform;

    [Header("Attack Sound")]
    public AudioSource attackSound;
    public AudioClip attackClip;
    public float attackSoundCooldown = 0.45f;
    private float lastAttackSoundTime = 0f;
    
    [Header("Damage Sound")]
    public AudioClip damageSound;
    [Range(0f, 1f)]
    public float damageSoundVolume = 0.7f;
    public float damageSoundCooldown = 1f;
    private float lastDamageSoundTime = 0f;
    private AudioSource damageAudioSource;
    
    [Header("Interaction Settings")]
    [SerializeField] private float interactionCheckInterval = 0.2f;
    
    Vector3 _inputDirection;
    bool _isAttacking = false;
    bool _isInteract = false;

    private bool canMove = true;
    private float lastInteractionCheckTime = 0f;
    private IInteractable currentInteractable = null;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        health = maxHealth;

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        
        SetupDamageAudioSource();
        ResetAnimatorOnStart();
    }
    
    private void ResetAnimatorOnStart()
    {
        if (animator != null)
        {
            animator.SetBool("Attack", false);
            animator.SetFloat("Speed", 0f);
            animator.Play("Idle", 0, 0f);
        }
    }
    
    public void ResetInput()
    {
        _isAttacking = false;
        _isInteract = false;
        _inputDirection = Vector3.zero;
    }
    
    private void SetupDamageAudioSource()
    {
        damageAudioSource = gameObject.AddComponent<AudioSource>();
        damageAudioSource.playOnAwake = false;
        damageAudioSource.spatialBlend = 0f;
        damageAudioSource.volume = damageSoundVolume;
    }
    
    public override void TakeDamage(int amount)
    {
        amount = Mathf.Clamp(amount - Deffent, 1, amount);
        health -= amount;
        
        PlayDamageSound();
        UpdateHealthBar();
        
        if (health <= 0)
        {
            TriggerDeathEvent();
        }
    }
        
    private void PlayDamageSound()
    {
        if (Time.time - lastDamageSoundTime < damageSoundCooldown)
        {
            return;
        }
        
        if (damageSound != null && damageAudioSource != null)
        {
            damageAudioSource.PlayOneShot(damageSound, damageSoundVolume);
            lastDamageSoundTime = Time.time;
        }
    }
    
    private void UpdateHealthBar()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateHealthBar(health, maxHealth);
        }
    }

    public void FixedUpdate()
    {
        if (IsGamePaused())
        {
            StopMovement();
            return;
        }
        
        if (canMove)
        {
            Move(_inputDirection);
            Turn(_inputDirection);
            Attack(_isAttacking);
        }
        else
        {
            StopMovement();
        }
        Interact(_isInteract);
    }
    
    public void Update()
    {
        if (IsGamePaused())
        {
            return;
        }
        
        if (canMove)
        {
            HandleInput();
        }
        else
        {
            _inputDirection = Vector3.zero;
            _isAttacking = false;
        }
        
        CheckForInteractable();
    }
    
    private bool IsGamePaused()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.isGamePaused;
        }
        return false;
    }
    
    private void HandleInput()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        _inputDirection = GetCameraRelativeMovement(x, y);
        
        if (Input.GetMouseButtonDown(0)) 
        {
            _isAttacking = true;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            _isInteract = true;
        }
    }
    
    private Vector3 GetCameraRelativeMovement(float horizontal, float vertical)
    {
        if (cameraTransform == null)
        {
            return new Vector3(horizontal, 0, vertical);
        }
        
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        Vector3 direction = forward * vertical + right * horizontal;
        
        return direction;
    }
    
    public void Attack(bool isAttacking) 
    {
        if (isAttacking) 
        {
            animator.SetTrigger("Attack");

            if (Time.time - lastAttackSoundTime >= attackSoundCooldown)
            {
                if (attackSound != null && attackClip != null)
                {
                    attackSound.PlayOneShot(attackClip);
                    lastAttackSoundTime = Time.time;
                }
            }

            var e = InFront as Idestoryable;
            if (e != null)
            {
                e.TakeDamage(Damage);
            }

            _isAttacking = false;
        }
    }
    
    private void Interact(bool interactable)
    {
        if (interactable)
        {
            IInteractable e = InFront as IInteractable;
            if (e != null) 
            {
                e.Interact(this);
            }
            _isInteract = false;
        }
    }
    
    private void CheckForInteractable()
    {
        if (Time.time - lastInteractionCheckTime < interactionCheckInterval)
        {
            return;
        }
        
        lastInteractionCheckTime = Time.time;
        
        IInteractable interactable = InFront as IInteractable;
        
        if (interactable != null && interactable.isInteractable)
        {
            if (currentInteractable != interactable)
            {
                currentInteractable = interactable;
                ShowInteractionPrompt();
            }
        }
        else
        {
            if (currentInteractable != null)
            {
                currentInteractable = null;
                HideInteractionPrompt();
            }
        }
    }
    
    private void ShowInteractionPrompt()
    {
        if (InteractionPromptUI.Instance != null)
        {
            InteractionPromptUI.Instance.ShowPrompt();
        }
    }
    
    private void HideInteractionPrompt()
    {
        if (InteractionPromptUI.Instance != null)
        {
            InteractionPromptUI.Instance.HidePrompt();
        }
    }
    
    private void StopMovement()
    {
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        rb.angularVelocity = Vector3.zero;
        animator.SetFloat("Speed", 0);
    }
    
    public void SetCanMove(bool value)
    {
        canMove = value;
        
        if (!canMove)
        {
            StopMovement();
        }
    }
    
    public bool CanMove()
    {
        return canMove;
    }
    
    private void OnDisable()
    {
        if (InteractionPromptUI.Instance != null)
        {
            HideInteractionPrompt();
        }
    }
}