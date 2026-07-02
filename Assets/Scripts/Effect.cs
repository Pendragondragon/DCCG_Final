using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect : MonoBehaviour
{
    public static Effect Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ResolveMonsterCombat(CardDisplay attacker, CardDisplay defender, bool isPlayerAttacking)
    {
        if (attacker == null || defender == null) return;

        // Base damage values from current attack stats
        int damageToDefender = attacker.currentAttack;
        int damageToAttacker = defender.currentAttack;

        // Track lethal keyword on attacker or blocker
        bool attackerIsLethal = HasEffect(attacker, "Lethal");
        bool defenderIsLethal = HasEffect(defender, "Lethal");

        //Calculate damage for vampirism keyword
        int actualDamageDealtToDefender = Mathf.Max(0, Mathf.Min(damageToDefender, defender.currentVigor));
        int actualDamageDealtToAttacker = Mathf.Max(0, Mathf.Min(damageToAttacker, attacker.currentVigor));

        // Attacker Has Vampirism
        if (attacker.displayCard != null && HasEffect(attacker, "Vampirism") && actualDamageDealtToDefender > 0)
        {
            if (isPlayerAttacking)
                // Heals player
                HealPlayerFace(actualDamageDealtToDefender);
            else
                // Heals opponent
                HealOpponentFace(actualDamageDealtToDefender);
        }

        // Defender Has Vampirism (still drains beccause it deals damage to attacking creature)
        if (defender.displayCard != null && HasEffect(defender, "Vampirism") && actualDamageDealtToAttacker > 0)
        {
            if (isPlayerAttacking)
                HealOpponentFace(actualDamageDealtToAttacker);
            else
                HealPlayerFace(actualDamageDealtToAttacker);
        }

        // If the attacker has Lethal and has more than 0 attack, it deals instantly lethal damage
        if (attackerIsLethal && damageToDefender > 0)
        {
            damageToDefender = defender.currentVigor;
            Debug.Log($"Lethal: {attacker.displayCard.name} fatal attack applied to {defender.displayCard.name}.");
        }

        // If the attacked creature has lethal as its keyword and has more than 0 attack, destroys opponents creature
        if (defenderIsLethal && damageToAttacker > 0)
        {
            damageToAttacker = attacker.currentVigor;
            Debug.Log($"Lethal: {defender.displayCard.name} fatal retaliation applied to {attacker.displayCard.name}.");
        }

        // Apply final calculated damage amounts to both monsters
        defender.TakeDamage(damageToDefender);
        attacker.TakeDamage(damageToAttacker);

        // If a lethal strike connected but armor/shields somehow left them with 1 HP, force drop them to 0
        if (attackerIsLethal && damageToDefender > 0 && defender.currentVigor > 0)
        {
            defender.currentVigor = 0;
            defender.RefreshCardUI(); 
        }
        if (defenderIsLethal && damageToAttacker > 0 && attacker.currentVigor > 0)
        {
            attacker.currentVigor = 0;
            attacker.RefreshCardUI();
        }

        // Will calculations based on destroying creature
        Will[] allWillPools = FindObjectsByType<Will>(FindObjectsSortMode.None);

        if (isPlayerAttacking)
        {
            // It's the Player's turn! If the enemy defender died, give the player 1 Will point.
            if (defender.currentVigor <= 0)
            {
                Debug.Log($"Reward: Player destroyed opponent's creature {defender.gameObject.name}! You receice 1 Will point.");
                foreach (Will pool in allWillPools)
                {
                    if (pool != null && pool.gameObject.name.IndexOf("Opponent", System.StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        pool.GainWill(1);
                        break;
                    }
                }
            }
        }
        else
        {
            // It's the Opponent's turn! If your player card died from the attack, give the opponent 1 Will point.
            if (attacker.currentVigor <= 0)
            {
                Debug.Log($"Reward: Opponent destroyed your creature {attacker.gameObject.name}! opponent receives 1 Will point.");
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
    }

    private bool HasEffect(CardDisplay card, string effectName)
    {
        if (card.displayCard == null || string.IsNullOrEmpty(card.displayCard.effect)) return false;
        
        // Checks if the effect string contains the keyword anywhere (handles multi-effect cards)
        return card.displayCard.effect.IndexOf(effectName, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void HealPlayerFace(int amount)
    {
        PlayerHP player = FindFirstObjectByType<PlayerHP>();
        if (player != null)
        {
            Debug.Log($"Vampirism Found PlayerHP script component. HP before heal execution: {player.hp} / {PlayerHP.maxHP}. Attacker dealt {amount} damage.");
            player.Heal(amount);
            Debug.Log($"Vampirism: Heal method finished. Player HP is now: {player.hp} (Static tracker synced to: {PlayerHP.staticHP})");
        }
        else
        {
            Debug.LogError("Vampirism Error: Effect script failed to find the 'PlayerHP' script component in your scene hierarchy!");
        }
    }

    private void HealOpponentFace(int amount)
    {
        OpponentHP opponent = FindFirstObjectByType<OpponentHP>();
        if (opponent != null)
        {
            Debug.Log($"Vampirism: Found OpponentHP script component. HP before heal execution: {opponent.hp}. Enemy dealt {amount} damage.");
            opponent.Heal(amount);
            Debug.Log($"Vampirism: Heal method finished. Opponent HP is now: {opponent.hp}");
        }
        else
        {
            Debug.LogError("[Vampirism Error: Effect script failed to find the 'OpponentHP' script component in your scene hierarchy!");
        }
    }
}