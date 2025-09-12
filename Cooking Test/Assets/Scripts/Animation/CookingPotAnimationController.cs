using Spine.Unity;
using Spine;
using System.Collections.Generic;
using UnityEngine;

public class CookingPotAnimationController : MonoBehaviour
{
    public SkeletonGraphic skeletonGraphic;
    private string[] cookingIdleSequence = { "idle-boiled", "idle2" };
    private int cookingIdleIndex = 0;
    private bool isCookingIdlePlaying = false;
    public bool SetIsCookingIdlePlaying
    {
        set { isCookingIdlePlaying = value; }
    }
    // ��˹� animation names + loop flag
    private Dictionary<string, bool> animationLoopMap = new Dictionary<string, bool>()
    {
        {"All Success - Example", false},
        {"All UnSuccess - Example", false},
        {"idle", true},
        {"idle-boiled", true},
        {"idle2", true},
        {"success", false},
        {"success-idle", true},
        {"susccess-close",false },
        {"unsuccess", false},
        {"unsuccess-idle", true},
        {"unsuccess-close",false }
    };

    void Start()
    {
        // ��� animation �������
        PlayAnimation("idle");
    }
    public void PlayCookingIdle()
    {
        if (isCookingIdlePlaying) return; // ��ͧ�ѹ���¡���
        isCookingIdlePlaying = true;
        PlayNextCookingIdle();
    }

    private void PlayNextCookingIdle()
    {
        string nextAnim = cookingIdleSequence[cookingIdleIndex];
        cookingIdleIndex = (cookingIdleIndex + 1) % cookingIdleSequence.Length;

        var entry = skeletonGraphic.AnimationState.SetAnimation(0, nextAnim, false);
        entry.Complete += e =>
        {
            // �礡�͹��ҡ��ѧ cooking ����
            if (CookingManagerExistsAndCooking())
                PlayNextCookingIdle();
            else
                isCookingIdlePlaying = false;
        };
    }

    // helper function ��Ǩ�ͺʶҹ� cooking
    private bool CookingManagerExistsAndCooking()
    {
        var manager = FindObjectOfType<CookingManager>();
        return manager != null && manager.isCooking && !manager.isPaused;
    }

    public void PlayAnimation(string animName)
    {
        if (!animationLoopMap.ContainsKey(animName))
        {
            Debug.LogWarning("Animation name not found: " + animName);
            return;
        }

        bool loop = animationLoopMap[animName];
        skeletonGraphic.AnimationState.SetAnimation(0, animName, loop);
    }

    // ��� overlay animation (track 1)
    public void PlayOverlayAnimation(string animName, bool loop)
    {
        skeletonGraphic.AnimationState.SetAnimation(1, animName, loop);
    }

    // ��� animation ���ǡ�Ѻ idle (optional)
    public void PlayOnceThenIdle(string animName)
    {
        if (!animationLoopMap.ContainsKey(animName))
            return;

        skeletonGraphic.AnimationState.SetAnimation(0, animName, false);
        skeletonGraphic.AnimationState.AddAnimation(0, "Idle", true, 0f);
    }
    public void PlaySuccessSequence()
    {
        skeletonGraphic.AnimationState.SetAnimation(0, "success", false)
            .Complete += entry =>
            {
                skeletonGraphic.AnimationState.SetAnimation(0, "success-idle", true); // loop idle ��ѧ success
            };
    }

}
