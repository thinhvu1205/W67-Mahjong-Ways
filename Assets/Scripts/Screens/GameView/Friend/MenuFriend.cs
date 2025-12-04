using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class MenuFriend : BaseView
{
    public override void OnDestroy()
    {
       
        Destroy(gameObject);
    }

    public void OpenListFriendView()
    {
        UIManager.instance.showListFriendView();

    }

}