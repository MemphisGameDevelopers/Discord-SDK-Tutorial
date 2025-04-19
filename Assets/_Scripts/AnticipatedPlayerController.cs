using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem.XR;
using UnityEngine.Playables;

public class AnticipatedPlayerController : NetworkBehaviour
{
    PlayableGraph playableGraph;
    Animator animator;
    AnimatorControllerPlayable ctrlPlayable;



    public override void OnNetworkSpawn()
    {
        animator = GetComponent<Animator>();
        playableGraph = PlayableGraph.Create();

        ctrlPlayable = AnimatorControllerPlayable.Create(playableGraph, animator.runtimeAnimatorController);

        

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void OnReanticipate(double lastRoundTripTime)
    {
        ctrlPlayable.SetTime(Time.timeAsDouble - lastRoundTripTime);
    }
}
