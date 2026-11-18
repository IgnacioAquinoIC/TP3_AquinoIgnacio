using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace ContainerShip
{
    /// <summary>Centralised input. Every binding is an Input System path editable in the Inspector.</summary>
    [DefaultExecutionOrder(-200)]
    public class InputReader : MonoBehaviour
    {
        public static InputReader Instance { get; private set; }

        [Header("Movement")]
        public string moveUpBinding = "<Keyboard>/w";
        public string moveDownBinding = "<Keyboard>/s";
        public string moveLeftBinding = "<Keyboard>/a";
        public string moveRightBinding = "<Keyboard>/d";
        public string sprintBinding = "<Keyboard>/leftShift";
        public string leanLeftBinding = "<Keyboard>/q";
        public string leanRightBinding = "<Keyboard>/e";
        [Header("Look")]
        public string lookBinding = "<Mouse>/delta";
        [Header("Combat")]
        public string fireBinding = "<Mouse>/leftButton";
        public string aimBinding = "<Mouse>/rightButton";
        public string reloadBinding = "<Keyboard>/r";
        public string throwBinding = "<Mouse>/middleButton";
        public string slot1Binding = "<Keyboard>/1";
        public string slot2Binding = "<Keyboard>/2";
        public string slot3Binding = "<Keyboard>/3";
        public string slot4Binding = "<Keyboard>/4";
        [Header("Equipment / Interaction")]
        public string nightVisionBinding = "<Keyboard>/n";
        public string interactBinding = "<Keyboard>/f";
        public string restartBinding = "<Keyboard>/r";

        public InputAction Move, Look, LeanLeft, LeanRight, Sprint, Fire, Aim, Reload, Throw,
                           Slot1, Slot2, Slot3, Slot4, NightVision, Interact, Restart;
        readonly List<InputAction> _all = new List<InputAction>();

        void Awake()
        {
            Instance = this;
            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", moveUpBinding).With("Down", moveDownBinding)
                .With("Left", moveLeftBinding).With("Right", moveRightBinding);
            _all.Add(Move);
            Look = Make("Look", InputActionType.Value, lookBinding);
            LeanLeft = Make("LeanLeft", InputActionType.Button, leanLeftBinding);
            LeanRight = Make("LeanRight", InputActionType.Button, leanRightBinding);
            Sprint = Make("Sprint", InputActionType.Button, sprintBinding);
            Fire = Make("Fire", InputActionType.Button, fireBinding);
            Aim = Make("Aim", InputActionType.Button, aimBinding);
            Reload = Make("Reload", InputActionType.Button, reloadBinding);
            Throw = Make("Throw", InputActionType.Button, throwBinding);
            Slot1 = Make("Slot1", InputActionType.Button, slot1Binding);
            Slot2 = Make("Slot2", InputActionType.Button, slot2Binding);
            Slot3 = Make("Slot3", InputActionType.Button, slot3Binding);
            Slot4 = Make("Slot4", InputActionType.Button, slot4Binding);
            NightVision = Make("NightVision", InputActionType.Button, nightVisionBinding);
            Interact = Make("Interact", InputActionType.Button, interactBinding);
            Restart = Make("Restart", InputActionType.Button, restartBinding);
        }

        InputAction Make(string n, InputActionType t, string path)
        {
            var a = new InputAction(n, t, path); _all.Add(a); return a;
        }

        void OnEnable() { foreach (var a in _all) a.Enable(); }
        void OnDisable() { foreach (var a in _all) a.Disable(); }
        void OnDestroy() { foreach (var a in _all) a.Dispose(); if (Instance == this) Instance = null; }
    }
}
