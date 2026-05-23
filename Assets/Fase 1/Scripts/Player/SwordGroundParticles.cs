using UnityEngine;
using System;
using System.Collections.Generic;

public class SwordGroundParticles : MonoBehaviour
{
    [Serializable]
    public class AnimationParticlePose
    {
        [Tooltip("Nome do clip/estado de animação. Ex.: SashaRun, RunTop, RunDown.")]
        public string animationName;

        [Tooltip("Posição local que o objeto das partículas deve usar enquanto essa animação estiver tocando.")]
        public Vector3 localPosition;
    }

    public PlayerMove playerMove;
    public SwordAttack swordAttack;
    public Rigidbody2D playerRb;
    public ParticleSystem particles;

    [Header("Ativação")]
    public float minMoveSpeed = 0.1f;
    public bool stopAndClearWhenStopping = true;

    [Header("Posicionamento por animação")]
    public Vector3 defaultLocalPosition;
    public bool useAnimationBasedPosition = true;
    public List<AnimationParticlePose> animationPoses = new();

    private Animator playerAnimator;
    private string lastAnimationName;
    private bool lastEmissionState;

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

        if (particles == null)
            particles = GetComponent<ParticleSystem>();

        if (particles != null)
        {
            ParticleSystem.EmissionModule emissionModule = particles.emission;
            emissionModule.enabled = false;
        }

        defaultLocalPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        if (particles == null || playerMove == null)
            return;

        bool usingSword = playerMove.currentWeaponMode == PlayerMove.WeaponMode.Sword;
        bool moving = playerMove.IsTryingToMove || (playerRb != null && playerRb.linearVelocity.magnitude > minMoveSpeed);
        bool attacking = swordAttack != null && swordAttack.isAttacking;

        bool shouldEmit = usingSword && moving && !attacking;

        UpdateParticlePosition(shouldEmit);
        SetEmissionState(shouldEmit);
        lastEmissionState = shouldEmit;
    }

    private void SetEmissionState(bool shouldEmit)
    {
        ParticleSystem.EmissionModule emissionModule = particles.emission;
        emissionModule.enabled = shouldEmit;

        if (shouldEmit)
        {
            if (!particles.isPlaying)
                particles.Play();

            return;
        }

        if (stopAndClearWhenStopping && lastEmissionState)
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        else if (!lastEmissionState && particles.isPlaying)
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void UpdateParticlePosition(bool shouldEmit)
    {
        if (!useAnimationBasedPosition)
            return;

        if (!shouldEmit)
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
            AnimationParticlePose pose = animationPoses[i];
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
