using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Will : MonoBehaviour
{
    //Variables Needed
    [Header("Will Currency Settings")]
    public int currentWill = 0;
    public int maxWill = 7;

    [Header("UI Elements")]
    public Image willBarImage;  
    public TMP_Text willText; 

    void Start()
    {
        UpdateWillVisuals();
    }

    // Gaining Will Points
    public void GainWill(int amount)
    {
        currentWill = Mathf.Clamp(currentWill + amount, 0, maxWill);
        Debug.Log($"{gameObject.name} Will: Gained {amount} Will! Balance: {currentWill}/{maxWill}");
        UpdateWillVisuals();
    }

    // Spending Will Points
    public bool SpendWill(int amount)
    {
        if (currentWill >= amount)
        {
            currentWill -= amount;
            Debug.Log($"{gameObject.name} Will: Spent {amount} Will! Balance: {currentWill}/{maxWill}");
            UpdateWillVisuals();
            return true;
        }
        
        Debug.LogWarning($"{gameObject.name} Will: Failed to spend {amount} Will. Not enough will! Balance: {currentWill}/{maxWill}");
        return false;
    }

    public void UpdateWillVisuals()
    {
        // Keep the text display fields up to date frame-by-frame
        if (willText != null)
        {
            willText.text = $"{currentWill}/{maxWill}";
        }

        if (willBarImage != null)
        {
            willBarImage.fillAmount = (float)currentWill / (float)maxWill;
            willBarImage.SetVerticesDirty();
            willBarImage.SetMaterialDirty();
        }
    }
}