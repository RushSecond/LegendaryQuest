using UnityEngine;
using System.Collections;

//This script attaches to a trigger box and allows the player to switch to a new scene when colliding with it
public class levelTrigger : MonoBehaviour {
	
    //Enemies that you need to kill to unlock this level trigger
	public GameObject[] enemiesToKill;
	public bool killEnemies;
	
    //String keywords that can be set to the playerprefs that enable or block the player from using this trigger
	public string blockString = "";
	public string enableString = "";
	
    //Sets the target level and startlocation in that new level
	public string Level;
	public int startLocation;
	public AudioClip soundEffect;
	
	private bool blocked = false;
	
	void Start()
	{
        //Check block and enable strings
		if (blockString != "" && PlayerPrefsPlus.GetBool(blockString, false))
		{
			Debug.Log("Block string found");
			collider.enabled = false;
			blocked = true;
			return;
		}
		if (enableString != "" && !PlayerPrefsPlus.GetBool(enableString, false))
		{
			Debug.Log("Enable string not found");
			collider.enabled = false;
			blocked = true;
			return;
		}
		
        //If kill enemies boolean is true, you have to kill the target enemies to use this trigger
		if(killEnemies)
			collider.enabled = false;
	}
	
	void FixedUpdate()
	{
        //check if enemies are killed
		if(killEnemies && !blocked)
		{
			foreach (GameObject enemy in enemiesToKill)
			{
				if (enemy != null)
					return;
			}
			
			collider.enabled = true;
		}
	}
	
    void OnTriggerEnter(Collider other)
	{
        //If player collides with this, send him to the next level
		if (other.gameObject.tag == "Player")
		{
			Health playerStatus = other.gameObject.GetComponent<Health>();
			if (playerStatus.Dead)
				return;
			else
				playerStatus.Invincible = false;
			
			if(soundEffect != null)
			{
				audio.PlayOneShot(soundEffect);
			}
			
			GameObject.Find("Menu Screens").GetComponent<PauseMenu>().canPause = false;
			PlayerPrefs.SetInt("startLocation", startLocation);
	        AutoFade.LoadLevel(Level,1,1.4f,Color.black);
		}
    }
}