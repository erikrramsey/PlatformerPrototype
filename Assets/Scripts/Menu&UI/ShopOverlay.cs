using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopOverlay : MonoBehaviour {
    [SerializeField] private TMP_Text DescriptionText;
    [SerializeField] private RectTransform ItemGridLayout;

    [SerializeField] private GameObject ItemPanel;
    [SerializeField] private Button BuyButton;

    public PlayerCharacter localPlayer;
    public static ShopOverlay Singleton { get; private set; }

    private Item selectedItem = null;

    void Awake() {
        if (Singleton == null) {
            Singleton = this;
        } else {
            Destroy(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }

    void Start() {
        BuyButton.gameObject.SetActive(false);
        foreach (var item in ItemList.Singleton.Items) {
            var ip = GameObject.Instantiate(ItemPanel, ItemGridLayout);

            var text = ip.GetComponentInChildren<TMP_Text>().text = item.ItemName;
            var but = ip.GetComponentInChildren<Button>();
            but.onClick.AddListener(() => {
                SetItemSelected(item);
            });
        }
    }

    void SetItemSelected(Item item) {
        selectedItem = item;
        BuyButton.onClick.RemoveAllListeners();
        BuyButton.gameObject.SetActive(item != null);
        BuyButton.onClick.AddListener(() => {
            localPlayer.AddItem(item);
            localPlayer.RefreshStatsUIDisplay();
        });

        DescriptionText.text = selectedItem.Description;
    }
}
