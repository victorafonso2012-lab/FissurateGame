using UnityEngine;
using System;
using System.Collections.Generic;

public class SwordGroundTrail : MonoBehaviour
{
    [Serializable]
    public class AnimationTrailPose
    {
        [Tooltip("Nome do clip/estado de animação. Ex.: SashaRun, RunTop, RunDown.")]
        public string animationName;

        [Tooltip("Posição local que o SwordTrailPoint deve usar enquanto essa animação estiver tocando.")]
        public Vector3 localPosition;
    }

    public PlayerMove playerMove;
    public SwordAttack swordAttack;
    public Rigidbody2D playerRb;
    public TrailRenderer trail;

    [Header("Ativação")]
    public float minMoveSpeed = 0.1f;
    public bool clearTrailWhenStopping = true;

    [Header("Posicionamento por animação")]
    public Vector3 defaultLocalPosition;
    public bool useAnimationBasedPosition = true;
    public List<AnimationTrailPose> animationPoses = new();

    private Animator playerAnimator;
    private string lastAnimationName;
    private bool lastTrailingState;

    void Awake()
    {
        if (playerMove == null)
            playerMove = FindFirstObjectByType<PlayerMove>();

        if (playerMove != null && playerRb == null)
            playerRb = playerMove.GetComponent<Rigidbody2D>();

        if (playerMove != null && playerAnimator == null)
            playerAnimator = playerMove.GetComponent<Animator>();

        if (playerMove != null && swordAttack == null)
            swordAttack = playerMove.GetComponentInChildren<SwordAttack>();

        if (trail == null)
            trail = GetComponent<TrailRenderer>();

        defaultLocalPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        if (trail == null || playerMove == null)
            return;

        bool usingSword = playerMove.currentWeaponMode == PlayerMove.WeaponMode.Sword;
        bool moving = playerMove.IsTryingToMove || (playerRb != null && playerRb.linearVelocity.magnitude > minMoveSpeed);
        bool attacking = swordAttack != null && swordAttack.isAttacking;

        bool shouldTrail = usingSword && moving && !attacking;

        UpdateTrailPosition(shouldTrail);
        trail.emitting = shouldTrail;

        if (clearTrailWhenStopping && lastTrailingState && !shouldTrail)
            trail.Clear();

        lastTrailingState = shouldTrail;
    }

    private void UpdateTrailPosition(bool shouldTrail)
    {
        if (!useAnimationBasedPosition)
            return;

        if (!shouldTrail)
        {
            ApplyDefaultPositionIfNeeded();
            return;
        }

        string currentAnimationName = GetCurrentAnimationName();
        if (string.IsNullOrWhiteSpace(currentAnimationName))
        {
            ApplyDefaultPositionIfNeeded();
            return;
        }

        if (currentAnimationName == lastAnimationName)
            return;

        lastAnimationName = currentAnimationName;

        for (int i = 0; i < animationPoses.Count; i++)
        {
            AnimationTrailPose pose = animationPoses[i];
            if (pose == null || string.IsNullOrWhiteSpace(pose.animationName))
                continue;

            if (AnimationNameMatches(currentAnimationName, pose.animationName))
            {
                transform.localPosition = pose.localPosition;
                return;
            }
        }

        ApplyDefaultPositionIfNeeded();
    }

    private void ApplyDefaultPositionIfNeeded()
    {
        lastAnimationName = string.Empty;

        if (transform.localPosition != defaultLocalPosition)
            transform.localPosition = defaultLocalPosition;
    }

    private string GetCurrentAnimationName()
    {
        if (playerAnimator == null)
            return string.Empty;

        AnimatorClipInfo[] clips = playerAnimator.GetCurrentAnimatorClipInfo(0);
        if (clips == null || clips.Length == 0 || clips[0].clip == null)
            return string.Empty;

        return clips[0].clip.name;
    }

    private static bool AnimationNameMatches(string currentAnimationName, string configuredAnimationName)
    {
        return currentAnimationName.Equals(configuredAnimationName, StringComparison.OrdinalIgnoreCase)
            || currentAnimationName.IndexOf(configuredAnimationName, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
