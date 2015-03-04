using UnityEngine;
using UnityEngine.UI;

public class SliderAnimator : MonoBehaviour
{
    private Slider _slider;

    private float _endVal;

    private bool _incrementing;

    private float _nextActionTime;

    /// <summary>
    /// How much the slider moves for each time delta
    /// </summary>
    public float Delta = 0.004f;

    /// <summary>
    /// How long (int seconds) between each time the slider moves
    /// </summary>
    public float TimeDelta = 0.01f;

    public bool IsAnimating { get; private set; }

    void Awake()
    {
        _slider = gameObject.GetComponent<Slider>();
    }

	void Update () {
	    if (IsAnimating && Time.time >= _nextActionTime)
	    {
	        UpdateNextActionTime();
            // Check end condition
	        if ((_incrementing && _slider.value < _endVal && _slider.value + Delta > _endVal) || 
                (!_incrementing && _slider.value > _endVal && _slider.value - Delta < _endVal))
	        {
	            _slider.value = _endVal;
	            IsAnimating = false;
                OnEnd();
	        }
            // Check if we are wrapping around
	        else if ((_incrementing && _slider.value + Delta >= _slider.maxValue) ||
                (!_incrementing && _slider.value - Delta <= _slider.minValue))
	        {
                OnWrapAround();
	            _slider.value = _incrementing ? _slider.minValue : _slider.maxValue;
	        }
	        // Increment slider
	        else
	        {
	            _slider.value = _slider.value + (_incrementing ? Delta : -Delta);
	        }
	    }
	}

    private void UpdateNextActionTime()
    {
        _nextActionTime = Time.time + TimeDelta;
    }

    /// <summary>
    /// Starts the animation from startVal to endVal.
    /// If endVal is less than startVal and increment is true,
    /// or endVal is greater than startVal and increment is false,
    /// the slider will wrap around and start from the beginning/end
    /// </summary>
    /// <param name="startVal">The value which the slider starts on</param>
    /// <param name="endVal">The value which the slider ends on</param>
    /// <param name="increment">
    /// True if the slider should increment (go right),
    /// false if it should decrement (go left).
    /// </param>
    public void Animate(float startVal, float endVal, bool increment=true)
    {
        _slider.value = startVal;
        if (startVal == endVal)
        {
            return;
        }
        IsAnimating = true;
        _incrementing = increment;
        _endVal = endVal;
        UpdateNextActionTime();
    }

    /// <summary>
    /// Called when the slider wraps around
    /// </summary>
    public virtual void OnWrapAround()
    {
        
    }

    /// <summary>
    /// Called when the slider is done animating
    /// </summary>
    public virtual void OnEnd()
    {
        
    }
}
