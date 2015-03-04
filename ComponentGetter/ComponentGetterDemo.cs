using UnityEngine;
using System.Collections;

public class ComponentGetterDemo : MonoBehaviour
{

    private TestScript _testScript;

    private ComponentGetter<TestScript> _testScriptGetter;

	// Use this for initialization
	void Start ()
	{
        // Will crash if the game object hasn't been created yet
	    _testScript = GameObject.Find("TestObject").GetComponent<TestScript>();
        // Will work even if the game object hasn't been created yet
        _testScriptGetter = new ComponentGetter<TestScript>("TestObject");
	}
	
	// Update is called once per frame
	void Update () {
	    if (Input.GetKeyUp("f"))
	    {
            // Ordinary way, which will already have crashed if the game object wasn't created when Start() ran
	        Debug.Log("Ordinary way: " + _testScript.GetTestString());
            // Using ComponentGetter, which will work as long as the game object has been created at this point
            Debug.Log("Using ComponentGetter: " + _testScriptGetter.Component.GetTestString());
	    }
	}
}
