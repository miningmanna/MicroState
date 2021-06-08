using MicroState;
using System;
using System.Collections.Generic;
using System.Text;

namespace Examples
{
    /*
     * Context to manipulate. In this example a flashlight, where the color of the light and the intensity can be changed.
     */
    public class Flashlight
    {
        public void TurnOn() => Console.WriteLine("Turned on.");
        public void TurnOff() => Console.WriteLine("Turned off.");
        public void SetColor(string color) => Console.WriteLine($"Set color to {color}.");
        public void SetIntesnity(double intensity) => Console.WriteLine($"Set intensity to {intensity}");
    }

    /*
     * State base class. All state types have to inherit from this class.
     * All relevant events are declared here as public virtual methods
     */
    public abstract class FlashlightState : State<Flashlight>
    {
        /*
         * Events for the color button and power button
         */
        public virtual void OnPowerButton() { }
        public virtual void OnColorButton() { } 
    }

    /*
     * Off state
     */
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

    public class OnState : FlashlightState
    {
        public override void OnEnter()
        {
            Context.TurnOn();
            SetState<RedState>();
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
    public class GreenState : FlashlightState
    {
        public override void OnEnter()
        {
            Context.SetColor("Green");
        }

        public override void OnColorButton()
        {
            SetState<BlueState>();
        }
    }

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

    [ParentState(typeof(OnState))]
    public class HalfPowerState : FlashlightState
    {
        public override void OnEnter()
        {
            Context.SetIntesnity(0.5);
        }

        public override void OnPowerButton()
        {
            SetState<OffState>();
        }
    }

    class FlashlightExample
    {

        public static void Run()
        {

            var fl = new Flashlight();
            var sm = new StateMachine<FlashlightState, Flashlight>(fl);
            sm.Start<OffState>();

            var run = true;
            while(run)
            {
                var key = Console.ReadKey().KeyChar;
                Console.WriteLine();
                switch(key)
                {
                    case 'p':
                    case 'P':
                        sm.Handle.OnPowerButton();
                        break;
                    case 'c':
                    case 'C':
                        sm.Handle.OnColorButton();
                        break;
                    case 'e':
                    case 'E':
                        run = false;
                        break;
                }
            }
        }

    }
}
