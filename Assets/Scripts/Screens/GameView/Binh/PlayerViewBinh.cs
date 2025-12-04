using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerViewBinh : PlayerView
{
    // Start is called before the first frame update
    public override void setEffectWin(string animName = "", bool isLoop = true)
    {
        //animResult.TrimRenderers();
        animResult.gameObject.SetActive(true);
        animResult.skeletonDataAsset = listAnimResult[2];
        animResult.Initialize(true);
        if (animName == "")
        {
            animResult.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            animResult.AnimationState.SetAnimation(0, "wincam", isLoop);

        }
        else
        {
            animResult.AnimationState.SetAnimation(0, animName, isLoop);
        }
        if (isLoop == false)
        {
            animResult.AnimationState.Complete += delegate
            {
                animResult.transform.localScale = new Vector3(1f, 1f, 1f);
                animResult.gameObject.SetActive(false);

            };
        }

    }
    public override void setEffectLose(bool isLoop = true)
    {
        animResult.TrimRenderers();
        animResult.gameObject.SetActive(true);
        animResult.skeletonDataAsset = listAnimResult[0];
        animResult.Initialize(true);
        animResult.AnimationState.SetAnimation(0, "lose_cam", isLoop);

        if (isLoop == false)
        {
            animResult.AnimationState.Complete += delegate
            {
                animResult.gameObject.SetActive(false);
            };
        }
    }

}
