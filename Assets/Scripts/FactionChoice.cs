using UnityEngine;
using UnityEngine.SceneManagement;

public class FactionChoice : MonoBehaviour
{
    public void LoadEnchanterScene()
    {
        SceneManager.LoadScene("Enchanter"); 
    }

    public void LoadMonsterSlayerScene()
    {
        SceneManager.LoadScene("MonsterSlayer");
    }

    public void LoadMenuScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
}