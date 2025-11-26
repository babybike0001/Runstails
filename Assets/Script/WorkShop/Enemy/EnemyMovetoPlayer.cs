using Unity.VisualScripting;
using UnityEngine;
using System.Collections;

public class EnemyMovetoPlayer : Character
{
    #region Inspector Fields
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float chaseRange = 10f;
    [SerializeField] private float TimeToAttack = 1f;
    
    [Header("Leash Settings")]
    [SerializeField] private bool useLeash = true;
    [SerializeField] private bool healOnReturn = true;
    [SerializeField] private float leashDistance = 20f;
    [SerializeField] private float returnSpeed = 1.5f;
    [SerializeField] private float returnThreshold = 2f;
    
    [Header("Death Animation Settings")]
    [SerializeField] private bool useDeathAnimation = true;
    [SerializeField] private string deathAnimationName = "Death";
    [SerializeField] private DeathTriggerType triggerType = DeathTriggerType.Bool;
    [SerializeField] private float deathAnimationDuration = 2f;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float deathSoundVolume = 1f;
    #endregion

    #region Private Fields
    protected enum State { Idle, Chase, Attack, Death, Returning }
    protected enum DeathTriggerType { Trigger, Bool }
    
    protected State currentState = State.Idle;
    protected float attackTimer = 0f;
    private bool isSubscribed = false;
    
