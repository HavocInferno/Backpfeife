using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AI;

/// <summary>
/// Generic character that can move, attack, has body and stats.
/// Can interact with other level objects such as traps and other characters.
/// Can be buffed or debuffed.
/// </summary>
public class GenericCharacter : NetworkBehaviour
{
    [Header("Generic Properties")]

    [SyncVar(hook = "OnChangeDead")]
    public bool isDead = false;

    [Header("Attacks")]

    public BasicAttack basicAttack;

    [Space(8)]

    public Renderer mainRenderer = null;

	public Animator anim; 

    /*public enum ActionState {
		NONE,
		ATTACK
	}
	[SyncVar(hook = "OnChangeActionState")]
	public ActionState actionState = ActionState.NONE;*/

    //#######################################################################
    //called after scene loaded

    protected virtual void Start()
    {
		var netanim = GetComponentInChildren<NetworkAnimator> ();
		if (netanim != null) {
			netanim.SetParameterAutoSend (0, true);
			netanim.SetParameterAutoSend (1, true);
		}
    }

    void OnValidate()
    {
        if (basicAttack == null)
        {
            Debug.LogWarningFormat("{0} | Basic attack not set", name);
        }
    }

    //###################### RPC CALLS #####################################
    //VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV
    [ClientRpc]
    protected void RpcAttack()
    {
        if (basicAttack != null)
            basicAttack.DoAttack(this);
        else
            Debug.LogErrorFormat("{0} | BasicAttack is not set", name);
    }

    //[ClientRpc]
    //public void RpcSetMaterial(bool cloak)
    //{
    //    foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
    //    {
    //        mr.material = cloak ? cloakMaterial : defaultMaterial;
    //        if (!cloak)
    //        {
    //            mr.material.color = playerColor;
    //        }
    //    }
    //}

    //###################### SYNCVAR HOOKS #####################################
    //VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV
    void OnChangeDead(bool dead)
    {
        if (!isDead)
        {
            isDead = true;
            OnDeath();
        }
    }

    protected virtual void OnDeath() { }

    public virtual void OnReceiveDamage(
        float amount,
        GenericCharacter attacker,
        Vector3 hitPoint,
        Vector3 hitDirection)
    {

		if (anim != null)
			anim.SetTrigger ("Hit");
    }

    public virtual void OnMakeDamage(
        float amount,
        GenericCharacter target,
        Vector3 hitPoint,
        Vector3 hitDirection)
    {
    }
}
