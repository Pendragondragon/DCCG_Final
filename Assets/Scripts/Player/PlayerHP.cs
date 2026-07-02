using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHP : MonoBehaviour
{
    public static float maxHP = 30;
    public static float staticHP = 30;
    public float hp;
    public Image Health; 
    public TMP_Text hpText;

    void Start()
    {
        if (staticHP <= 0) 
        {
            staticHP = 30;
        }
        
        hp = staticHP;
        UpdateVisuals();
    }

    void Awake()
    {
        if (staticHP != 30) 
        {
            staticHP = 30;
            hp = 30;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        TakeDamage(damageAmount, true);

        if (hp <= 0)
        {
            FindFirstObjectByType<GameManager>()?.EndGame(false);
        }
    }

    public void TakeDamage(float damageAmount, bool isDirectCombatAttack)
    {
        hp -= damageAmount;
        if (hp < 0) hp = 0;

        staticHP = hp;
        UpdateVisuals();

        Debug.Log($"Player Avatar: Took {damageAmount} damage! Current HP: {hp}");

        if (hp <= 0)
        {
            Debug.Log("Player Defeated! Triggering GameManager...");
            FindFirstObjectByType<GameManager>()?.EndGame(false);
        }

        if (isDirectCombatAttack)
        {
            Will[] allWillPools = FindObjectsByType<Will>(FindObjectsSortMode.None);
            foreach (Will pool in allWillPools)
            {
                if (pool != null && pool.gameObject.name.IndexOf("Opponent", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    pool.GainWill(1);
                    break;
                }
            }
        }
    }

    public void Heal(float healAmount)
    {
        float oldHP = hp;
        hp += healAmount;
        if (hp > maxHP) hp = maxHP;
        staticHP = hp;
        
        string objectPath = gameObject.name;
        Transform currentParent = transform.parent;
        while (currentParent != null)
        {
            objectPath = currentParent.name + "/" + objectPath;
            currentParent = currentParent.parent;
        }

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (Health == null) 
        {
            GameObject healthBar = GameObject.Find("PlayerHealthBar");
            if (healthBar != null) Health = healthBar.GetComponent<Image>();
        }
        
        if (Health != null) 
        {
            float targetFill = (float)hp / (float)maxHP;
            Health.fillAmount = targetFill; 
        }
        
        if (hpText == null)
        {
            hpText = GameObject.Find("PlayerHPText")?.GetComponent<TMP_Text>();
        }
        
        if (hpText != null) 
        {
            hpText.text = hp + "HP";
        }
    }
}