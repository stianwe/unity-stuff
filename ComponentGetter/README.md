ComponentGetter
===========

Tired of always having to check if a game object has been created yet before getting its reference, so that you don't have to run the expensive GameObject.Find() every time you want to use one of the components attached to it? Then you have come to the right place!

ComponentGetter let you create and set the component reference even if the game object hasn't been created yet. The only time you need to care about if the game object has been created, is when you attempt to use on of its components. This method also only run GameObject.Find() once!

Instructions:<br/>
1. Download ComponentGetter.cs and import it into your Unity project.<br/>
2. When you want to create a reference to a component attached to another game object, simply use the ComponentGetter instead of the component itself. <br/>
Example:<br/>
```
// Not allowed
private SomeComponent _comp = Gameobject.Find("SomeGameObject").GetComponent<SomeComponent>();

// Declare the component here, and get its reference in Start()
private SomeComponent _comp;

void Start() {
    // Will only work if SomeGameObject has been created before this game object
    _comp = GameObject.Find("SomeGameObject").GetComponent<SomeComponent>();
}

// Declaring the ComponentGetter - This will never fail!
private ComponentGetter<SomeComponent> _componentGetter = new ComponentGetter<SomeComponent>("SomeGameObject"); 

public void UseComponent() {
    // Regular way of using the component
    _component.SomeMethod();
    // Using the component with ComponentGetter
    // Will only fail if the game object hasn't been created at this point
    _componentGetter.component.SomeMethod();
}
```
