
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public GameObject airplane; 
    public string targetSceneName; 
    public Vector3 targetPosition; 
    public Vector3 targetRotation; 
    private void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.P))
        {
          
            DontDestroyOnLoad(airplane);


            SceneManager.LoadScene(targetSceneName);


            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        if (airplane != null)
        {
            airplane.transform.position = targetPosition;
            airplane.transform.rotation = Quaternion.Euler(targetRotation);


            FixFlightControllerReferences();
        }


        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void FixFlightControllerReferences()
    {

    var flightController = airplane.GetComponentInChildren<MFlight.MouseFlightController>(true);

    if (flightController != null)
    {

        var mainCamera = GameObject.FindGameObjectWithTag("MainCamera")?.transform;

        if (mainCamera != null)
        {
            flightController.GetType().GetField("cam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flightController, mainCamera);

            flightController.GetType().GetField("aircraft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flightController, airplane.transform);

            Debug.Log("MouseFlightController references have been fixed.");
        }
        else
        {
            Debug.LogError("Camera with tag 'MainCamera' not found!");
        }
    }
    else
    {
        Debug.LogError("MouseFlightController not found in Flight or its children!");
    }
}

}
