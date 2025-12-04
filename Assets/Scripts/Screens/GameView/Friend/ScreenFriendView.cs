using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

public class ScreenFriendView : MonoBehaviour
{
    public static ScreenFriendView instance;
    [SerializeField] private GameObject m_ItemTabScreenFrien;
    [SerializeField] private Transform m_ParentListTab;
    private JObject FriendData = new JObject();
    private JArray ListTabFriend = new JArray();
    private JArray ListFriend = new JArray();
    private JArray ListInvited = new JArray();
    private JArray ListRequest = new JArray();
    public void Awake()
    {
        instance = this;
        SocketSend.getListFriend();
        ListTabFriend = new JArray
{
    new JObject
    {
        ["name"] = "Friend",
        ["quantity"] = 5
    },
    new JObject
    {
        ["name"] = "Close Friend",
        ["quantity"] = 10
    },
     new JObject
    {
        ["name"] = "Best Friend",
        ["quantity"] = 10
    },
     new JObject
    {
        ["name"] = "Soulmate",
        ["quantity"] = 10
    },
     new JObject
    {
        ["name"] = "Request",
        ["quantity"] = 10
    },
     new JObject
    {
        ["name"] = "Invitation",
        ["quantity"] = 10
    },

};

    }
    void Start()
    {
        ReloadListTabFriend();
    }
    void ReloadListTabFriend()
    {
        foreach (Transform child in m_ParentListTab)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < ListTabFriend.Count; i++)
        {
            var itemTab = Instantiate(m_ItemTabScreenFrien, m_ParentListTab);
            itemTab.transform.localScale = Vector3.one;
            itemTab.GetComponent<ItemTabScreenFriend>().SetInfo(ListTabFriend[i]["name"].ToString(), i == 0, (int)ListTabFriend[i]["quantity"]);
        }
    }
    public void reloadListFriend()
    {
        FriendData = Globals.COMMON_DATA.JsonDataFriend;
        ListFriend = (JArray)FriendData["listFriend"];
        ListInvited = (JArray)FriendData["listInvite"];
        ListRequest = (JArray)FriendData["listRequest"];
    }
    public void OnDestroy()
    {
        Destroy(gameObject);
    }
}
