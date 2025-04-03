﻿using System;
using System.Collections.Generic;
using System.Linq;
using uLipSync;
using UnityEngine;

public class AnimatorController : AutoActor, ISubActor, ISubNode, ISubSentiment
{
    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private AnimationControllerEntry[] _genderSpecificControllers;

    [SerializeField]
    private uLipSyncTexture _lipSync;

    [SerializeField]
    private Transform eyebrows;

    [SerializeField]
    private float minEyebrowY = -0.1f;

    [SerializeField]
    private float maxEyebrowY = 0.1f;

    [SerializeField]
    private float speed = 2f;
    private Sentiment _sentiment;

    private Vector3 position;

    private void Update()
    {
        _animator.SetBool("Talking", ActorController.IsTalking);

        var mood = Mathf.Lerp(
            _animator.GetFloat("Mood"),
            _sentiment.Score,
            Time.deltaTime * speed);
        _animator.SetFloat("Mood", mood);

        var energy = Mathf.Lerp(
            _animator.GetFloat("Energy"),
            ActorController.Energy,
            Time.deltaTime * speed);
        _animator.SetFloat("Energy", energy);
        _animator.SetFloat("Speed", ActorController.Speed);

        var weight = ActorController.VoiceVolume;
        if (ActorController.IsTalking)
            weight += 0.5f;
        weight = Mathf.Lerp(
            _animator.GetLayerWeight(2),
            weight,
            Time.deltaTime * speed);
        _animator.SetLayerWeight(2, weight);
    }

    private void LateUpdate()
    {
        if (eyebrows == null)
            return;
        var score = (_sentiment.Score + 1f) / 2f;
        var position = new Vector3(
            eyebrows.localPosition.x,
            Mathf.Lerp(minEyebrowY, maxEyebrowY, score),
            eyebrows.localPosition.z);
        eyebrows.localPosition = Vector3.Lerp(
            eyebrows.localPosition,
            position,
            Time.deltaTime * speed);
    }

    public void Activate(ChatNode node)
    {

    }

    public void UpdateActor(ActorContext context)
    {
        if (_genderSpecificControllers.Length == 0)
            return;

        var gender = _genderSpecificControllers
            .FirstOrDefault(c => c.Pronouns == context.Reference.Pronouns);
        if (gender == null)
            gender = _genderSpecificControllers.First();

        _animator.runtimeAnimatorController = gender.Controller;

        if (_animator.HasState(2, Animator.StringToHash(context.Reference.Name)))
            _animator.Play(context.Reference.Name, 2);
    }

    public void UpdateSentiment(Sentiment sentiment)
    {
        if (sentiment == null)
            return;

        _animator.Play(sentiment.Name, 0);

        _lipSync.initialTexture = sentiment.Lips;
        _lipSync.textures.First().texture = sentiment.Lips;
        _sentiment = sentiment;

        if (ActorController.LookTarget != null)
            position = ActorController.LookTarget.position;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (ActorController.LookTarget == null)
            return;
        var score = Math.Abs(_sentiment.Score);
        _animator.SetLookAtPosition(position);
        _animator.SetLookAtWeight(1.0f,
            ActorController.Energy,
            score + 0.5f, score * 0.5f, 0.75f);
    }

    [Serializable]
    public class AnimationControllerEntry
    {
        public string Pronouns;
        public RuntimeAnimatorController Controller;
    }
}