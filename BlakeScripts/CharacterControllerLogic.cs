using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// #DESCRIPTION OF CLASS#
/// This is the main character controller script that I worked on for my Capstone game.
/// I started with a stock character controller that my colleague found for the beginning prototype
/// and edited it extensively to control movement, input, collision, and weapon swapping
/// </summary>
public class CharacterControllerLogic : MonoBehaviour 
{
	// Inspector serialized
	[SerializeField]
	private Animator animator;
	[SerializeField]
	private BlakesCamera gamecam;
	[SerializeField]
	private float rotationDegreePerSecond = 1080f;
	[SerializeField]
	private float directionSpeed = 1f;
	[SerializeField]
	private float directionDampTime = 0.25f;
	[SerializeField]
	private float runSpeed = 1.5f;
	[SerializeField]
	private float speedDampTime = 0.05f;
	[SerializeField]
	private float fovDampTime = 3f;
	[SerializeField]
	private float jumpMultiplier = 1f;
	[SerializeField]
	private CapsuleCollider capCollider;
	[SerializeField]
	private float jumpDist = 1f;
	
	private List<Transform> weapons;
	private int currentWeapon = -1;
	private int numWeapons = 0;
	private bool noSwitch = false;
	private bool hasHammer = false;
	private Transform shieldBox;
	
	
	// Private global only
	private float leftX = 0f;
	private float leftY = 0f;
	private AnimatorStateInfo stateInfo;
	private AnimatorTransitionInfo transInfo;
	private float speed = 0f;
	private float charSpeed = 0f;
	private float direction = 0f;
	private float charAngle = 0f;
	private float leanAngle = 0f;
	private const float SPRINT_MULTIPLIER = 1.6f;	
	private const float SPRINT_FOV = 75.0f;
	private const float NORMAL_FOV = 60.0f;
	private float capsuleHeight;
	private bool grounded = false;
	private float lastY = 0f;
	
	// Hash all animation names for performance
    private int m_LocomotionId = Animator.StringToHash("Base Layer.1H Locomotion");
	private int m_2hLocomotionId = Animator.StringToHash("Base Layer.2H Locomotion");
	private int m_LocomotionPivotId = Animator.StringToHash("Base Layer.1H 180 Turn");
	private int m_LocomotionPivotTransId = Animator.StringToHash("Base Layer.1H Locomotion -> Base Layer.1H 180 Turn");
	
	private int m_1HslashTransId = Animator.StringToHash("Base Layer.1H Locomotion -> Base Layer.1H slash");
	private int m_2HslashTransId = Animator.StringToHash("Base Layer.2H Locomotion -> Base Layer.2H slash");
	private int m_1HblockHoldId = Animator.StringToHash("Base Layer.1H block");
	private int m_2HblockHoldId = Animator.StringToHash("Base Layer.2H block");
	private int m_1HblockTransIdleId = Animator.StringToHash("Base Layer.1H Idle -> Base Layer.1H block");
	private int	m_2HblockTransIdleId = Animator.StringToHash("Base Layer.2H Idle -> Base Layer.2H block");
	private int m_1HblockTransId = Animator.StringToHash("Base Layer.1H Locomotion -> Base Layer.1H block");
	private int	m_2HblockTransId = Animator.StringToHash("Base Layer.2H Locomotion -> Base Layer.2H block");
	private int m_1HjumpTransId = Animator.StringToHash("Base Layer.1H Locomotion -> Base Layer.LocomotionJump");
	private int m_2HjumpTransId = Animator.StringToHash("Base Layer.2H Locomotion -> Base Layer.LocomotionJump");
	
	//Audio clip
	public AudioClip important_pickup;
	
	public ParticleSystem particle;
	
	
	public Animator Animator
	{
		get{return this.animator;}
	}
	
	public GameObject Weapon
	{	
		get{return weapons[currentWeapon].gameObject;}
	}

	public float Speed
	{
		get{return this.speed;}
	}
	
	public float CharSpeed
	{
		get{return this.charSpeed;}
		set{this.charSpeed = value;}
	}
	
