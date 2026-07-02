using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class CardInteractionOpponent : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Variables Needed
    public enum InteractionMode { None, HandDrag, BattlefieldAttack, ETBTargeting }
    private InteractionMode currentMode = InteractionMode.None;

    [Header("Hand Drag Settings")]
    [HideInInspector] public Transform parentToReturnTo = null;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;

    [Header("Battlefield Attack Settings")]
    public GameObject linePrefab; 
    private GameObject activeLine;
    private RectTransform lineRect;

    [Header("Token Summon Settings")]
    public GameObject cardPrefab;   
    public ScriptableObject Mastiff;

    private TurnSystem turnSystem;
    private CardDisplay cardDisplay;
    private bool isInitialized = false;

    void Start()
    {
        ForceInit();
    }

    public void ForceInit()
    {
        if (isInitialized) return;

        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

        cardDisplay = GetComponent<CardDisplay>();
        FindTurnSystem();

        isInitialized = true;
    }

    private void FindTurnSystem()
    {
        if (turnSystem == null)
        {
            GameObject turnSystemGO = GameObject.Find("TurnSystem");
            if (turnSystemGO != null)
            {
                turnSystem = turnSystemGO.GetComponent<TurnSystem>();
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ForceInit();

        currentMode = InteractionMode.None;
        Transform currentParent = this.transform.parent;
        if (currentParent == null) return;

        FindTurnSystem();
        // Opponent cards can only react if it is the opponent's turn
        if (turnSystem == null || turnSystem.isPlayerTurn)
        {
            eventData.pointerDrag = null;
            return; 
        }

        // Card is in Opponent Hand
        if (currentParent.name == "HandOpponent" || currentParent.CompareTag("HandOpponent"))
        {
            if (cardDisplay != null && cardDisplay.displayCard != null)
            {
                if (turnSystem.essenceCurrent < cardDisplay.displayCard.essence)
                {
                    Debug.LogWarning("Interaction Opponent: Not enough Essence to play this card!");
                    eventData.pointerDrag = null;
                    return;
                }
            }

            currentMode = InteractionMode.HandDrag;
            parentToReturnTo = currentParent;
            this.transform.SetParent(canvas.transform);
            canvasGroup.blocksRaycasts = false; 
        }

        //  Card is on the Opponent's Battlefield
        else if (currentParent.name == "BattlefieldOpponent" || 
                 currentParent.CompareTag("BattlefieldOpponent") || 
                 FindBattlefieldInAncestors(currentParent) != null)
        {
            currentMode = InteractionMode.BattlefieldAttack;

            if (linePrefab != null && canvas != null)
            {
                activeLine = Instantiate(linePrefab, canvas.transform);
                lineRect = activeLine.GetComponent<RectTransform>();
                
                Transform backgroundTransform = canvas.transform.Find("Background");
                if (backgroundTransform != null)
                {
                    activeLine.transform.SetSiblingIndex(backgroundTransform.GetSiblingIndex() + 1);
                }
                else
                {
                    activeLine.transform.SetAsLastSibling();
                }
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null || currentMode == InteractionMode.None) return;

        if (currentMode == InteractionMode.HandDrag)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                rectTransform.position = eventData.position;
            }
            else
            {
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector3 globalMousePos))
                {
                    rectTransform.position = globalMousePos;
                }
            }
        }
        else if (currentMode == InteractionMode.BattlefieldAttack && activeLine != null && lineRect != null)
        {
            Vector2 startPos = transform.position;
            Vector2 endPos = eventData.position;

            lineRect.position = startPos;
            Vector2 direction = endPos - startPos;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            lineRect.rotation = Quaternion.Euler(0, 0, angle - 90);
            
            float distance = Vector2.Distance(startPos, endPos);
            lineRect.sizeDelta = new Vector2(lineRect.sizeDelta.x, distance);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentMode == InteractionMode.BattlefieldAttack)
        {
            currentMode = InteractionMode.None;
            if (activeLine != null) Destroy(activeLine);

            HandleAttackResolution(eventData);
            return;
        }

        if (currentMode == InteractionMode.HandDrag)
        {
            currentMode = InteractionMode.None;
            canvasGroup.blocksRaycasts = true;
            HandleHandDragResolution(eventData);
            return;
        }

        currentMode = InteractionMode.None;
        canvasGroup.blocksRaycasts = true;
    }

    private void HandleHandDragResolution(PointerEventData eventData)
    {
        GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;

        if (hitObject != null)
        {
            Transform targetBattlefield = null;
            // INVERSION: Opponent plays cards to BattlefieldOpponent
            if (hitObject.name == "BattlefieldOpponent" || hitObject.CompareTag("BattlefieldOpponent"))
            {
                targetBattlefield = hitObject.transform;
            }
            else
            {
                targetBattlefield = FindBattlefieldInAncestors(hitObject.transform);
            }

            if (targetBattlefield != null)
            {
                int cost = cardDisplay.displayCard.essence;
                if (!turnSystem.SpendEssence(cost))
                {
                    ReturnToHome();
                    return;
                }

                parentToReturnTo = targetBattlefield;
                SetAtCalculatedIndex(targetBattlefield, eventData.position);

                if (cardDisplay.displayCard != null)
                {
                    string currentEffect = cardDisplay.displayCard.effect;
                    
                    Debug.Log($"Opponent's ETB Trigger Card '{cardDisplay.displayCard.name}' played! Effect field reads: \"{currentEffect}\"");

                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Heal 2"))
                    {
                        OpponentHP opponent = FindFirstObjectByType<OpponentHP>();
                        if (opponent != null) opponent.Heal(2);
                    }

                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Heal 1"))
                    {
                        OpponentHP opponent = FindFirstObjectByType<OpponentHP>();
                        if (opponent != null) opponent.Heal(1);
                    }

                    else if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Deals 2 damage"))
                    {
                        PlayerHP player = FindFirstObjectByType<PlayerHP>();
                        if (player != null) player.TakeDamage(2);
                    }

                    else if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Deals 3 damage"))
                    {
                        PlayerHP player = FindFirstObjectByType<PlayerHP>();
                        if (player != null) player.TakeDamage(3);
                    }

                    // Urgok ETB
                    else if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Thunder"))
                    {
                        TriggerThunderEffect();
                    }

                    // Lord of the Hunt Trigger
                    else if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Summon 2 Mastiffs"))
                    {
                        if (cardPrefab != null && Mastiff != null)
                        {
                            GameObject opponentHand = GameObject.Find("HandOpponent");

                            int masterDatabaseId = -1;
                            if (CardDatabase.cardList != null)
                            {
                                for (int index = 0; index < CardDatabase.cardList.Count; index++)
                                {
                                    if (CardDatabase.cardList[index] != null && 
                                        CardDatabase.cardList[index].name == Mastiff.name)
                                    {
                                        masterDatabaseId = index;
                                        break;
                                    }
                                }
                            }

                            if (masterDatabaseId == -1) masterDatabaseId = 0; 

                            for (int i = 0; i < 2; i++)
                            {
                                GameObject tokenCard = Instantiate(cardPrefab, opponentHand != null ? opponentHand.transform : canvas.transform);
                                tokenCard.name = "CardToHand(Clone)";
                                tokenCard.tag = "Untagged"; 

                                CanvasGroup cg = tokenCard.GetComponent<CanvasGroup>();
                                if (cg != null)
                                {
                                    cg.interactable = true;
                                    cg.blocksRaycasts = true;
                                }

                                Attack attackComponent = tokenCard.GetComponent<Attack>();
                                if (attackComponent == null)
                                {
                                    attackComponent = tokenCard.AddComponent<Attack>();
                                    attackComponent.linePrefab = this.linePrefab; 
                                }

                                CardDisplay tokenDisplay = tokenCard.GetComponent<CardDisplay>();
                                if (tokenDisplay != null)
                                {
                                    tokenDisplay.displayId = masterDatabaseId;
                                    tokenDisplay.displayCard = CardDatabase.cardList[masterDatabaseId];
                                    tokenDisplay.currentAttack = tokenDisplay.displayCard.attack;
                                    tokenDisplay.currentVigor = tokenDisplay.displayCard.vigor;
                                    tokenDisplay.RefreshCardUI();
                                }

                                CardInteractionOpponent tokenInteraction = tokenCard.GetComponent<CardInteractionOpponent>();
                                if (tokenInteraction != null)
                                {
                                    tokenInteraction.ForceInit();
                                    tokenInteraction.parentToReturnTo = targetBattlefield;
                                }

                                tokenCard.transform.SetParent(targetBattlefield);

                                RectTransform tokenRect = tokenCard.GetComponent<RectTransform>();
                                if (tokenRect != null)
                                {
                                    tokenRect.localScale = Vector3.one;
                                    tokenRect.localPosition = Vector3.zero;
                                    tokenRect.anchoredPosition = Vector2.zero;
                                    tokenRect.localRotation = Quaternion.Euler(25, 0, 0);
                                }
                            }
                            
                            LayoutRebuilder.ForceRebuildLayoutImmediate(targetBattlefield.GetComponent<RectTransform>());
                            Debug.Log("Opponent: Mastiffs summoned for the enemy side layout!");
                        }
                    }
                    
                    //Draw 2 cards trigger
                    else if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Draw 2 cards"))
                    {
                        PlayerDeck playerDeckScript = FindFirstObjectByType<PlayerDeck>();
                        if (playerDeckScript != null)
                        {
                            playerDeckScript.DrawSingleCardDirectly();
                            playerDeckScript.DrawSingleCardDirectly();
                        }
                    }

                    //Draws 1 card trigger 
                    else if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Draw a card"))
                    {
                        PlayerDeck playerDeckScript = FindFirstObjectByType<PlayerDeck>();
                        if (playerDeckScript != null)
                        {
                            playerDeckScript.DrawSingleCardDirectly();
                        }
                    }

                    // Sir Lancelot Buff
                    else if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Other monsters you control get +1/+1"))
                    {
                        GameObject opponentBattlefield = GameObject.FindWithTag("BattlefieldOpponent");
                        if (opponentBattlefield == null) opponentBattlefield = GameObject.Find("BattlefieldOpponent");

                        if (opponentBattlefield != null)
                        {
                            int buffedCount = 0;
                            foreach (Transform cardTransform in opponentBattlefield.transform)
                            {
                                if (cardTransform.gameObject == this.gameObject) continue;

                                CardDisplay creatureDisplay = cardTransform.GetComponent<CardDisplay>();
                                if (creatureDisplay != null)
                                {
                                    creatureDisplay.currentAttack += 1;
                                    creatureDisplay.currentVigor += 1;
                                    creatureDisplay.isBuffed = true;
                                    creatureDisplay.RefreshCardUI();
                                    buffedCount++;
                                }
                            }
                            Debug.Log($"Lancelot: Granted +1/+1 to {buffedCount} allied enemy creatures.");
                        }
                    }

                    // tavern Bard Buff
                    else if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Other monsters you control get +0/+1"))
                    {
                        GameObject opponentBattlefield = GameObject.FindWithTag("BattlefieldOpponent");
                        if (opponentBattlefield == null) opponentBattlefield = GameObject.Find("BattlefieldOpponent");

                        if (opponentBattlefield != null)
                        {
                            int buffedCount = 0;
                            foreach (Transform cardTransform in opponentBattlefield.transform)
                            {
                                if (cardTransform.gameObject == this.gameObject) continue;

                                CardDisplay creatureDisplay = cardTransform.GetComponent<CardDisplay>();
                                if (creatureDisplay != null)
                                {
                                    creatureDisplay.currentVigor += 1;
                                    creatureDisplay.isBuffed = true;
                                    creatureDisplay.RefreshCardUI();
                                    buffedCount++;
                                }
                            }
                            Debug.Log($"Tavern Bard: Granted +1 Vigor to {buffedCount} allied enemy creatures.");
                        }
                    }

                    // Shadowveil triiger
                    else if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Shadowveil"))
                    {
                        if (cardDisplay != null)
                        {
                            cardDisplay.hasShadowveil = true;
                            cardDisplay.RefreshCardUI();
                        }
                    }
                }
                return;
            }
        }

        ReturnToHome();
    }

    private void HandleAttackResolution(PointerEventData eventData)
    {
        if (activeLine != null) Destroy(activeLine);

        GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;

        if (hitObject == null)
        {
            var pointerData = new PointerEventData(EventSystem.current) { position = eventData.position };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            foreach (var result in results)
            {
                if (result.gameObject != this.gameObject && result.gameObject.name != "TargetingLine")
                {
                    hitObject = result.gameObject;
                    break;
                }
            }
        }

        if (hitObject == null) return;

        // Urgok attack trigger
        if (cardDisplay != null && cardDisplay.displayCard != null && !string.IsNullOrEmpty(cardDisplay.displayCard.effect))
        {
            if (cardDisplay.displayCard.effect.Contains("Thunder"))
            {
                TriggerThunderEffect();
            }
        }

        // Break shadowveil keyword effect
        if (cardDisplay != null && cardDisplay.hasShadowveil)
        {
            cardDisplay.hasShadowveil = false;
            cardDisplay.RefreshCardUI();
        }

        Transform currentCheck = hitObject.transform;
        bool hitPlayerFace = false;
        while (currentCheck != null)
        {
            if (currentCheck.name == "PlayerHP" || currentCheck.gameObject.name == "PlayerHP" || currentCheck.CompareTag("Player"))
            {
                hitPlayerFace = true;
                break;
            }
            currentCheck = currentCheck.parent;
        }

        if (hitPlayerFace)
        {
            PlayerHP player = FindFirstObjectByType<PlayerHP>();
            if (player != null)
            {
                Debug.Log($"Combat: Enemy attacked player avatar for {cardDisplay.currentAttack} damage!");
                
                if (cardDisplay.displayCard != null && !string.IsNullOrEmpty(cardDisplay.displayCard.effect))
                {
                    if (cardDisplay.displayCard.effect.Trim().Equals("Vampirism", System.StringComparison.OrdinalIgnoreCase))
                    {
                        int faceDamage = (int)cardDisplay.currentAttack;
                        int actualDamage = Mathf.Min(faceDamage, (int)player.hp);
                        
                        if (actualDamage > 0)
                        {
                            OpponentHP opponent = FindFirstObjectByType<OpponentHP>();
                            if (opponent != null) opponent.Heal(actualDamage);
                        }
                    }
                }

                player.TakeDamage(cardDisplay.currentAttack);
            }
            return;
        }

        CardDisplay targetCard = hitObject.GetComponentInParent<CardDisplay>();
        if (targetCard != null && targetCard.transform.parent != null)
        {
            if (targetCard.transform.parent.name == "BattlefieldPlayer" || targetCard.transform.parent.CompareTag("BattlefieldPlayer"))
            {
                if (targetCard.hasShadowveil)
                {
                    Debug.LogWarning($"Opponent Combat Cancelled: Cannot attack {targetCard.displayCard.name} because it has Shadowveil!");
                    return;
                }

                if (Effect.Instance != null)
                {
                    Effect.Instance.ResolveMonsterCombat(cardDisplay, targetCard, false);
                }
                else
                {
                    int enemyAttackAmount = cardDisplay.currentAttack;
                    int playerVigorAmount = targetCard.currentAttack;
                    targetCard.TakeDamage(enemyAttackAmount);
                    cardDisplay.TakeDamage(playerVigorAmount);
                }
            }
        }
    }

    private void TriggerThunderEffect()
    {
        Debug.Log("Thunder Effect: Enemy Urgok channels lightning across your board!");

        GameObject playerBattlefield = GameObject.FindWithTag("BattlefieldPlayer");
        if (playerBattlefield == null) playerBattlefield = GameObject.Find("BattlefieldPlayer");

        if (playerBattlefield != null)
        {
            CardDisplay[] playerMonsters = playerBattlefield.GetComponentsInChildren<CardDisplay>();
            foreach (CardDisplay monster in playerMonsters)
            {
                if (monster != null)
                {
                    monster.TakeDamage(1);
                }
            }
        }

        PlayerHP playerAvatar = FindFirstObjectByType<PlayerHP>();
        if (playerAvatar != null)
        {
            playerAvatar.TakeDamage(1);
        }
    }

    private Transform FindBattlefieldInAncestors(Transform current)
    {
        while (current != null)
        {
            if (current.name == "BattlefieldOpponent" || current.CompareTag("BattlefieldOpponent")) return current;
            current = current.parent;
        }
        return null;
    }

    private void SetAtCalculatedIndex(Transform battlefield, Vector2 mousePosition)
    {
        this.transform.SetParent(battlefield);
        int targetIndex = battlefield.childCount;

        for (int i = 0; i < battlefield.childCount; i++)
        {
            Transform child = battlefield.GetChild(i);
            if (child == this.transform) continue;

            RectTransform childRect = child.GetComponent<RectTransform>();
            if (childRect != null)
            {
                if (mousePosition.x < childRect.position.x)
                {
                    targetIndex = i;
                    break; 
                }
            }
        }

        this.transform.SetSiblingIndex(targetIndex);
        ReturnToHome();
        LayoutRebuilder.ForceRebuildLayoutImmediate(battlefield.GetComponent<RectTransform>());
    }

    private void ReturnToHome()
    {
        if (parentToReturnTo == null) return;
        this.transform.SetParent(parentToReturnTo);
        rectTransform.localPosition = Vector3.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localEulerAngles = new Vector3(25, 0, 0);
    }
}