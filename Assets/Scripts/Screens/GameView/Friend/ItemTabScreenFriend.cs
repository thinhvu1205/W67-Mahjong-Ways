using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
public class ItemTabScreenFriend : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_TxtNameTab;
    [SerializeField] private GameObject m_TabOn;
    [SerializeField] private TextMeshProUGUI m_TxtCountFriend;
    public void SetInfo(string nameTab, bool isChoose, int countFriend)
    {
        m_TxtNameTab.text = nameTab;
        m_TabOn.gameObject.SetActive(isChoose);
        m_TxtCountFriend.text = countFriend.ToString();
    }
}