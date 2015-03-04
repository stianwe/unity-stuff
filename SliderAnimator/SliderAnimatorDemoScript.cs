using UnityEngine;

public class SliderAnimatorDemoScript : MonoBehaviour {

	void Update () {
        // Animate the slider when releasing the f key
        if (Input.GetKeyUp("f"))
        {
            // Retrieve the animator script from the slider which it is attached to (named simply "Slider").
            var animator = GameObject.Find("Slider").GetComponent<SliderAnimator>();
            // Set the wrap around action to print a single message on slider wrap around.
            animator.OnWrapAround = () => { Debug.Log("Slider wrapped around!"); };
            // Set the end action to print a single message when the slider animation ends.
            animator.OnEnd = () => { Debug.Log("Slider animation ended!"); };
            // Start the slider animation at 30%, end it at 40%, and make the animation decrement (go from right to left)
            animator.Animate(0.3f, 0.4f, false);
        }
	}
}
