using UnityEngine;

/// <summary>
/// A simple class which can be used to get a component attached to a game object,
/// which eliminates the need to specificaly know and care about the time of creation
/// for the game object.
/// </summary>
/// <typeparam name="T">
/// The type of the component which you want to get from the game object
/// </typeparam>
public class ComponentGetter<T> where T : Component
{
    private T _component;

    private readonly string _gameObjectName;

    private readonly GameObject _gameObject;

    /// <summary>
    /// Initializes a new ComponentGetter which can be used to retrieve 
    /// component T on the game object with the name specified.
    /// </summary>
    /// <param name="gameObjectName">
    /// The name of the game object which the component is attached to
    /// </param>
	public ComponentGetter(string gameObjectName)
	{
	    _gameObjectName = gameObjectName;
	}

    /// <summary>
    /// Initializes a new ComponentGetter which can be used to retrieve 
    /// component T on the given game object.
    /// </summary>
    /// <param name="gameObject">
    /// The game object which the component is attached to
    /// </param>
    public ComponentGetter(GameObject gameObject)
    {
        _gameObject = gameObject;
    }

    /// <summary>
    /// Gets the component.
    /// Will return null if the game object doesn't exist, or no component T
    /// is attached to the game object.
    /// </summary>
    public T Component
    {
        get
        {
            if (_component != null)
            {
                return _component;
            }
            if (_gameObject == null)
            {
                _gameObject = GameObject.Find(_gameObjectName);
            }
            if (_gameObject == null)
            {
                return null;
            }
            _component = _gameObject.GetComponent<T>();
            return _component;
        }
    }
}
