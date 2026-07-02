using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnSystem : MonoBehaviour
{
    // Variables needed
    public bool isPlayerTurn;
    public int playerTurn = 1;
    public int opponentTurn = 0;
    public TMP_Text turnText;

    public int essenceCurrent => isPlayerTurn ? playerEssenceCurrent : opponentEssenceCurrent;
    public int essenceMax => isPlayerTurn ? playerEssenceMax : opponentEssenceMax;

    [Header("Essence Pools")]
    public int playerEssenceMax = 1;
    public int playerEssenceCurrent = 1;
    public TMP_Text playerEssenceText;
    public int opponentEssenceMax = 1;
    public int opponentEssenceCurrent = 1;
    public TMP_Text opponentEssenceText;


    private void Start()
    {
        isPlayerTurn = true;
        UpdateUIElements();
    }

    private void Update()
    {
        if (turnText != null)
            turnText.text = isPlayerTurn ? "Your Turn" : "Opponent's Turn";
    }

    public void EndPlayerTurn()
    {
        if (!isPlayerTurn) return;

        // Process all effects related with turn on the Player's board
        ProcessEndOfTurnEffects(GameObject.FindWithTag("BattlefieldPlayer"), true);

        // Zephyr's Trigger on player's board
        HandleZephyrSwarm(GameObject.FindWithTag("BattlefieldPlayer"), true);

        // Switch turn from player to opponent 
        isPlayerTurn = false;
        opponentTurn++;
        opponentEssenceMax = Mathf.Min(opponentEssenceMax + 1, 10);
        opponentEssenceCurrent = opponentEssenceMax;

        // Opponent board
        ResetBattlefieldRestrictions(GameObject.FindWithTag("BattlefieldOpponent"));
        FindFirstObjectByType<OpponentDeck>()?.TriggerOpponentDraw(1);
        FindFirstObjectByType<OpponentManager>()?.StartOpponentTurn();

        UpdateUIElements();
    }

    public void EndOpponentTurn()
    {
        if (isPlayerTurn) return;

        // Process all effects related with turn on the opponent's board
        GameObject opponentBattlefield = GameObject.FindWithTag("BattlefieldOpponent");
        ProcessEndOfTurnEffects(opponentBattlefield, false);

        // Zephyr's Trigger on opponent's board
        if (opponentBattlefield != null)
        {
            foreach (Transform child in opponentBattlefield.transform)
            {
                var display = child.GetComponent<CardDisplay>();
                if (display != null && display.displayCard.name.ToLower().Contains("zephyr"))
                {
                    FindFirstObjectByType<OpponentManager>()?.TriggerZephyrEffectForAI();
                }
            }
        }

        // Switch turn from oppoenent to player
        isPlayerTurn = true;
        playerTurn++;
        playerEssenceMax = Mathf.Min(playerEssenceMax + 1, 10);
        playerEssenceCurrent = playerEssenceMax;

        // Reset Player board
        ResetBattlefieldRestrictions(GameObject.FindWithTag("BattlefieldPlayer"));
        //Draw 1 card at the beginning of turn
        FindFirstObjectByType<PlayerDeck>()?.TriggerPlayerDraw(1);
        
        foreach (var power in FindObjectsByType<AvatarPower>(FindObjectsSortMode.None))
            if (power != null) power.hasBeenUsedThisTurn = false;

        UpdateUIElements();
    }

    private void ProcessEndOfTurnEffects(GameObject battlefield, bool isPlayer)
    {
        if (battlefield == null) return;
        var aiManager = FindFirstObjectByType<OpponentManager>();

        foreach (Transform child in battlefield.transform)
        {
            CardDisplay card = child.GetComponent<CardDisplay>();
            if (card == null || card.displayCard == null) continue;

            string name = card.displayCard.name.ToLower();
            string effect = card.displayCard.effect?.ToLower() ?? "";

            //Merlin end of turn trigger
            if (name.Contains("merlin") || effect.Contains("4 damage"))
                card.TriggerMerlinEndTurnEffect(isPlayer);
            //Mutated Hunbter end of turn trigger
            else if (name.Contains("mutated hunter") || effect.Contains("growth"))
                card.TriggerMutatedHunterGrowth();
            //Pan Piper end of turn trigger
            else if (name.Contains("pan piper") || effect.Contains("summons a rat"))
            {
                if (!isPlayer && aiManager != null) aiManager.SpawnTokensForAI(aiManager.ratSO, 1);
                else card.TriggerPanPiperSummon(isPlayer);
            }
        }
    }

    //Zephyr end of turn trigger
    private void HandleZephyrSwarm(GameObject battlefield, bool isPlayer)
    {
        foreach (Transform child in battlefield.transform)
        {
            var display = child.GetComponent<CardDisplay>();
            if (display != null && display.displayCard.name.Contains("Zephyr"))
                TriggerZephyrSkeletonSwarm(battlefield, child.GetComponent<CardInteraction>());
        }
    }
    
    private void ResetBattlefieldRestrictions(GameObject battlefield)
    {
        if (battlefield == null) return;
        foreach (var monster in battlefield.GetComponentsInChildren<CardDisplay>())
            if (monster != null) monster.ResetTurnRestrictions();
    }
    
    public void PerformZephyrSwarm(GameObject targetBattlefield)
    {
        foreach (Transform child in targetBattlefield.transform)
        {
            var display = child.GetComponent<CardDisplay>();
            if (display != null && display.displayCard.name.Contains("Zephyr"))
            {
                TriggerZephyrSkeletonSwarm(targetBattlefield, child.GetComponent<CardInteraction>());
                break; // Only trigger once per turn
            }
        }
    }

    //Zephyr summon trigger
    private void TriggerZephyrSkeletonSwarm(GameObject battlefield, CardInteraction creator)
    {
        ScriptableObject skeletonSO = null;
        if (CardDatabase.cardList != null)
        {
            foreach (var card in CardDatabase.cardList)
            {
                if (card != null && card.name.Equals("Skeleton", System.StringComparison.OrdinalIgnoreCase))
                {
                    skeletonSO = card;
                    break;
                }
            }
        }

        if (skeletonSO == null) return;

        while (battlefield.transform.childCount < 7)
        {
            GameObject tokenCard = Instantiate(creator.cardPrefab, battlefield.transform);
            tokenCard.name = "Skeleton_Token";

            CanvasGroup cg = tokenCard.GetComponent<CanvasGroup>();
            if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; }

            Attack oldAttackComp = tokenCard.GetComponent<Attack>();
            if (oldAttackComp != null) Destroy(oldAttackComp);

            CardDisplay tokenDisplay = tokenCard.GetComponent<CardDisplay>();
            if (tokenDisplay != null)
            {
                tokenDisplay.displayCard = (Card)skeletonSO;
                tokenDisplay.currentAttack = tokenDisplay.displayCard.attack;
                tokenDisplay.currentVigor = tokenDisplay.displayCard.vigor;
                tokenDisplay.cardCover = false; 
                tokenDisplay.RefreshCardUI();
            }

            CardInteraction tokenInteraction = tokenCard.GetComponent<CardInteraction>();
            if (tokenInteraction == null) tokenInteraction = tokenCard.AddComponent<CardInteraction>();
            if (tokenInteraction != null)
            {
                tokenInteraction.linePrefab = creator.linePrefab;
                tokenInteraction.cardPrefab = creator.cardPrefab;
                tokenInteraction.Skeleton = creator.Skeleton;
                tokenInteraction.ForceInit();
                tokenInteraction.parentToReturnTo = battlefield.transform;
                tokenInteraction.hasAttackedThisTurn = false;
            }

            RectTransform tokenRect = tokenCard.GetComponent<RectTransform>();
            if (tokenRect != null)
            {
                tokenRect.localScale = Vector3.one;
                tokenRect.localPosition = Vector3.zero;
                tokenRect.anchoredPosition = Vector2.zero;
                tokenRect.localRotation = Quaternion.Euler(0, 0, 0); 
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(battlefield.GetComponent<RectTransform>());
    }

    public void UpdateUIElements()
    {
        if (playerEssenceText) playerEssenceText.text = $"{playerEssenceCurrent}/{playerEssenceMax}";
        if (opponentEssenceText) opponentEssenceText.text = $"{opponentEssenceCurrent}/{opponentEssenceMax}";
    }

    //Essence usage
    public bool SpendEssence(int amount)
    {
        ref int current = ref (isPlayerTurn ? ref playerEssenceCurrent : ref opponentEssenceCurrent);
        if (current >= amount)
        {
            current -= amount;
            UpdateUIElements();
            return true;
        }
        return false;
    }
}