using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemHistoryCatte : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI lbName;
    public TextMeshProUGUI lbMoney;
    public GameObject[] listCard;

    void Start()
    {
        // Optional: initialization if needed
    }

    public void SetInfo(HistoryData data, bool isWinAll = false)
    {
        lbName.text = data.username;
        lbMoney.text = Globals.Config.FormatNumber(data.m);

        for (int i = 0; i < listCard.Length && i < data.cards.Length; i++)
        {
            Card card = listCard[i].GetComponent<Card>();
            if (card != null)
            {
                card.setTextureWithCode(data.cards[i].I);
                if (data.wins != null && i < data.wins.Length && data.wins[i] == 1)
                {
                    card.animBorderCatte.gameObject.SetActive(true);
                }
                else
                {

                    card.animBorderCatte.gameObject.SetActive(isWinAll);


                }
            }
        }
    }
}
public class HistoryData
{
    public string username;
    public int m;
    public CardData[] cards;
    public int[] wins;
}

public class CardData
{
    public int I;
}

