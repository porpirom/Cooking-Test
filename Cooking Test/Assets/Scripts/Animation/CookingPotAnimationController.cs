using Spine.Unity;
using Spine;
using System.Collections.Generic;
using UnityEngine;

public class CookingPotAnimationController : MonoBehaviour
{
    #region Inspector References
    [SerializeField] private SkeletonGraphic skeletonGraphic;
    #endregion

    #region Private Fields
    private readonly string[] cookingIdleSequence = { "idle-boiled", "idle2" };
    private int cookingIdleIndex = 0;
    private bool isCookingIdlePlaying = false;

    private readonly Dictionary<string, bool> animationLoopMap = new Dictionary<string, bool>()
    {
        {"All Success - Example", false},
        {"All UnSuccess - Example", false},
        {"idle", true},
        {"idle-boiled", true},
        {"idle2", true},
        {"success", false},
        {"success-idle", true},
        {"susccess-close", false},
        {"unsuccess", false},
        {"unsuccess-idle", true},
        {"unsuccess-close", false}
    };
    #endregion

    #region Properties
    public bool SetIsCookingIdlePlaying
    {
        set { isCookingIdlePlaying = value; }
    }
    #endregion

    #region Unity Methods
    private void Start()
    {
        PlayAnimation("idle"); // Play default idle animation
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Starts alternating cooking idle animations.
    /// </summary>
    public void PlayCookingIdle()
    {
        if (isCookingIdlePlaying) return; // Prevent multiple calls
        isCookingIdlePlaying = true;
        PlayNextCookingIdle();
    }

    /// <summary>
    /// Plays an animation by name if it exists in the loop map.
    /// </summary>
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

    /// <summary>
    /// Plays overlay animation on track 1.
    /// </summary>
    public void PlayOverlayAnimation(string animName, bool loop)
    {
        skeletonGraphic.AnimationState.SetAnimation(1, animName, loop);
    }

    /// <summary>
    /// Plays an animation once and then returns to idle.
    /// </summary>
    public void PlayOnceThenIdle(string animName)
    {
        if (!animationLoopMap.ContainsKey(animName))
            return;

        skeletonGraphic.AnimationState.SetAnimation(0, animName, false);
        skeletonGraphic.AnimationState.AddAnimation(0, "idle", true, 0f);
    }

    /// <summary>
    /// Plays the success animation sequence.
    /// </summary>
    public void PlaySuccessSequence()
    {
        skeletonGraphic.AnimationState.SetAnimation(0, "success", false)
            .Complete += entry =>
            {
                skeletonGraphic.AnimationState.SetAnimation(0, "success-idle", true);
            };
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Plays the next idle animation in the sequence for cooking.
    /// </summary>
    private void PlayNextCookingIdle()
    {
        string nextAnim = cookingIdleSequence[cookingIdleIndex];
        cookingIdleIndex = (cookingIdleIndex + 1) % cookingIdleSequence.Length;

        var entry = skeletonGraphic.AnimationState.SetAnimation(0, nextAnim, false);
        entry.Complete += e =>
        {
            if (CookingManagerExistsAndCooking())
                PlayNextCookingIdle();
            else
                isCookingIdlePlaying = false;
        };
    }

    /// <summary>
    /// Checks if a CookingManager exists and cooking is in progress.
    /// </summary>
    private bool CookingManagerExistsAndCooking()
    {
        var manager = FindObjectOfType<CookingManager>();
        return manager != null && manager.IsCooking;
    }
    #endregion
}
