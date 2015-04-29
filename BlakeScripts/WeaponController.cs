using UnityEngine;
using System.Collections;

//I wrote this script from scratch to control weapon behavior for both the player
//and enemies in our game. When the character swings, his weapon activates its hitbox
//at a certain time in the middle of the swing, allowing it to hit and damage enemies.
public class WeaponController : MonoBehaviour {
	
	public Animator animator;
	public AudioClip swordSwing;
	public Object hitEffect;
	public int damage = 1;
	
	private bool locked;
	private bool successfulHit;
	
	// Use this for initialization
	void Start () {
		collider.enabled = false;
		locked = false;
		successfulHit = false;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void swingSound()
	{
		if(Application.loadedLevelName != "cutScene_2" && Application.loadedLevelName != "cut_end")
		{
			audio.PlayOneShot(swordSwing);
		}
	}
	
    //Enable weapon hitbox so it hits enemies
	void enableBox()
	{
		if (!locked)
		{
			this.collider.enabled = true;
		}
	}
	
    //Disable hitbox so it doesn't hit things when idle
	void disableBox()
	{
		if (!locked)
		{
			this.collider.enabled = false;
			successfulHit = false;
		}
	}
	
    //Disable a hitbox permanently (when the character dies, for example)
	void lockBox()
	{
		disableBox();
		locked = true;
	}
	
	void unlockBox()
	{
		locked = false;
	}
	
	void OnTriggerEnter(Collider other)
	{
		int layer = other.gameObject.layer;
		if ((layer == 12) && !successfulHit) // Hit player shield
		{
			disableBox();
			animator.SetBool("Flinch", true);
		}
		else if (layer == 9 || layer == 11) // Hit enemy or player
		{
			Debug.Log(this.ToString() + "just hit!");
			successfulHit = true;
			for (int i = 0; i < damage; i++)
				other.gameObject.GetComponent("Health").SendMessage("Damage");
			if (hitEffect)
			{
				Object hit = Instantiate(hitEffect, transform.position, transform.rotation);
				Destroy(hit, 1f);
			}
		}
		else if (layer == 14 && damage >= 2) // Hit breakable
		{
			other.gameObject.SendMessage("Break");
		}
    }
}
