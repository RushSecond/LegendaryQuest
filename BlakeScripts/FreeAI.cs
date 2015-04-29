using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//This is a free script that we had found online. I modified it to fit our uses in the game

public class FreeAI : MonoBehaviour {
	//THE CHARACTER COLLISION LAYER FOR TARGETS
	public Transform AICharacter;
	
	public int CharacterCollisionLayer=15;
	//ENABLES MELEE COMBAT
	public bool EnableCombat=true;
	//THE TARGET WHICH HE FOLLOWS AND ATTACKS
	public Transform Target;
	//THE VECTOR OF THE TARGET
	private Vector3 CurrentTarget;
	//TARGET VISIBIL BOOL
	private bool TargetVisible;
	private bool MoveToTarget;
	//SPEED WHICH THE AI TURNS
	public float turnspeed=5;
	//SPEED WHICH AI RUNS
	public float runspeed=4;
	public int Damage=1;
	//public float AttackSpeed=1;
	public float AttackRange=1;
	
	//WHEN ATTACKING
	public GameObject weapon;
	
	//ANIMATIONS
	private bool stop;
	private bool Swing;
	public bool IsDead;
	private bool DeadPlayed = false;
	private bool EvenMoreDead = false;
	public string kill_string = "";
	public static GameObject deathEffect;
	public static GameObject healthDrop;
	
	private AnimatorStateInfo stateInfo;
	private AnimatorTransitionInfo transInfo;
	private int m_slashId = Animator.StringToHash("Base Layer.slash");
	private int m_LocomotionId = Animator.StringToHash("Base Layer.Locomotion");
	private int m_slashTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.slash");
	private int m_deathID = Animator.StringToHash("Base Layer.Death");
	private int m_flinchID = Animator.StringToHash("Base Layer.Flinch");
	
	private float Atimer;
	private bool startfollow;
	//PATHFINDING STUFF
	public bool EnableFollowNodePathFinding;
	public bool DebugShowPath;
	public float DistanceNodeChange=1.5f;
	public List<Vector3> Follownodes;
	private int curf;
	
	private Animator animator;
	
	void Awake(){
		if (kill_string != "" && PlayerPrefsPlus.GetBool(kill_string, false))
			Destroy(gameObject);
		
		if (deathEffect == null)
			deathEffect = GameObject.FindGameObjectWithTag("MDP");
		if (healthDrop == null)
			healthDrop = GameObject.FindGameObjectWithTag("health_pickup");
	}
	
	// Use this for initialization
	void Start () {
		if(AICharacter){}
		else AICharacter=transform;
		
		animator = GetComponent<Animator>();
	}
	
	public bool IsInSlash()
	{
		return stateInfo.nameHash == m_slashId ||
			transInfo.nameHash == m_slashTransId;
	}
	
	public bool IsInLocomotion()
	{
		return stateInfo.nameHash == m_LocomotionId;
	}
	
