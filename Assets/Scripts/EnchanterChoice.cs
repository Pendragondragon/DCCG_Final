using UnityEngine;
using UnityEngine.SceneManagement;

public class EnchanterChoice : MonoBehaviour
{
    public void LoadEnchanterDeck1Scene()
    {
        SceneManager.LoadScene("EnchanterDeck1"); 
    }

    public void LoadEnchanterDeck2Scene()
    {
        SceneManager.LoadScene("EnchanterDeck2");
    }

    public void LoadMenuFromEnchanterScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
}