using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CoinManager : MonoBehaviour
{
    public static CoinManager instance;

    public int coinCount = 0;
    public GameObject coinPrefab;
    public Transform player; // The plane
    [Header("Game State Management")]
    [Tooltip("Text displaying the coins collected")] public TextMeshProUGUI coinText;

    private float spawnDistance = 800f; // Distance ahead to spawn coins

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        Debug.Log("Coins Started");
        // Start spawning coins dynamically
        InvokeRepeating("SpawnCoin", 1f, 0.8f);
        
    }

    public void CollectCoin()
    {
        coinCount++;
        Debug.Log("Coins Collected: " + coinCount);
        coinText.text = $"Kits: {coinCount}";
    }

    void SpawnCoin()
    {
        // Randomize position
        Vector3 spawnPosition = player.position + player.forward * spawnDistance;
        spawnPosition.x += Random.Range(-5f, 1f); // Randomize left/right
        spawnPosition.y = 250+Random.Range(-40f,0f);   // Randomize height

        Instantiate(coinPrefab, spawnPosition, Quaternion.identity);
    }
}