	public float Direction
	{
		get{return this.direction;}
		set{this.direction = value;}
	}
	
	public float CharAngle
	{
		get{return this.charAngle;}
		set{this.charAngle = value;}
	}
	
	public void SetCharSpeed(float f)
	{
		charSpeed = f;
	}
	
	public float LocomotionThreshold { get { return 0.1f; } }
	
	
	
	void Awake()
	{
		weapons = new List<Transform>();	
		shieldBox = transform.GetChild(0);
	}
	
	
	/// <summary>
	/// Use this for initialization.
	/// </summary>
	void Start() 
	{
		animator = GetComponent<Animator>();
		GetCamera();
		capCollider = GetComponent<CapsuleCollider>();
		capsuleHeight = capCollider.height;
		
		numWeapons = 2;
		weapons.Add(GameObject.FindGameObjectWithTag("1h").transform);
		weapons.Add(GameObject.FindGameObjectWithTag("2h").transform);
		SwitchWeapon();
		
		if(animator.layerCount >= 2)
		{
			animator.SetLayerWeight(1, 1);
		}		
	}
	
	/// <summary>
	/// Update is called once per frame.
	/// </summary>
	void Update() 
	{
		if (animator && gamecam.CamState != BlakesCamera.CamStates.FirstPerson)
		{
			stateInfo = animator.GetCurrentAnimatorStateInfo(0);
			transInfo = animator.GetAnimatorTransitionInfo(0);
			
			HandleInput();
			
			// Prevent climbing walls
			Vector3 pos;
			if (Terrain.activeTerrain != null)
			{
				pos = this.gameObject.transform.position - Terrain.activeTerrain.transform.position;
				TerrainData data = Terrain.activeTerrain.terrainData;
				float steep = data.GetSteepness(pos.x/data.size.x, pos.z/data.size.z);
				if (steep > 55 && grounded)
				{
					speed = speed*(1-(steep-55)/20);
					if (speed <0) speed = 0;
					Vector3 push = data.GetInterpolatedNormal(pos.x/data.size.x, pos.z/data.size.z);
					push = Vector3.RotateTowards(push, Vector3.down, Mathf.PI/4, 99f);
					transform.Translate(push * ((steep-55)/12f) * Time.deltaTime, Terrain.activeTerrain.transform);
				}
			}
			
			
			animator.SetFloat("Speed", speed, speedDampTime, Time.deltaTime);
			animator.SetFloat("Direction", direction, directionDampTime, Time.deltaTime);
			
			if (speed > LocomotionThreshold)	// Dead zone
			{
				if (!IsInPivot())
				{
					Animator.SetFloat("Angle", charAngle);
				}
			}
			if (speed < LocomotionThreshold && charAngle < 0.5f)    // Dead zone
			{
				animator.SetFloat("Direction", 0f);
				animator.SetFloat("Angle", 0f);
			}
			
			// Translate and rotate character
			if (IsInLocomotion() && gamecam.CamState != BlakesCamera.CamStates.Free && speed > LocomotionThreshold)	
			{		
				this.transform.eulerAngles = new Vector3(0f,this.transform.eulerAngles.y,0f);
				if (Mathf.Abs (charAngle) > 1f)
				{
					leanAngle = Mathf.Lerp(leanAngle, -charAngle/4f, Time.deltaTime);
					
					Vector3 rotationAmount = new Vector3(0f, Mathf.Lerp(0f, rotationDegreePerSecond * (charAngle < 0f ? -1f : 1f), Mathf.Abs(charAngle/180f)), 0f);
					Vector3 leanAmount = new Vector3(0f, 0f, leanAngle);
					Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime) * Quaternion.Euler(leanAmount);
	        		this.transform.rotation = (this.transform.rotation * deltaRotation);
					
				}
				else{leanAngle = 0f;}
				
				animator.speed = speed;
				
				Vector3 forwardVec = this.transform.forward;
				forwardVec.y = 0; forwardVec.Normalize();
				forwardVec *= speed * Time.deltaTime;
				this.transform.position += forwardVec;
				Vector3 newPlayerPosition = this.transform.position + this.transform.forward*0.3f;
						
				RaycastHit wallHit = new RaycastHit();			
				if (Physics.Linecast(this.transform.position + Vector3.up*0.5f, 
					newPlayerPosition + Vector3.up*0.5f, out wallHit))
				{
					if ((wallHit.collider.gameObject.layer == 15 ||
						wallHit.collider.gameObject.layer == 0 ||
						wallHit.collider.gameObject.layer == 14) &&
						!wallHit.collider.isTrigger) // Terrain layer
					{
						this.transform.position += wallHit.normal* speed * Time.deltaTime
							* Mathf.Abs(Vector3.Dot(forwardVec.normalized, wallHit.normal));
						
						//Check if another wall nearby
						Vector3 axisSign = Vector3.Cross(forwardVec, wallHit.normal);
						Vector3 bouncePosition = wallHit.point + transform.right*0.2f*(axisSign.y <= 0 ? -1f : 1f);
						RaycastHit bounceHit = new RaycastHit();
						if (Physics.Linecast(wallHit.point, bouncePosition, out bounceHit))
						{
							Debug.DrawLine(wallHit.point, bouncePosition, Color.blue);
							this.transform.position += bounceHit.normal* speed * Time.deltaTime
							* Mathf.Abs(Vector3.Dot(forwardVec.normalized, bounceHit.normal));
						}
						else
						{
							Debug.DrawLine(newPlayerPosition + Vector3.up*0.5f,
								this.transform.position + Vector3.up*0.5f, Color.red, 5f);
						}
					}
				}
			}
			else{animator.speed = 1;}
			
			if (IsInJump() || IsInSlash () || IsInBlock())
			{
				animator.speed = 1;
			}
			
			// Jump physics
			if (IsInJump())
			{
				float oldY = transform.position.y;
				transform.Translate(Vector3.up * jumpMultiplier * animator.GetFloat("JumpCurve") * Time.deltaTime);
				if (IsInJump())
				{
					transform.Translate(Vector3.forward * Time.deltaTime * jumpDist);
				}
				capCollider.height = capsuleHeight + (animator.GetFloat("CapsuleCurve") * 0.5f);
				if (gamecam.CamState != BlakesCamera.CamStates.Free)
				{
					gamecam.ParentRig.Translate(Vector3.up * (transform.position.y - oldY) * 0.5f);
				}
			}
			
			// Rotate during block
			if (IsInBlock())
			{
				if (Mathf.Abs (charAngle) > 1f)
				{
					Vector3 rotationAmount = new Vector3(0f, Mathf.Lerp(0f, rotationDegreePerSecond * (charAngle < 0f ? -1f : 1f), Mathf.Abs(charAngle/180f)), 0f);
					Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
	        		this.transform.rotation = (this.transform.rotation * deltaRotation);	
				}
				shieldBox.SendMessage("enableBox");
			}
			else
			{
				shieldBox.SendMessage("disableBox");
			}
			
			//Debug.Log(lastY - this.transform.position.y);
			lastY = this.transform.position.y;
		} 
	}
	
	// Handle input from the controller
	void HandleInput()
	{
		// Pull values from controller/keyboard
		leftX = Input.GetAxis("Horizontal");
		leftY = Input.GetAxis("Vertical");
		
		charAngle = 0f;
		direction = 0f;	
		charSpeed = 0f;
		
		// Translate controls stick coordinates into world/cam/character space
    	StickToWorldspace(this.transform, gamecam.transform, ref direction, ref charSpeed, ref charAngle, IsInPivot());	
		
		// Press Q to sprint
		if (Input.GetButton("Sprint"))
		{
			//if (speed < charSpeed)
				//speed = charSpeed;
			speed = Mathf.Lerp(speed, charSpeed*SPRINT_MULTIPLIER, Time.deltaTime*5f);
			gamecam.camera.fieldOfView = Mathf.Lerp(gamecam.camera.fieldOfView, NORMAL_FOV, fovDampTime * Time.deltaTime);
		}
		else
		{
			speed = Mathf.Lerp(speed, charSpeed, Time.deltaTime*5f);
			gamecam.camera.fieldOfView = Mathf.Lerp(gamecam.camera.fieldOfView, NORMAL_FOV, fovDampTime * Time.deltaTime);		
		}
		
		if (Input.GetButton("Jump"))
		{
			animator.SetBool("Jump", true);
		}
		else
		{
			animator.SetBool("Jump", false);
		}
		// Press X to jump
		if (Input.GetButton("Slash"))
		{
			//speed = 0;
			animator.SetBool("Slash", true);
		}
		else
		{
			animator.SetBool("Slash", false);
		}
		// Right click to block
		if (Input.GetButton("Block") && !animator.GetBool("Block"))
		{
			speed = 0;
			animator.SetBool("Block", true);
		}
		else if (!Input.GetButton("Block") && animator.GetBool("Block"))
		{
			animator.SetBool("Block", false);
		}
		
		// Press E to switch weapon
		if (Input.GetButton("SwitchWeapon") && hasHammer)
		{
			if (!noSwitch){
				SwitchWeapon();
				noSwitch = true;
			}
		}
		else
		{
			noSwitch = false;
		}
	}
	
	/// <summary>
	/// Any code that moves the character needs to be checked against physics
	/// </summary>
	void FixedUpdate()
	{							
		
	}
	

	void OnDrawGizmos()
	{	
	
	}
	
	
	
	public bool IsInSlash()
	{
		return transInfo.nameHash == m_1HslashTransId || 
			transInfo.nameHash == m_2HslashTransId;
	}
	
	public bool IsInBlock()
	{
		return stateInfo.nameHash == m_1HblockHoldId ||
			stateInfo.nameHash == m_2HblockHoldId ||
			transInfo.nameHash == m_1HblockTransId ||
			transInfo.nameHash == m_2HblockTransId ||
			transInfo.nameHash == m_1HblockTransIdleId ||
			transInfo.nameHash == m_2HblockTransIdleId;
	}
	
	public bool IsInJump()
	{
		return transInfo.nameHash == m_1HjumpTransId ||
			transInfo.nameHash == m_2HjumpTransId ||
			animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.LocomotionJump");
	}

	public bool IsInIdleJump()
	{
		return animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.IdleJump");
	}
	
	public bool IsInPivot()
	{
		return stateInfo.nameHash == m_LocomotionPivotId ||
			transInfo.nameHash == m_LocomotionPivotTransId;
	}

    public bool IsInLocomotion()
    {
        return stateInfo.nameHash == m_LocomotionId ||
			stateInfo.nameHash == m_2hLocomotionId ||
				IsInJump();
    }
	
	public void StickToWorldspace(Transform root, Transform camera, ref float directionOut, ref float speedOut, ref float angleOut, bool isPivoting)
    {
        Vector3 rootDirection = root.forward;
				
        Vector3 stickDirection = new Vector3(leftX, 0, leftY);
		
		speedOut = stickDirection.sqrMagnitude;
		if (speedOut > 1){speedOut = 1;}
		speedOut*=runSpeed;

        // Get camera rotation
        Vector3 CameraDirection = camera.forward;
        CameraDirection.y = 0.0f; // kill Y
        Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(CameraDirection));

        // Convert joystick input in Worldspace coordinates
		Vector3 moveDirection;
		if (speedOut <= LocomotionThreshold){moveDirection = root.forward;}
		else {moveDirection = referentialShift * stickDirection;}
		Vector3 axisSign = Vector3.Cross(moveDirection, rootDirection);
		
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), moveDirection, Color.green);
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), rootDirection, Color.magenta);
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), stickDirection, Color.blue);
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2.5f, root.position.z), axisSign, Color.red);
		
		float angleRootToMove = Vector3.Angle(rootDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);
		if (!isPivoting)
		{
			angleOut = angleRootToMove;
		}
		angleRootToMove /= 180f;
		
		directionOut = angleRootToMove * directionSpeed;	
	}	
	
	public void TurnOffAnim()
	{
		animator.SetBool("Death", false);
		animator.SetBool("Flinch", false);
	}
		
	public void OnCollisionEnter(Collision collide)
	{
		if (collide.gameObject.layer == 15) // Terrain
			grounded = true;
	}
	
	public void OnCollisionExit(Collision collide)
	{
		if (collide.gameObject.layer == 15) // Terrain
			grounded = false;
	}
	
	public void GetCamera()
	{
		gamecam = (BlakesCamera)GameObject.FindGameObjectWithTag("MainCamera").GetComponent("BlakesCamera");
	}
	
	public void SwitchWeapon()
	{	
		Transform connectW;
		
		Transform connectS = GameObject.FindGameObjectWithTag("ShieldConnect").transform;
		Transform shield = GameObject.FindGameObjectWithTag("Shield").transform;
		
		if(currentWeapon == -1){
			currentWeapon = 0;
		}
		else{
			weapons[currentWeapon].transform.position += new Vector3(0, -100, 0);
			weapons[currentWeapon].SendMessage("disableBox");
			currentWeapon++;
			if (currentWeapon >= numWeapons){currentWeapon = 0;}
		}
		
		if(weapons[currentWeapon].tag == "1h"){
			connectW = GameObject.FindGameObjectWithTag("SwordConnect").transform;
			shield.position = connectS.position;
			shield.rotation = connectS.rotation;
			shield.parent = connectS;
			animator.SetBool("2handed", false);
			
			GameObject.Find("Icon_Sword").guiTexture.enabled = true;
			GameObject.Find("Icon_Hammer").guiTexture.enabled = false;
		}
		else{
			connectW = GameObject.FindGameObjectWithTag("HammerConnect").transform;
			connectS.DetachChildren();
			shield.transform.position += new Vector3(0,-100,0);
			animator.SetBool("2handed", true);
			
			GameObject.Find("Icon_Sword").guiTexture.enabled = false;
			GameObject.Find("Icon_Hammer").guiTexture.enabled = true;
		}
		
		connectW.DetachChildren();
					
		weapons[currentWeapon].position = connectW.position;
		weapons[currentWeapon].rotation = connectW.rotation;
		weapons[currentWeapon].parent = connectW;
		weapons[currentWeapon].SendMessage("disableBox");
		
		AnimEvent[] events = GetComponent<AnimatorEvents>().Events;
		if(weapons[currentWeapon].tag == "1h")
		{
			events[0].fireTime = 0.35f;
			events[1].fireTime = 0.50f;
		}
		else
		{
			events[0].fireTime = 0.46f;
			events[1].fireTime = 0.69f;
		}
		for(int i = 0; i <= 2; i++)
		{
			events[i].target = weapons[currentWeapon];
			events[i].Init();
		}
	}
	
	// Interacting with items or objects
	public void OnTriggerEnter(Collider other)
	{
		if (gameObject.GetComponent<Health>().Dead)
			return;
		
		string tag = other.gameObject.tag;
		switch (tag)
		{
		case "HammerPickup":
			hasHammer = true;
			SwitchWeapon();
			audio.PlayOneShot(important_pickup);
			PlayerPrefs.SetInt("hammerPickup", -1);
			Destroy(other.gameObject);
			break;
			
		case "SwordHilt":
			PlayerPrefs.SetInt("hiltPickup", -1);
			audio.PlayOneShot(important_pickup);
			GameObject.Find("Menu Screens").GetComponent<PauseMenu>().CollectPiece();
			other.gameObject.SendMessage("EnableLadderLighting");
			Destroy(other.gameObject);
			break;
			
		case "TreasureChest":
			other.gameObject.SendMessage("OpenChest");
			break;
			
		case "health_pickup":
			gameObject.SendMessage("Heal");
			Destroy(other.gameObject);
			break;
			
		case "extra_heart":
			audio.PlayOneShot(important_pickup);
			particle.Play();
			gameObject.SendMessage("IncreaseMaxHealth");
			PlayerPrefsPlus.SetBool(other.gameObject.name,true);
			Destroy(other.gameObject);
			break;
			
			
		}
	}
}