	// Update is called once per frame
	void Update ()
	{
		stateInfo = animator.GetCurrentAnimatorStateInfo(0);
		transInfo = animator.GetAnimatorTransitionInfo(0);
		
        //This was a decent first attempt to get death animations to play only once.
        //In hindsight, I should have used animator triggers.
		if(IsDead)
		{
			if(DeadPlayed)
			{
				if (stateInfo.nameHash == m_deathID && !EvenMoreDead)
				{
					animator.SetBool("Death", false);
					EvenMoreDead = true;
				}
			}
			else 
			{
				animator.SetBool("Death", true);
				DeadPlayed=true;
			}	
		}
		else
		{
			if (stateInfo.nameHash == m_flinchID)
				animator.SetBool("Flinch", false);
			
			//COMBAT BEHAVE
            //If the enemy has a target, then check how close you are to the target
            //Then either move forward or attack accordingly
			if(Target)
			{
				CurrentTarget=Target.position;
				float Tdist=Vector3.Distance(Target.position, transform.position);	
				if(Tdist<=AttackRange && !Target.gameObject.GetComponent<Health>().Dead){
					if(EnableCombat)
						animator.SetBool("Slash", true);								
					//MoveToTarget=false;
				}
				else 
				{
					animator.SetBool("Slash", false);
					//IF THE TARGET IS VISIBLE
					if(TargetVisible && !Target.gameObject.GetComponent<Health>().Dead)
					{
						MoveToTarget=true;
					}
					else
						MoveToTarget=false;
				}
					
				//RAYCAST VISION SYSTEM
				RaycastHit hit = new RaycastHit();	
				LayerMask lay=CharacterCollisionLayer;
				Vector3 pdir = (Target.transform.position - transform.position).normalized;
				float playerdirection = Vector3.Dot(pdir, transform.forward);
				Debug.DrawLine(transform.position, transform.position+transform.forward, Color.blue);
				if(Physics.Linecast(transform.position, Target.position, out hit, lay))
				{
					TargetVisible=false;
				}
				else
				{
					if(playerdirection > 0f)
					{
						startfollow=true;
						TargetVisible=true;	
					}
					else
						TargetVisible=false;
						
				}

			}
		
		
		//MOVES/RUNS TO TARGET
		if(MoveToTarget)
		{
			animator.SetFloat("Speed", runspeed);
			if(IsInLocomotion() && !IsInSlash())
			{
				transform.position += transform.forward * +runspeed * Time.deltaTime;
			}
		}
		else
		{
			animator.SetFloat("Speed", 0f);
		}
		
		
		//FOLLOW PATHFINDING
		if(TargetVisible){}
		else{
			if(EnableFollowNodePathFinding&startfollow){
			if(Follownodes.Count<=0)Follownodes.Add(CurrentTarget);
		
				RaycastHit hit = new RaycastHit();	
		LayerMask lay=CharacterCollisionLayer;
					
			if(Physics.Linecast(Follownodes[Follownodes.Count-1], Target.position, out hit, lay)){	
						Follownodes.Add(Target.position);
					
				}
				

				float dist=Vector3.Distance(transform.position, Follownodes[0]);
					if(dist<DistanceNodeChange){
						 Follownodes.Remove(Follownodes[0]);
					}
				}
			}
		} // end else not dead
		
		
		if(TargetVisible&Follownodes.Count>0){
				Follownodes.Clear();
				}
		
		if(DebugShowPath){
				if(Follownodes.Count>0){
				int listsize=Follownodes.Count;
				Debug.DrawLine(Follownodes[0], transform.position, Color.green);
			for (int i = 0; i < listsize; i++)
					if(i<Follownodes.Count-1){
					{
					Debug.DrawLine(Follownodes[i], Follownodes[i+1], Color.green);
				
					}
					
				}
				
			}
		
	
			
		
		//POINT AT TARGET
		if(Target){
			if(Follownodes.Count>0){
			transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Follownodes[0] - transform.position), turnspeed * Time.deltaTime);	
			}
			else{
			transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(CurrentTarget - transform.position), turnspeed * Time.deltaTime);	
				
			}
	
		}
			
		transform.eulerAngles = new Vector3(0,transform.eulerAngles.y,0);
	}
	}
	
	public void TurnOffAnim()
	{
		//animator.SetBool("Death", false);
		//animator.SetBool("Flinch", false);
	}
	
    //Creates a poof particle effect on enemies that die
	public void DestroySelf()
	{
		if (deathEffect)
		{ 
			GameObject poof = (GameObject)Instantiate(deathEffect,transform.position,Quaternion.LookRotation(Vector3.up));
			Destroy(poof, 2.5f);
		}
		Destroy(gameObject);
	}
	
    //50% chance to drop a health potion on death
	public void DropHealth()
	{
		float rng = Random.value;
		if (rng > 0.5)
			Instantiate(healthDrop, transform.position, Quaternion.identity);
	}
}
