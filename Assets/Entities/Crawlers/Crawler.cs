﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AI;
using UnityEngine.UI;

public class Crawler : GenericCharacter
{
    [Header("Player Properties")]

    public Text nameTag;
    [SyncVar(hook = "OnChangeName")]
    public string pName = "player";
    [SyncVar]
    public Color playerColor = Color.white;
    [SyncVar]
    public bool isMonster = false;
    public Sprite icon;
	public SkinnedMeshRenderer mesh;
	public GameObject ragdoll;
	Vector3 lastHitDir;

	[Header("UI (to be disabled for local)")]
	public GameObject tpsUI;

	[Header("Ping effects")]
	public GameObject pingMasterEffect;
	public GameObject pingLocalEffect;
	public float pingEffectLifetime;
	CUI_Ping pingIndicator;
	public float pingCooldownSeconds;
	float pingLastUseTime;

    public override void OnStartServer()
    {
        base.OnStartServer();

        //on the server, add yourself to the level-wide player list
        if (isServer)
        {
            Debug.Log("SERVER: " + pName + " is here.");
            //if (!isMonster)
                FindObjectOfType<PlayersManager>().players.Add(transform);
        }
    }

    //#######################################################################
    //called after scene loaded
    protected override void Start()
    {
        //if (crawlerClass != null)
        //    crawlerClass.Apply(this);

        base.Start();

        gameObject.name = pName;
        //foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
        //{
        //    mr.material = defaultMaterial;
        //    mr.material.color = playerColor;
        //}

		mesh = GetComponentInChildren<SkinnedMeshRenderer>();
		if (mesh != null)
			mesh.material.color = playerColor;
		else {
			ParticleSystem mm = GetComponentInChildren<ParticleSystem>();
			if (mm != null) {
				ParticleSystem.MainModule mm2 = mm.main;
				mm2.startColor = playerColor;
			}
		}
        
        nameTag.text = pName;

        //scale up the player object if this is the VR master [this is a temporary visualisation, to be removed once a proper Master representation is done]
        if (isMonster)
        {
            transform.localScale *= 1.3f;
            gameObject.tag = "Crawler_Monster";
            //gameObject.GetComponentInChildren<CrawlerController>().enabled = false;
            //gameObject.SetActive (false);
        }

        if (isLocalPlayer)
        {
			FindObjectOfType<CUI_lowStat>().Register(GetComponent<Stats>());
			if (tpsUI)
				tpsUI.SetActive (false);

            //if (!isMonster) {
				FindObjectOfType<CUI_crosshair> ().registerCrawler (this);
			//}
        }

        if (!isLocalPlayer && !isMonster)
        {
            // Add this player to the team status panel
            FindObjectOfType<TeamStatusPanel>().Register(this);
        }

		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Confined;

		if (!pingIndicator) {
			pingIndicator = FindObjectOfType<CUI_Ping> ();
		}
		if(pingIndicator) {
			pingIndicator.TogglePingIcon (true);
		}
		/*if (!FindObjectOfType<Master> ()) {
			pingIndicator.TogglePingIcon (false);
		}*/
    }

    //is called when the local client's scene starts
    public override void OnStartLocalPlayer()
    {
        float minDistance = 0.0f;
        NetworkStartPosition closestPosition = null;
        foreach (var point in FindObjectsOfType<NetworkStartPosition>())
        {
            float dist = Vector3.Distance(point.transform.position, transform.position);
            if (closestPosition == null || dist < minDistance)
            {
                minDistance = dist;
                closestPosition = point;
            }
        }

        // CANNOT ROTATE CAMERA WITH THE PLAYER.
        // transform.rotation = closestPosition.transform.rotation;

        if (GetComponent<NavMeshAgent>())
            GetComponent<NavMeshAgent>().enabled = true;

        /*if this is the VR Master, do:
        * enable VR if not done already
        * disable the default camera
        * enable the OpenVR cam rig
        * move transform up a bit (to compensate for the scale increase in Start()) [this is a temporary visualisation, to be removed once a proper Master representation is done]
        * append (VR MASTER) to the player name */
        if (isMonster)
        {
            /*UnityEngine.XR.XRSettings.enabled = true;
			QualitySettings.vSyncCount = 0; //this is an ugly fix, proper fix would be to globally save the player-set vsync state and recover it after the match is done.
            FindObjectOfType<CameraManager>().nonVRCamera.SetActive(false);
            FindObjectOfType<CameraManager>().vrCamera.SetActive(true);*/
            //transform.position += Vector3.up;
            pName = pName + " (MONSTER)";
            gameObject.tag = "Crawler_Monster";
        }
        /*if not, do:
		 * disable VR if not done already
		 * enable the default camera
		 * disable the OpenVR cam rig
		 * set this gameObject as the main cam target */
        //else
        //{
            UnityEngine.XR.XRSettings.enabled = false;
            //FindObjectOfType<CameraManager>().vrCamera.SetActive(false);
            FindObjectOfType<CameraManager>().nonVRCamera.SetActive(true);
            Camera.main.GetComponent<DungeonCamera>().target = this.gameObject;
			Camera.main.GetComponent<DungeonCamera>().shakeDistanceTarget = transform;
        //}
    }

