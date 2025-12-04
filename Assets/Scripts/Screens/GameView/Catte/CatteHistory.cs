using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;

public class CatteHistory : MonoBehaviour // Assuming PopupEffect is a MonoBehaviour
{
    [Header("History UI")]
    public ScrollRect scrollViewHistory;
    public GameObject itemHistory;

    void Start()
    {
        // Optional start logic
    }

    public void Init(List<HistoryData> data)
    {
        bool allWinsAreZero = false;
        if (data != null && data.Count > 0)
        {
            allWinsAreZero = data.All(d => d.wins.All(w => w == 0));
        }
        Transform content = scrollViewHistory.content;

        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
        }

        for (int i = 0; i < data.Count; i++)
        {
            GameObject item;
            if (i < content.childCount)
            {
                item = content.GetChild(i).gameObject;
            }
            else
            {
                item = Instantiate(itemHistory, content);
            }

            item.SetActive(true);
            var itemComponent = item.GetComponent<ItemHistoryCatte>();
            if (itemComponent != null)
            {
                itemComponent.SetInfo(data[i], allWinsAreZero && data[i].m > 0 ? true : false);
            }
        }
    }

    public virtual void OnPopOn()
    {
        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }
    public void OnPopOff()
    {
        gameObject.SetActive(false);
    }

}

// Note: You must define class HistoryData and ItemHistoryCatte with method SetInfo(HistoryData info) accordingly.
