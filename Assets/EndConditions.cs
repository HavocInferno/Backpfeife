using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EndConditions : NetworkBehaviour
{
    public bool gameEnded = false;
	public EndScreenUI endScreenUI;

    void Start()
    {
		if (!isServer)
			return;

        NetworkServer.Spawn(gameObject);
	}

	public void CheckEndCondition()
    {
		if (gameEnded || !isServer)
			return;
		
		bool anyCrawlerAlive = false;
		foreach (Transform c in FindObjectOfType<PlayersManager>().players)
        {
			if (!c.GetComponentInChildren<Crawler>().isDead)
            {
				anyCrawlerAlive = true;
				break;
			}
		}
		if (!anyCrawlerAlive)
        {
			gameEnded = true;
			RpcTriggerLOSE();
			return;
		}
	}

	[ClientRpc]
	void RpcTriggerWIN()
    {
		Debug.Log("RPC: All enemies dead, WON!");
		endScreenUI.gameObject.SetActive(true);
		endScreenUI.SetEndScreen (true);
	}

	[ClientRpc]
	void RpcTriggerLOSE()
    {
		Debug.Log ("RPC: All crawlers dead, LOSE!");
		endScreenUI.gameObject.SetActive (true);
		endScreenUI.SetEndScreen (false);
	}
}
