using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CoinManager : MonoBehaviour
{
    public static CoinManager instance;
    public GameObject coinPrefab;
    public Transform player; // The plane
    private float spawnDistance = 800f; // Distance ahead to spawn coins

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        Debug.Log("Coins Started");
        // Start spawning coins dynamically
        InvokeRepeating("SpawnCoin", 1f, 2f);
        
    }


    void SpawnCoin()
    {  
      
        // Randomize position
        Vector3 spawnPosition = player.position + player.forward * spawnDistance;
        spawnPosition.x += Random.Range(-2f, 2f); // Randomize left/right
        spawnPosition.y = 250+Random.Range(-40f,0f);   // Randomize height

        Instantiate(coinPrefab, spawnPosition, Quaternion.identity);
    }
}
