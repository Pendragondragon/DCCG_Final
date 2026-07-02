using UnityEngine;
using UnityEngine.SceneManagement;

public class MonsterSlayerChoice : MonoBehaviour
{
    public void LoadMonsterSlayerDeck1Scene()
    {
        SceneManager.LoadScene("MonsterSlayerDeck1"); 
    }

    public void LoadMonsterSlayerDeck2Scene()
    {
        SceneManager.LoadScene("MonsterSlayerDeck2");
    }

    public void LoadMenuFromMonsterSlayerScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
}