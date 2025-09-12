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
    // กำหนด animation names + loop flag
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
        // เล่น animation เริ่มต้น
        PlayAnimation("idle");
    }
    public void PlayCookingIdle()
    {
        if (isCookingIdlePlaying) return; // ป้องกันเรียกซ้ำ
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
            // เช็คก่อนว่ากำลัง cooking อยู่
            if (CookingManagerExistsAndCooking())
                PlayNextCookingIdle();
            else
                isCookingIdlePlaying = false;
        };
    }

    // helper function ตรวจสอบสถานะ cooking
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

    // เล่น overlay animation (track 1)
    public void PlayOverlayAnimation(string animName, bool loop)
    {
        skeletonGraphic.AnimationState.SetAnimation(1, animName, loop);
    }

    // เล่น animation แล้วกลับ idle (optional)
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
                skeletonGraphic.AnimationState.SetAnimation(0, "success-idle", true); // loop idle หลัง success
            };
    }

}
