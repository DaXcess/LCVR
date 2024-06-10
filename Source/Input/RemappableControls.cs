using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.Input;

public class RemappableControls : MonoBehaviour
{
    public RemappableControl[] controls;
    public ControllerIcons icons;
}

[Serializable]
public class RemappableControl
{
    public string controlName;
    public InputActionReference currentInput;
    public int bindingIndex = -1;
}

[Serializable]
public struct ControllerIcons
{
    public Sprite leftStick;
    public Sprite leftStickClick;
    public Sprite leftStickUp;
    public Sprite leftStickDown;
    public Sprite leftStickLeft;
    public Sprite leftStickRight;
    public Sprite leftPrimaryButton;
    public Sprite leftSecondaryButton;
    public Sprite leftTrigger;
    public Sprite leftGrip;

    public Sprite rightStick;
    public Sprite rightStickClick;
    public Sprite rightStickUp;
    public Sprite rightStickDown;
    public Sprite rightStickLeft;
    public Sprite rightStickRight;
    public Sprite rightPrimaryButton;
    public Sprite rightSecondaryButton;
    public Sprite rightTrigger;
    public Sprite rightGrip;

    public Sprite menuButton;
    public Sprite unknown;

    public Sprite this[string controlPath]
    {
        get
        {
            if (string.IsNullOrEmpty(controlPath))
                return null;

            var path = Regex.Replace(controlPath.ToLowerInvariant(), @"<[^>]+>([^ ]+)", "$1");
            var hand = path.Split('/')[0].TrimStart('{').TrimEnd('}');
            controlPath = string.Join("/", path.Split('/').Skip(1)).TrimStart('{').TrimEnd('}');
            
            return (hand, controlPath) switch
            {
                ("lefthand", "primary2daxis" or "thumbstick") => leftStick,
                ("lefthand", "primary2daxisclick" or "thumbstickclicked") => leftStickClick,
                ("lefthand", "primary2daxis/up" or "thumbstick/up") => leftStickUp,
                ("lefthand", "primary2daxis/down" or "thumbstick/down") => leftStickDown,
                ("lefthand", "primary2daxis/left" or "thumbstick/left") => leftStickLeft,
                ("lefthand", "primary2daxis/right" or "thumbstick/right") => leftStickRight,
                ("lefthand", "primarybutton" or "primarypressed") => leftPrimaryButton,
                ("lefthand", "secondarybutton" or "secondarypressed") => leftSecondaryButton,
                ("lefthand", "triggerbutton" or "trigger" or "triggerpressed") => leftTrigger,
                ("lefthand", "gripbutton" or "grip" or "grippressed") => leftGrip,
                
                ("righthand", "primary2daxis" or "thumbstick") => rightStick,
                ("righthand", "primary2daxisclick" or "thumbstickclicked") => rightStickClick,
                ("righthand", "primary2daxis/up" or "thumbstick/up") => rightStickUp,
                ("righthand", "primary2daxis/down" or "thumbstick/down") => rightStickDown,
                ("righthand", "primary2daxis/left" or "thumbstick/left") => rightStickLeft,
                ("righthand", "primary2daxis/right" or "thumbstick/right") => rightStickRight,
                ("righthand", "primarybutton" or "primarypressed") => rightPrimaryButton,
                ("righthand", "secondarybutton" or "secondarypressed") => rightSecondaryButton,
                ("righthand", "triggerbutton" or "trigger" or "triggerpressed") => rightTrigger,
                ("righthand", "gripbutton" or "grip" or "grippressed") => rightGrip,
                
                (_, "menu" or "menubutton" or "menupressed") => menuButton,
                
                _ => unknown
            };
        }
    }
}