﻿using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "Jump", menuName = "FPS Sample/Animation/AnimGraph/Jump")]
public class AnimGraph_Jump : AnimGraphAsset
{
    public AnimationClip animJump;
    
    [Tooltip("Jump height in animation. NOT actual ingame jump height")]
    public float jumpHeight = 1.7f; // Jump height of character in last frame of animation
    public AnimationClip animAimDownToUp;

    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph)
    {
        var animState = new Instance(entityManager, owner, graph, this);
        return animState;
    }
    
    class Instance : IAnimGraphInstance, IGraphState
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, AnimGraph_Jump settings)
        {
            m_EntityManager = entityManager;
            m_Owner = owner;

    
            m_additiveMixer = AnimationLayerMixerPlayable.Create(graph);
    
            m_animJump = AnimationClipPlayable.Create(graph, settings.animJump);
            m_animJump.SetApplyFootIK(true);
            m_animJump.SetDuration(settings.animJump.length);
            m_animJump.Pause();
            int port = m_additiveMixer.AddInput(m_animJump, 0);
            m_additiveMixer.SetLayerAdditive((uint)port, false);
            m_additiveMixer.SetInputWeight(port, 1);
    
            // Adjust play speed so vertical velocity in animation is matched with character velocity (so feet doesnt penetrate ground)
            var animJumpVel = settings.jumpHeight / settings.animJump.length;
            var characterJumpVel = Game.config != null ? Game.config.jumpAscentHeight / Game.config.jumpAscentDuration : animJumpVel; 
            playSpeed = characterJumpVel / animJumpVel;
    
    
            // Aim
            m_aimHandler = new AimVerticalHandler(m_additiveMixer, settings.animAimDownToUp);
        }
    
        public void Shutdown()
        {
        }
    
        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
        }
    
        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_additiveMixer;
            playablePort = 0;
        }
    
        public void UpdatePresentationState(bool firstUpdate, GameTime time, float deltaTime)
        {
            var animState = m_EntityManager.GetComponentData<CharAnimState>(m_Owner);
            animState.rotation = animState.aimYaw;

            if (firstUpdate)
                animState.jumpTime = 0;
            else
                animState.jumpTime += playSpeed*deltaTime;
            m_EntityManager.SetComponentData(m_Owner, animState);
        }

        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            var animState = m_EntityManager.GetComponentData<CharAnimState>(m_Owner);
            m_aimHandler.SetAngle(animState.aimPitch);
            m_animJump.SetTime(animState.jumpTime);
        }

        
        EntityManager m_EntityManager;
        Entity m_Owner;
        AnimationLayerMixerPlayable m_additiveMixer;
        AnimationClipPlayable m_animJump;
        AimVerticalHandler m_aimHandler;

        float playSpeed;
    }
}
