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
    [SerializeField] private RectTransform itemLayout;

    public PlayerCharacter localPlayer;
    public static ShopOverlay Singleton { get; private set; }
    private Dictionary<Item, Button> itemShopButtons = new Dictionary<Item, Button>();

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
            var ip = CreateItemPanel(item, ItemGridLayout);
            var but = ip.GetComponentInChildren<Button>();
            itemShopButtons.Add(item, but);

            but.onClick.AddListener(() => {
                SetItemSelected(item);
            });
        }
    }

    void SetItemSelected(Item item) {
        if (item == null || item.GoldValue > localPlayer.currentGold.Value) {
            DescriptionText.text = "hi welcome to the shop";
            BuyButton.gameObject.SetActive(false);
            return;
        }
        if (item.GoldValue > localPlayer.currentGold.Value) return;
        BuyButton.interactable = true;
        BuyButton.GetComponent<Image>().color = Color.white;
        if (localPlayer.items.Contains(item)) {
            BuyButton.interactable = false;
            BuyButton.GetComponent<Image>().color = Color.black;
        }
        BuyButton.onClick.RemoveAllListeners();
        BuyButton.gameObject.SetActive(true);
        BuyButton.onClick.AddListener(() => {
            localPlayer.AddItem(item);
            localPlayer.GetComponent<IHasGold>().AddGoldServerRpc(-item.GoldValue);
            //itemShopButtons[item].interactable = false;
            foreach(var txt in itemShopButtons[item].transform.parent.GetComponentsInChildren<TMP_Text>()) {
                txt.color = Color.gray;
            }
            itemShopButtons[item].transform.parent.GetComponentInChildren<Image>().color = Color.black;
            SetItemSelected(null);
        });

        DescriptionText.text = item.Description;
    }

    public void SetItems(HashSet<Item> items) {
        foreach (Transform tr in itemLayout.transform) {
            GameObject.Destroy(tr.gameObject);
        }

        foreach (Item item in items) {
            var ip = CreateItemPanel(item, itemLayout);
            ip.transform.Find("Value").gameObject.SetActive(false);
        }
    }

    private GameObject CreateItemPanel(Item item, Transform parent) {
        var ip = GameObject.Instantiate(ItemPanel, parent);
        ip.transform.Find("Name").GetComponent<TMP_Text>().text = item.ItemName;
        ip.transform.Find("Value").GetComponent<TMP_Text>().text = item.GoldValue.ToString();
        return ip;
    }

}
