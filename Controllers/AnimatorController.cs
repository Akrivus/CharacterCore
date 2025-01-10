using System;
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
    private Transform headBone;

    [SerializeField]
    private Transform facePlane;

    [SerializeField]
    private float speed = 2f;
    private Sentiment _sentiment;

    [Header("Face Plane Transform")]
    public float x;
    public float y;
    public float z;
    public float xAngle;
    public float yAngle;
    public float zAngle;

    private void Update()
    {
        if (headBone == null || facePlane == null)
            return;
        facePlane.transform.position = headBone.position;
        facePlane.transform.rotation = headBone.rotation;

        facePlane.transform.Rotate(xAngle, yAngle, zAngle);
        facePlane.transform.Translate(x, y, z);
        _animator.SetBool("Talking", ActorController.IsTalking);

        var mood = Mathf.Lerp(
            _animator.GetFloat("Mood"),
            _sentiment.Score,
            Time.deltaTime * speed);
        _animator.SetFloat("Mood", mood);

        var weight = ActorController.VoiceVolume;
        if (ActorController.IsTalking)
            weight += 0.5f;
        weight = Mathf.Lerp(
            _animator.GetLayerWeight(1),
            weight,
            Time.deltaTime * speed);
        _animator.SetLayerWeight(1, weight);
    }

    public void Activate(ChatNode node)
    {

    }

    public void UpdateActor(ActorContext context)
    {
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
    }

    [Serializable]
    public class AnimationControllerEntry
    {
        public string Pronouns;
        public RuntimeAnimatorController Controller;
    }
}