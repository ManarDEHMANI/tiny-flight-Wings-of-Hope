using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    /// <summary>
    /// Loads the next scene in the build index, which should be the game scene.
    /// </summary>
    /// <remarks>
    /// This method is called when the "Play" button is clicked in the main menu.
    /// </remarks>
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    
    /// <summary>
    /// Quits the game application.
    /// </summary>
    /// <remarks>
    /// If running in the Unity Editor, it stops play mode. Otherwise, it quits the application.
    /// </remarks>

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
            Debug.Log("Quit");
        #endif
    }

    /// <summary>
    /// Returns to the main menu.
    /// </summary>
    public void ReturnToMenu()
    {
        SceneManager.LoadScene(0);
    }
}
