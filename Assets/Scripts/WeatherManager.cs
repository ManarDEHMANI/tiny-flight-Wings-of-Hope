using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public ParticleSystem rainEffect;
    public ParticleSystem snowEffect;
    public ParticleSystem stormEffect;

    public Light directionalLight; // Lumière principale pour le jour/nuit
    public float dayDuration = 20f; // Durée totale d'une journée (en secondes)

    private int currentWeatherIndex = 0;
    private float weatherTimer = 0f;
    private float lightTimer = 0f;

    private void Start()
    {
        // Désactive tous les effets au début
        DisableAllEffects();
    }

    private void Update()
    {
        // Gère le changement de météo toutes les 5 secondes
        weatherTimer += Time.deltaTime;
        if (weatherTimer >= 5f) // Toutes les 5 secondes
        {
            ChangeWeather();
            weatherTimer = 0f;
        }

        // Gère le cycle de jour/nuit
        UpdateDayNightCycle();
    }

    public void ChangeWeather()
    {
        currentWeatherIndex = (currentWeatherIndex + 1) % 4; // 4 états : rien, pluie, neige, tempête
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

    void UpdateDayNightCycle()
    {
        // Avancement du temps pour le cycle jour/nuit
        lightTimer += Time.deltaTime;

        // Normaliser le temps (0 à 1 pour une journée complète)
        float normalizedTime = (lightTimer % dayDuration) / dayDuration;

        // Calcul de l'angle de la lumière directionnelle (simule la position du soleil)
        float sunAngle = Mathf.Lerp(-90f, 270f, normalizedTime); // -90° à 270° pour un cycle complet
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 0f, 0f);

        // Ajustement de l'intensité et de la couleur selon l'heure de la journée
        if (normalizedTime <= 0.25f || normalizedTime >= 0.75f) // Nuit
        {
            directionalLight.intensity = Mathf.Lerp(0.2f, 0.5f, Mathf.PingPong(normalizedTime * 4, 1));
            directionalLight.color = Color.Lerp(Color.blue * 0.5f, Color.black, 0.5f);
        }
        else // Jour
        {
            directionalLight.intensity = Mathf.Lerp(0.5f, 1.2f, Mathf.PingPong(normalizedTime * 4, 1));
            directionalLight.color = Color.Lerp(Color.yellow, Color.white, 0.5f);
        }
    }

    void DisableAllEffects()
    {
        rainEffect.Stop();
        snowEffect.Stop();
        stormEffect.Stop();
    }
}
