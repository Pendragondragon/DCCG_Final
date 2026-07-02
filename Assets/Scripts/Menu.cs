using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void LoadPlayScene()
    {
        SceneManager.LoadScene("FactionChoice"); 
    }

    public void LoadCollectionScene()
    {
        SceneManager.LoadScene("Collection");
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
        // If it's in the editor, stop the play mode
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        // If it's in a built game, close the application
        Application.Quit();
    #endif
        Debug.Log("Quit. Until we meet again, farewell");
    }
}