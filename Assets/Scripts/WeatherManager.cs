using UnityEngine;
using UnityEngine.Rendering;

public class WeatherManager : MonoBehaviour
{
    public ParticleSystem rainEffect;
    public ParticleSystem snowEffect;
    public ParticleSystem stormEffect;

    // Skybox materials
    public Material daySkybox;
    public Material eveningSkybox;
    public Material nightSkybox;

    // Timer pour la météo
    private int currentWeatherIndex = 0;
    private float weatherTimer = 0f;

    // Timer pour changer la Skybox
    private float skyboxTimer = 0f;
    private float skyboxChangeInterval = 5f; // Changement toutes les 5 secondes

    private int currentSkyboxIndex = 0; // 0 = jour, 1 = soir, 2 = nuit

    private void Start()
    {
        // Désactiver tous les effets au début
        DisableAllEffects();

        // Définir la Skybox initiale
        RenderSettings.skybox = daySkybox;
        DynamicGI.UpdateEnvironment(); // Mettre à jour l'éclairage global
    }

    private void Update()
    {
        // Gestion du changement de météo toutes les 5 secondes
        weatherTimer += Time.deltaTime;
        if (weatherTimer >= 5f)
        {
            ChangeWeather();
            weatherTimer = 0f;
        }

        // Gestion du changement de Skybox toutes les 5 secondes
        skyboxTimer += Time.deltaTime;
        if (skyboxTimer >= skyboxChangeInterval)
        {
            ChangeSkybox();
            skyboxTimer = 0f;
        }
    }

    public void ChangeWeather()
    {
        currentWeatherIndex = (currentWeatherIndex + 1) % 4; // 4 états : clair, pluie, neige, tempête
        DisableAllEffects();

        switch (currentWeatherIndex)
        {
            case 1:
                rainEffect.Play();
                Debug.Log("Weather: Rain");
                break;
            case 2:
                snowEffect.Play();
                Debug.Log("Weather: Snow");
                break;
            case 3:
                stormEffect.Play();
                Debug.Log("Weather: Storm");
                break;
            default:
                Debug.Log("Weather: Clear");
                break;
        }
    }

    void ChangeSkybox()
    {
        currentSkyboxIndex = (currentSkyboxIndex + 1) % 3; // 3 états : jour, soirée, nuit

        switch (currentSkyboxIndex)
        {
            case 0:
                RenderSettings.skybox = daySkybox;
                Debug.Log("Skybox: Day");
                break;
            case 1:
                RenderSettings.skybox = eveningSkybox;
                Debug.Log("Skybox: Evening");
                break;
            case 2:
                RenderSettings.skybox = nightSkybox;
                Debug.Log("Skybox: Night");
                break;
        }

        DynamicGI.UpdateEnvironment(); // Mettre à jour l'éclairage pour que le changement soit visible
    }

    void DisableAllEffects()
    {
        rainEffect.Stop();
        snowEffect.Stop();
        stormEffect.Stop();
    }
}

