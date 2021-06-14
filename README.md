# MicroState
A state machine library inspired by the Gang of Four State pattern.

This library is designed to make working with complex state machines as simple as possible.
The reason being, that state-machines are a really handy tool for designing systems, and while other
libraries do exist, they often use fluent APIs. In contrast to fluent APIs, the GoF state pattern
allows the logic of a state to be more easily isolated from other pieces of code.

A state class could look as simple as this:

```cs
    [ParentState(typeof(OnState))]
    public class BlueState : FlashlightState
    {
        public override void OnEnter()
        {
            Context.SetColor("Blue");
        }

        public override void OnColorButton()
        {
            SetState<RedState>();
        }
    }
```

For more details, look at the section Getting stated.

## Getting started
In order to get started with this library, a few things must be done.
* Add the library to the desired project.
* Create the state base class.
* Creating your states and statemachine.
* Use the statemachine.

### Add the library to the desired project
If using Visual Studio, right click on the desired project and select "Manage Nuget packages".
Under browse, you can then search for MicroState and install a desired version.

Alternatively, if not using Visual Studio, use the commands given on Nuget:
https://www.nuget.org/packages/MicroState/

### Create a state base class
Creating a state base class is simply determining what events are present in your system.
For example given a state machine for a colored flashlight, relevant events would be
a press of the power or color button. If needed, these events can take arguments.
Events are defined by creating a public virtual void method with some relevant parameters
and a fitting (most of the time empty) base implementation.

```cs
    public abstract class FlashlightState : State<FlashlightController>
    {
        /*
         * Events for the color button and power button
         */
        public virtual void OnPowerButton() { }
        public virtual void OnColorButton() { } 
    }
```

The base class can be a regular or abstract class that is derived from the MicroState.State<CT> class.
The type argument CT defines the context type, which can be used with the protected Context property.
More details are in the section.

### Creating your states and statemachine
#### The states
Now comes the interesting part. Given a statemachine you want to implement, you can now start creating its states.
Each state is new class which is derived from the state baseclass created in the previous section. The class must
have a parameter-less constructor. An example for a state taken from the flashlight example:

```cs
    public class OffState : FlashlightState
    {
        public override void OnEnter()
        {
            // Context is of the type given in the state base class as type parameter for State<CT>
            Context.TurnOff();
        }

        public override void OnPowerButton()
        {
            // Power button pressed. Change state to the On state
            SetState<OnState>();
        }
    }
```

The relevant events can now be handled by overriding their virtual method defined in the basestate,
aswell as the OnEnter and OnExit events, which are virtual methods from the MicroState.State<CT> class.
These methods will be called, when the statemachine exits or enters this state.

Actions on the context can then be performed in these methods. In the example above, when the statemachine enters the OffState,
the method TurnOff() is called on the context. This tells the flashlight to turn off its light, when the OffState is entered.
The Context property is present in all states derived from a valid state base class. Further, the context is given in the
constructor for the statemachine. This is demonstrated in the section "The statemachine".

Transitioning/switching to a different state is easy. Simply call the SetState<>(); function with type parameter set to the
state that should be entered. The statemachine will handle the calling of OnExit and OnEnter for the old and new state respectively.

The MicroState library also supports the creation of substates and orthogonal states. This can be achieved by creating states
as shown in the example below:
  
```cs
    public class OnState : FlashlightState
    {
        public override void OnEnter()
        {
            Context.TurnOn();
            SetState<RedState>();
            SetState<FullPowerState>();
        }

    }

    [ParentState(typeof(OnState))]
    public class RedState : FlashlightState
    {
        public override void OnEnter()
        {
            Context.SetColor("Red");
        }

        public override void OnColorButton()
        {
            SetState<GreenState>();
        }
    }
  
    [ParentState(typeof(OnState))]
    public class FullPowerState : FlashlightState
    {
        public override void OnEnter()
        {
            Context.SetIntesnity(1);
        }

        public override void OnPowerButton()
        {
            SetState<HalfPowerState>();
        }
    }
```
The example above shows the OnState, RedState and FullPowerState from the flashlight example.
In the RedState and FullPowerState are orthogonal states, aswell as substates of the OnState.
In order to declare them as substates, the state class needs the ParentState attribute, which
takes the type of the parent state (in this case OnState) as parameter.

When SetState is called,
if the new state is a substate of the current state, the substate is added to the state tree.
Once the a substate tells the statemachine to set the state to the OffState for example, the
statemachine will handle the calling of OnExit on all states, which would no longer be active.

Lastly, in order to create orthogonal states, simply call the SetState method with 2 different
substates. The statemachine will enter both states and also handle the OnExit and OnEnter method
call by iteself as soon as the states are entered/exited.

#### The statemachine
With the states defined, the statemachine can now be created and used:

```cs
    var fl = new FlashlightController();
    var sm = new StateMachine<FlashlightState, FlashlightController>(fl);
```

When creating the statemachine, simply pass the state base class, aswell as the context type as type parameter.
The actual parameter is then an instance which then is used in the states with the Context property.

### Use the statemachine

When the statemachine has been created, starting and passing events to the state machine is fairly simple:
```cs
    // Creation
    var fl = new FlashlightController();
    var sm = new StateMachine<FlashlightState, FlashlightController>(fl);
    
    // Usage
    sm.Start<OffState>();
    sm.Handle.OnPowerButton();
```

Starting the state machine follows by calling Start<>(); with a valid state.
A valid state is a subclass of FlashlightState, which has no ParentState attribute.
As soon as the method has been called. Events can be triggered by calling the relevant event on the handle.
In the example above, this is the last line.

The handle is a runtime implementation of the state baseclass, which will pass the event to all currently active
states. This includes all substates and orthogonal states.

### Summary
Hopefully the getting started section gives a sufficient overview on how to use the library.
If the instructions seem unclear, or if there is room for improvement, feel free to open up an issue.
