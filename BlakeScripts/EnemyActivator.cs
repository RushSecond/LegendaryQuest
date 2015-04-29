using UnityEngine;
using System.Collections;

//This is a quick script that you can attach to an empty object in unity
//When the player enters it's trigger area, it wakes up all its associated enemies
//You can use this to setup ambushes or just aggro a bunch of enemies at once
public class EnemyActivator : MonoBehaviour {

	public GameObject[] enemies;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
    void OnTriggerEnter(Collider other)
	{
        if (other.gameObject == GameObject.FindWithTag("Player"))
		{
			foreach (GameObject e in enemies)
			e.GetComponent("DistanceActive").SendMessage("Activate");
			Destroy(gameObject);
		}
    }
}
