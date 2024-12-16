using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager instance;

    public int coinCount = 0;
    public GameObject coinPrefab;
    public Transform player; // The plane
    public WeatherManager weatherManager;

    private float spawnDistance = 50f; // Distance ahead to spawn coins

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        // Start spawning coins dynamically
        InvokeRepeating("SpawnCoin", 1f, 2f);
        
    }

    public void CollectCoin()
    {
        coinCount++;
        Debug.Log("Coins Collected: " + coinCount);

        if (coinCount % 5 == 0) // Change weather every 5 coins
        {
            weatherManager.ChangeWeather();
        }
    }

    void SpawnCoin()
    {
        // Randomize position
        Vector3 spawnPosition = player.position + player.forward * spawnDistance;
        spawnPosition.x += Random.Range(-10f, 10f); // Randomize left/right
        spawnPosition.y += Random.Range(-2f, 5f);   // Randomize height

        Instantiate(coinPrefab, spawnPosition, Quaternion.identity);
    }
}
