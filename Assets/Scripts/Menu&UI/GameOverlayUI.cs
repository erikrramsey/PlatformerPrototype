using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverlayUI : MonoBehaviour {
    [SerializeField] private Image healthImage;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] public float maxHealth;

    public void setMaxHealth(float max, float current) {
        maxHealth = max;
        healthImage.fillAmount = current / maxHealth;
        healthText.text = current.ToString();
    }

    public void setHealth(float current) {
        healthImage.fillAmount = current / maxHealth;
        healthText.text = current.ToString();
    }
}