	public override void OnReceiveDamage(
		float amount,
		GenericCharacter attacker,
		Vector3 hitPoint,
		Vector3 hitDirection)
	{
		base.OnReceiveDamage (amount,attacker,hitPoint, hitDirection);
		lastHitDir = hitDirection.normalized*amount; 
	}

    public void Attack()
    {
        if (!isLocalPlayer)
            return;

        //weapon firing. dumb and unoptimized.
        CmdAttack();
    }

	public void Ping() {
		if (!isLocalPlayer)
			return;
		
		if (!(Time.time - pingCooldownSeconds > pingLastUseTime))
			return;
		
		pingLastUseTime = Time.time;

		if (!pingIndicator) {
			pingIndicator = FindObjectOfType<CUI_Ping> ();
		}
		if(pingIndicator) {
			pingIndicator.TogglePingIcon (false);
			StartCoroutine (PingIndicatorReset ());
		}

		CmdPingMaster (); 

		//show local effect
		Destroy(Instantiate(pingLocalEffect, this.gameObject.transform), pingEffectLifetime);
	}

	IEnumerator PingIndicatorReset() {
		//reset cooldown
		yield return new WaitForSeconds(pingCooldownSeconds);
		pingIndicator.TogglePingIcon (true);
	}

	public void PingMastervis() {
		//show master effect
		Destroy(Instantiate(pingMasterEffect, this.gameObject.transform), pingEffectLifetime);
	}

    //###################### COMMAND CALLS #####################################
    //VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV
    [Command]
    void CmdAttack()
    {
        RpcAttack();
    }

	[Command]
	void CmdPingMaster() {
		//show master effect
		PingMastervis();
	}

    //###################### RPC CALLS #####################################
    //VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV

    //[ClientRpc]
    //void RpcFoo(int index) {}

    //###################### SYNCVAR HOOKS #####################################
    //VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV
    void OnChangeName(string newName)
    {
        nameTag.text = newName;
    }

	protected override void OnDeath()
	{
        FindObjectOfType<EndConditions>().CheckEndCondition();
		nameTag.text += " [DEAD]";
		gameObject.GetComponentInChildren<CrawlerController>().enabled = false;
		if (isLocalPlayer && isDead) {
			FindObjectOfType<EndScreenUI> ().SetDeathScreen (true);
		}
		if (ragdoll != null) {
			foreach (var rend in GetComponentsInChildren<Renderer>())
				rend.enabled = false;
			foreach (var col in GetComponentsInChildren<Collider>())
				col.enabled = false;
			GameObject rag = Instantiate (ragdoll, transform.position, transform.rotation);
			mesh = rag.GetComponentInChildren<SkinnedMeshRenderer>();
			if (mesh != null)
				mesh.material.color = playerColor; 
			Debug.Log (lastHitDir.magnitude);
			foreach (Rigidbody rig in rag.GetComponentsInChildren<Rigidbody>()) {
				rig.AddExplosionForce (lastHitDir.magnitude,transform.position+lastHitDir.normalized, 100,1,ForceMode.VelocityChange);
			}


		}
		/* here disable collider and renderer? also disable UI ring thing */
	}
}
