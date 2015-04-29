using UnityEngine;
using System.Collections;

//This is a helper script I wrote for my colleague so he could easily create
//and switch between various textboxes for cutscenes.
public class TextSwitch : MonoBehaviour {
	
	[System.Serializable]
	public class TextBox : System.Object
	{
		public GUITexture texture;
		public float switchTime;
	}
	
	public TextBox[] switchTimes;
	private static float timer;
	private TextBox currentBox;

	// Use this for initialization
	void Start (){
		timer = 0f;
		foreach (TextBox box in switchTimes)
			box.texture.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
		timer += Time.deltaTime;
		
		foreach (TextBox box in switchTimes)
		{
			if (timer >= box.switchTime)
			{
				if (currentBox != null)
					currentBox.texture.enabled = false;
				currentBox = box;
				box.texture.enabled = true;
			}
		}
	}
}