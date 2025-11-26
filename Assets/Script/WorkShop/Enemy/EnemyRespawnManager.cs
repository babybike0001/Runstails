using System.Collections;
using UnityEngine;

public class EnemyRespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float respawnDelay = 5f;
    [SerializeField] private bool autoRespawn = true;
    
    [Header("Spawn Position")]
    [SerializeField] private Transform spawnPoint;
    
    [Header("Visual Effects (Optional)")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;
    
    [Header("Sound Effects (Optional)")]
    [SerializeField] private AudioClip respawnSound;
    [SerializeField] [Range(0f, 1f)] private float respawnSoundVolume = 1f;
    [SerializeField] private float soundMinDistance = 5f;
    [SerializeField] private float soundMaxDistance = 20f;
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private GameObject currentEnemy;
    private bool isRespawning = false;
    private AudioSource audioSource;
    
    private void Start()
    {
        InitializeRespawnManager();
    }
    
    private void InitializeRespawnManager()
    {
        if (spawnPoint != null)
        {
            originalPosition = spawnPoint.position;
            originalRotation = spawnPoint.rotation;
        }
        else
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
        }
        
        SetupAudioSource();
        FindExistingEnemy();
        
        if (currentEnemy == null && enemyPrefab != null)
        {
            SpawnEnemyImmediate();
        }
    }
    
    private void FindExistingEnemy()
    {
        EnemyMovetoPlayer existingEnemy = GetComponentInChildren<EnemyMovetoPlayer>();
        
        if (existingEnemy != null)
        {
            currentEnemy = existingEnemy.gameObject;
            SubscribeToEnemyDeath(existingEnemy);
        }
    }
    
    private void SetupAudioSource()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = respawnSoundVolume;
        audioSource.minDistance = soundMinDistance;
        audioSource.maxDistance = soundMaxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.dopplerLevel = 0f;
        audioSource.spread = 0f;
    }
    
    private void SubscribeToEnemyDeath(EnemyMovetoPlayer enemy)
    {
        if (enemy != null)
        {
            Character character = enemy.GetComponent<Character>();
            if (character != null)
            {
                character.OnDestory += OnEnemyDeath;
            }
        }
    }
    
    private void OnEnemyDeath(Idestoryable destroyed)
    {
        PlayDeathEffect();
        
        if (autoRespawn && !isRespawning)
        {
            StartCoroutine(RespawnCoroutine());
        }
    }
    
    private IEnumerator RespawnCoroutine()
    {
        isRespawning = true;
        
        yield return new WaitForSeconds(respawnDelay);
        
        SpawnEnemy();
        
        isRespawning = false;
    }
    
    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            return;
        }
        
        PlaySpawnEffect();
        PlayRespawnSound();
        
        currentEnemy = Instantiate(enemyPrefab, originalPosition, originalRotation);
        
        if (spawnPoint != null)
        {
            currentEnemy.transform.SetParent(spawnPoint);
        }
        else
        {
            currentEnemy.transform.SetParent(transform);
        }
        
        EnemyMovetoPlayer newEnemy = currentEnemy.GetComponent<EnemyMovetoPlayer>();
        if (newEnemy != null)
        {
            SubscribeToEnemyDeath(newEnemy);
            newEnemy.SetSpawnPosition(originalPosition);
        }
    }
    
    private void SpawnEnemyImmediate()
    {
        if (enemyPrefab == null) return;
        
        currentEnemy = Instantiate(enemyPrefab, originalPosition, originalRotation);
        
        if (spawnPoint != null)
        {
            currentEnemy.transform.SetParent(spawnPoint);
        }
        else
        {
            currentEnemy.transform.SetParent(transform);
        }
        
        EnemyMovetoPlayer enemy = currentEnemy.GetComponent<EnemyMovetoPlayer>();
        if (enemy != null)
        {
            SubscribeToEnemyDeath(enemy);
            enemy.SetSpawnPosition(originalPosition);
        }
    }
    
    private void PlayDeathEffect()
    {
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, originalPosition, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }
    
    private void PlaySpawnEffect()
    {
        if (spawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(spawnEffectPrefab, originalPosition, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }
    
    private void PlayRespawnSound()
    {
        if (respawnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(respawnSound, respawnSoundVolume);
        }
    }
    
    public void ForceRespawn()
    {
        if (!isRespawning)
        {
            if (currentEnemy != null)
            {
                Destroy(currentEnemy);
            }
            
            SpawnEnemy();
        }
    }
    
    public void StopAutoRespawn()
    {
        autoRespawn = false;
    }
    
    public void EnableAutoRespawn()
    {
        autoRespawn = true;
    }
    
    public void SetRespawnDelay(float newDelay)
    {
        respawnDelay = Mathf.Max(0f, newDelay);
    }
    
    private void OnDrawGizmos()
    {
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPos, 0.5f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(spawnPos, spawnPos + Vector3.up * 2f);
        
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(spawnPos, soundMinDistance);
        
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(spawnPos, soundMaxDistance);
    }
    
    private void OnDrawGizmosSelected()
    {
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(spawnPos, 1f);
        
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.1f);
        Gizmos.DrawSphere(spawnPos, soundMinDistance);
        
        Gizmos.color = new Color(1f, 0f, 0f, 0.05f);
        Gizmos.DrawSphere(spawnPos, soundMaxDistance);
    }
}