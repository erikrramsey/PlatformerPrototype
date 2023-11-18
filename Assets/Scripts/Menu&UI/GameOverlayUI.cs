using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverlayUI : MonoBehaviour {
    [SerializeField] private Image healthImage;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] public float maxHealth;
    [SerializeField] private TMP_Text goldText;

    public static GameOverlayUI Singleton { get; private set; }

    void Awake() {
        if (Singleton == null) {
            Singleton = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }

    public void setMaxHealth(float max, float current) {
        maxHealth = max;
        healthImage.fillAmount = current / maxHealth;
        healthText.text = current.ToString();
    }

    public void setHealth(float current) {
        healthImage.fillAmount = current / maxHealth;
        healthText.text = current.ToString();
    }
    
    public void setGold(int current) {
        goldText.text = "Gold: " + current;
    }
}