    private Vector3 spawnPosition;
    private bool hasSpawnPosition = false;
    private bool isPlayerDead = false;
    private bool hasSubscribedToPlayerDeath = false;
    private bool isDying = false;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        SetUP();
    }

    private void Update()
    {
        if (player == null)
        {
            HandleNoPlayer();
            return;
        }
        
        if (isDying) return;
        
        CheckPlayerStatus();
        attackTimer -= Time.deltaTime;
        
        if (isPlayerDead)
        {
            HandlePlayerDeath();
            return;
        }
        
        float distanceToPlayer = GetDistanPlayer();
        UpdateState(distanceToPlayer);
    }
    #endregion

    #region Initialization
    public override void SetUP()
    {
        base.SetUP();
        TrySubscribeToDeathEvent();
        SaveSpawnPosition();
        SubscribeToPlayerDeath();
    }
    
    private void SaveSpawnPosition()
    {
        spawnPosition = transform.position;
        hasSpawnPosition = true;
    }
    
    public void SetSpawnPosition(Vector3 position)
    {
        spawnPosition = position;
        hasSpawnPosition = true;
    }

    private void TrySubscribeToDeathEvent()
    {
        if (isSubscribed) return;

        if (QuestManager.Instance != null)
        {
            OnDestory += OnEnemyDeath;
            isSubscribed = true;
        }
        else
        {
            Invoke(nameof(TrySubscribeToDeathEvent), 0.5f);
        }
    }
    
    private void SubscribeToPlayerDeath()
    {
        if (hasSubscribedToPlayerDeath || player == null) return;
        
        Character playerCharacter = player.GetComponent<Character>();
        if (playerCharacter != null)
        {
            playerCharacter.OnDestory += OnPlayerDied;
            hasSubscribedToPlayerDeath = true;
        }
    }
    
    private void OnPlayerDied(Idestoryable destroyed)
    {
        isPlayerDead = true;
    }
    
    private void CheckPlayerStatus()
    {
        if (player == null) return;
        
        Character playerCharacter = player.GetComponent<Character>();
        if (playerCharacter != null)
        {
            if (playerCharacter.health <= 0)
            {
                isPlayerDead = true;
            }
            else
            {
                isPlayerDead = false;
            }
        }
    }
    
    private void HandlePlayerDeath()
    {
        currentState = State.Idle;
        animator.SetBool("Attack", false);
        animator.SetFloat("Speed", 0);
        StopMovement();
    }
    #endregion
    
    #region TakeDamage Override
    public override void TakeDamage(int amount)
    {
        if (isDying) return;
        base.TakeDamage(amount);
    }
    #endregion

    #region Death Handling
    private void OnEnemyDeath(Idestoryable destroyed)
    {
        if (!isDying)
        {
            destroyOnDeath = false;
            StartCoroutine(DeathSequence());
        }
    }
    
    private IEnumerator DeathSequence()
    {
        isDying = true;
        currentState = State.Death;
        
        Debug.Log($"[Enemy] {Name} starting death sequence...");
        
        StopMovement();
        animator.SetBool("Attack", false);
        animator.SetFloat("Speed", 0);
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"[Enemy] {Name} collider disabled");
        }
        
        // ✅ เล่น Death Animation
        float animationLength = 0f;
        if (useDeathAnimation && animator != null)
        {
            // Reset Animator ก่อน
            animator.ResetTrigger(deathAnimationName);
            
            // ตรวจสอบว่าใช้ Bool หรือ Trigger
            if (triggerType == DeathTriggerType.Bool)
            {
                animator.SetBool(deathAnimationName, true);
                Debug.Log($"[Enemy] {Name} setting death BOOL: {deathAnimationName} = true");
            }
            else
            {
                animator.SetTrigger(deathAnimationName);
                Debug.Log($"[Enemy] {Name} triggering death TRIGGER: {deathAnimationName}");
            }
            
            // รอให้ Animator อัพเดต State
            yield return new WaitForEndOfFrame();
            yield return null;
            
            // ตรวจสอบว่า Animation เล่นหรือยัง
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[Enemy] {Name} current animator state: {stateInfo.shortNameHash} (looking for Death)");
            
            // หาความยาวของ Animation
            animationLength = GetAnimationLength(deathAnimationName);
            if (animationLength <= 0)
            {
                animationLength = deathAnimationDuration;
                Debug.LogWarning($"[Enemy] {Name} using default animation duration: {animationLength}s");
            }
            else
            {
                Debug.Log($"[Enemy] {Name} animation length: {animationLength}s");
            }
        }
        
        // เล่นเสียง
        PlayDeathSound();
        
        // อัพเดท Quest
        UpdateQuest();
        
        // ✅ ปิด Rigidbody หลังเล่น Animation (ไม่ให้ตกหล่น)
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // รอให้ Animation เล่นจบ
        if (animationLength > 0)
        {
            Debug.Log($"[Enemy] {Name} waiting for animation to complete ({animationLength}s)...");
            yield return new WaitForSeconds(animationLength);
        }
        
        // ทำลาย GameObject
        Debug.Log($"[Enemy] {Name} destroying now!");
        Destroy(gameObject);
    }
    
    private float GetAnimationLength(string animationName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) 
            return 0f;
        
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == animationName)
            {
                Debug.Log($"[Enemy] Found animation '{animationName}' length: {clip.length}s");
                return clip.length;
            }
        }
        
        Debug.LogWarning($"[Enemy] Animation '{animationName}' not found! Available clips:");
        foreach (AnimationClip clip in clips)
        {
            Debug.Log($"  - {clip.name}");
        }
        
        return 0f;
    }

    private void PlayDeathSound()
    {
        if (deathSound != null)
        {
            GameObject soundObject = new GameObject($"{Name}_DeathSound");
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();
            
            audioSource.clip = deathSound;
            audioSource.volume = deathSoundVolume;
            audioSource.spatialBlend = 0f;
            audioSource.Play();
            
            Destroy(soundObject, deathSound.length);
        }
    }

    private void UpdateQuest()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.UpdateQuestProgress("quest_kill_enemies", Name, 1);
        }
    }
    #endregion

    #region State Machine
    private void HandleNoPlayer()
    {
        currentState = State.Idle;
        animator.SetBool("Attack", false);
        animator.SetFloat("Speed", 0);
    }

    private void UpdateState(float distanceToPlayer)
    {
        if (useLeash && hasSpawnPosition)
        {
            float distanceFromSpawn = Vector3.Distance(transform.position, spawnPosition);
            
            if (distanceFromSpawn > leashDistance && currentState != State.Returning)
            {
                currentState = State.Returning;
            }
        }
        
        switch (currentState)
        {
            case State.Idle:
                HandleIdleState(distanceToPlayer);
                break;
            case State.Chase:
                HandleChaseState(distanceToPlayer);
                break;
            case State.Attack:
                HandleAttackState(distanceToPlayer);
                break;
            case State.Returning:
                HandleReturningState();
                break;
        }
    }

    private void HandleIdleState(float distance)
    {
        animator.SetFloat("Speed", 0);
        
        if (useLeash && hasSpawnPosition)
        {
            float distanceFromSpawn = Vector3.Distance(transform.position, spawnPosition);
            
            if (distanceFromSpawn > returnThreshold)
            {
                currentState = State.Returning;
                return;
            }
        }

        if (distance <= chaseRange && !isPlayerDead)
        {
            currentState = State.Chase;
        }
    }

    private void HandleChaseState(float distance)
    {
        Vector3 directionToPlayer = GetDirectionToPlayer();
        Turn(directionToPlayer);

        if (distance <= attackRange)
        {
            TransitionToAttack();
        }
        else if (distance > chaseRange)
        {
            currentState = State.Idle;
        }
        else
        {
            Move(directionToPlayer);
        }
    }

    private void HandleAttackState(float distance)
    {
        Vector3 directionToPlayer = GetDirectionToPlayer();
        Turn(directionToPlayer);

        if (distance > attackRange + 0.5f)
        {
            TransitionToChase();
        }
        else
        {
            if (!isPlayerDead)
            {
                Attack(player);
            }
            else
            {
                TransitionToIdle();
            }
        }
    }
    
    private void HandleReturningState()
    {
        if (!hasSpawnPosition) 
        {
            currentState = State.Idle;
            return;
        }
        
        float distanceFromSpawn = Vector3.Distance(transform.position, spawnPosition);
        
        if (distanceFromSpawn < 1f)
        {
            ReachedSpawnPoint();
            return;
        }
        
        Vector3 directionToSpawn = (spawnPosition - transform.position).normalized;
        Turn(directionToSpawn);
        ReturnToSpawn(directionToSpawn);

    }
    
    private void ReachedSpawnPoint()
    {
        StopMovement();
        
        if (healOnReturn && health < maxHealth)
        {
            health = maxHealth;
        }
        
        currentState = State.Idle;
    }

    private void TransitionToAttack()
    {
        currentState = State.Attack;
        animator.SetFloat("Speed", 0);
    }

    private void TransitionToChase()
    {
        currentState = State.Chase;
        animator.SetBool("Attack", false);
    }
    
    private void TransitionToIdle()
    {
        currentState = State.Idle;
        animator.SetBool("Attack", false);
        animator.SetFloat("Speed", 0);
    }
    #endregion

    #region Movement & Combat
    private Vector3 GetDirectionToPlayer()
    {
        return (player.transform.position - transform.position).normalized;
    }

    protected override void Turn(Vector3 direction)
    {
        if (direction == Vector3.zero) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
    }
    
    private void ReturnToSpawn(Vector3 direction)
    {
        float returnMovementSpeed = movementSpeed * returnSpeed;
        rb.linearVelocity = new Vector3(direction.x * returnMovementSpeed, rb.linearVelocity.y, direction.z * returnMovementSpeed);
        animator.SetFloat("Speed", rb.linearVelocity.magnitude);
    }

    protected virtual void Attack(Player _player)
    {
        StopMovement();

        if (CanAttack())
        {
            ExecuteAttack(_player);
        }
    }

    private void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            rb.angularVelocity = Vector3.zero;
        }
    }

    private bool CanAttack()
    {
        return attackTimer <= 0;
    }

    private void ExecuteAttack(Player _player)
    {
        if (_player == null || isPlayerDead) return;
        
        _player.TakeDamage(Damage);
        animator.SetBool("Attack", true);
        attackTimer = TimeToAttack;
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        DrawAttackRange();
        DrawChaseRange();
        DrawLeashRange();
    }

    private void DrawAttackRange()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void DrawChaseRange()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
    
    private void DrawLeashRange()
    {
        if (!useLeash) return;
        
        Vector3 center = hasSpawnPosition ? spawnPosition : transform.position;
        
        Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(center, leashDistance);
        
        Gizmos.color = new Color(1f, 0f, 1f, 0.1f);
        Gizmos.DrawSphere(center, leashDistance);
        
        if (hasSpawnPosition && Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(center, transform.position);
            
            float currentDistance = Vector3.Distance(transform.position, center);
            
            if (currentDistance > leashDistance * 0.8f)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = Color.green;
            }
            
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(center, 0.3f);
    }
    #endregion
    
    #region Cleanup
    private void OnPlayerRespawned()
    {
        isPlayerDead = false;
        currentState = State.Idle;
    }
    
    private void OnDestroy()
    {
        if (hasSubscribedToPlayerDeath && player != null)
        {
            Character playerCharacter = player.GetComponent<Character>();
            if (playerCharacter != null)
            {
                playerCharacter.OnDestory -= OnPlayerDied;
            }
        }
    }
    #endregion
}