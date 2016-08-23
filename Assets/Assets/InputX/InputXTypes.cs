/*
 * It is strongly recommended that you do not modify these enum definitions before reading the User Manual
 * That said, understanding how to modify them correctly is the key to getting the most out of InputX.
 * It is not difficult to learn, but it also not difficult to do something that silently breaks the script.
 * It will only take a minute or two!
 */

using UnityEngine;

public enum InputCode 
{
	// KeyCodes are organized in lines roughly by category so that you can hide an entire related set of keys 
	// with a single set of // at the front. Fewer displayed keys makes selecting input in the inspector easier.
	// Feel free to hide what you don't intend to use. You can't do any harm by hiding members (just don't move them around!).
	
	None = KeyCode.None, Backspace = KeyCode.Backspace, Tab = KeyCode.Tab, Clear = KeyCode.Clear, Return = KeyCode.Return, 
	Pause = KeyCode.Pause, Escape = KeyCode.Escape, Space = KeyCode.Space, Exclaim = KeyCode.Exclaim, DoubleQuote = KeyCode.DoubleQuote, 
	Hash = KeyCode.Hash, Dollar = KeyCode.Dollar, Ampersand = KeyCode.Ampersand, Quote = KeyCode.Quote, LeftParen, RightParen, Asterisk, Plus, Comma, Minus, Period, Slash, 
	//Alpha0 = KeyCode.Alpha0, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8 , Alpha9, 
	//Colon = KeyCode.Colon, SemiColon, Less, Equals, Greater, Question, At, 
	//LeftBracket = KeyCode.LeftBracket, Backslash, RightBracket, Caret, Underscore, BackQuote, 
	A = KeyCode.A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, 
	//Delete = KeyCode.Delete, Keypad0 = KeyCode.Keypad0, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9, KeypadPeriod, KeypadDivide, KeypadMultiply, KeypadMinus, KeypadPlus, KeypadEnter, KeypadEquals, 
	Numlock = KeyCode.Numlock, CapsLock, ScrollLock, RightShift, LeftShift, 
	RightControl = KeyCode.RightControl, LeftControl, RightAlt, LeftAlt, 
	RightCommand = KeyCode.RightCommand, LeftCommand = KeyCode.LeftCommand,
	//RightApple = KeyCode.RightApple, LeftApple = KeyCode.LeftApple, 
	//LeftWindows = KeyCode.LeftWindows, RightWindows, 
	//AltGr = KeyCode.AltGr, Help = KeyCode.Help, Print, SysReq, Break, Menu, 
	
	
	//Mouse0 = KeyCode.Mouse0, Mouse1 = KeyCode.Mouse1, Mouse2 = KeyCode.Mouse2, 
	//Mouse3 = KeyCode.Mouse3, Mouse4 = KeyCode.Mouse4, Mouse5 = KeyCode.Mouse5, Mouse6 = KeyCode.Mouse6, 
	
	/*
	JoystickButton0 = KeyCode.JoystickButton0, JoystickButton1 = KeyCode.JoystickButton1, JoystickButton2 = KeyCode.JoystickButton2, 
	JoystickButton3 = KeyCode.JoystickButton3, JoystickButton4 = KeyCode.JoystickButton4, JoystickButton5 = 335, 
	JoystickButton6 = 336, JoystickButton7 = 337, JoystickButton8 = 338, JoystickButton9 = 339, 
	JoystickButton10 = 340, JoystickButton11 = 341, JoystickButton12 = 342, JoystickButton13 = 343, JoystickButton14 = 344, 
	JoystickButton15 = 345, JoystickButton16 = 346, JoystickButton17 = 347, JoystickButton18 = 348, JoystickButton19 = 349, 
	*/
	
	// Skips all KeyCode members for the numbered joysticks. Feel free to add them yourself if desired.
	
	
	// 	End of KeyCode members
	// ---------------------------
	// 	Start of AxisCode members
	
	MouseAxisX = AxisCode.MouseX, MouseAxisY = AxisCode.MouseY, 

	//Remove the /* */ tags if you want to use these. Or instead, consider defining aliases for them.
	/*
	JoystickAxisX = AxisCode.JoystickX, JoystickAxisY = AxisCode.JoystickY, 
	JoystickAxis1 = AxisCode.Joystick1, JoystickAxis2 = AxisCode.Joystick2, 
	JoystickAxis3 = AxisCode.Joystick3, JoystickAxis4 = AxisCode.Joystick4, 
	JoystickAxis5 = AxisCode.Joystick5, JoystickAxis6 = AxisCode.Joystick6, 
	JoystickAxis7 = AxisCode.Joystick7, JoystickAxis8 = AxisCode.Joystick8, 
	*/
	
	KeyboardWS = AxisCode.KeyboardWS, KeyboardAD = AxisCode.KeyboardAD,
	
	// 	End of AxisCode members
	// -------------------------
	// 	Start of alias members (members with duplicate values but more convenient or specific names)
	
	MouseClickLeft = KeyCode.Mouse0, MouseClickRight = KeyCode.Mouse1,  MouseScrollWheelClick = KeyCode.Mouse2, 
	
	Num0 = KeyCode.Alpha0, Num1 = KeyCode.Alpha1, Num2 = KeyCode.Alpha2, Num3 = KeyCode.Alpha3, Num4 = KeyCode.Alpha4,
	Num5 = KeyCode.Alpha5, Num6 = KeyCode.Alpha6, Num7 = KeyCode.Alpha7, Num8 = KeyCode.Alpha8 , Num9 = KeyCode.Alpha9, 
	
	#if UNITY_EDITOR_WIN || !UNITY_EDITOR
	//Define WinXbox inputs here
	XboxButtonA = KeyCode.JoystickButton0, XboxButtonB = KeyCode.JoystickButton1, XboxButtonX = KeyCode.JoystickButton2, 
	XboxButtonY = KeyCode.JoystickButton3, XboxLeftBumper = KeyCode.JoystickButton4, XboxRightBumper = KeyCode.JoystickButton5, 
	XboxBackButton = KeyCode.JoystickButton6, XboxStartButton = KeyCode.JoystickButton7, 
	XboxLeftStickClick = KeyCode.JoystickButton8, XboxRightStickClick = KeyCode.JoystickButton9,

    XboxLeftTrigger = AxisCode.Joystick3, XboxRightTrigger = AxisCode.Joystick3, XboxLeftStickX = AxisCode.JoystickX, 
    XboxRightStickX = AxisCode.Joystick4, XboxLeftStickY = AxisCode.JoystickY, XboxRightStickY = AxisCode.Joystick5,
    XboxInvertedRightStickY = AxisCode.NegJoystick4,
	#endif
	
	#if UNITY_EDITOR_OSX || !UNITY_EDITOR
	MacXboxDPadUp = KeyCode.JoystickButton5, MacXboxDPadDown = KeyCode.JoystickButton6, MacXboxDPadLeft = KeyCode.JoystickButton7, 
	MacXboxDPadRight = KeyCode.JoystickButton8, MacXboxStartButton = KeyCode.JoystickButton9, 
	MacXboxBackButton = KeyCode.JoystickButton10, MacXboxLeftStickClick = KeyCode.JoystickButton11, 
	MacXboxRightStickClick = KeyCode.JoystickButton12, MacXboxLeftBumper = KeyCode.JoystickButton13, 
	MacXboxRightBumper = KeyCode.JoystickButton14, MacXboxCenterGuideButton = KeyCode.JoystickButton15, 
	MacXboxButtonA = KeyCode.JoystickButton16, MacXboxButtonB = KeyCode.JoystickButton17, 
	MacXboxButtonX = KeyCode.JoystickButton18, MacXboxButtonY = KeyCode.JoystickButton19,
	
	MacXboxLeftTrigger = AxisCode.Joystick5, MacXboxRightTrigger = AxisCode.Joystick6, MacXboxLeftStickX = AxisCode.JoystickX, 
	MacXboxRightStickX = AxisCode.Joystick3, MacXboxLeftStickY = AxisCode.JoystickY, MacXboxRightStickY = AxisCode.Joystick4,
	MacXboxInvertedRightStickY = AxisCode.NegJoystick4
	#endif
}

public enum AxisCode
{
/* NOTE: Unless you use this asset's packaged version of InputManager.asset, you will need to redefine this enum.
 * 
 * RULES for defining members of the AxisCode enum:
 * 1) The first member is the only one that may be explicitly assigned a value (and it must be).
 * 2) The value that is assigned to the first member must be greater than 429, or it will clash with KeyCode.
 * 3) Each member must correspond to an axis set up in Unity Input Settings with the exact same name, same capitalization, and no spaces.
 * Deviating from any of these rules will cause the script to throw errors or return incorrect data.
 * 
 *  The members listed below are defaults that work with the axes set up in the InputManager.asset file included with InputX.
 *  Feel free to add your own or delete any that you don't want. Each of these are associated with a different kind of axis.
 * 	They allow you to define an axis once, and then reference it by any number of names by creating members of InputCode assigned to
 * 	that AxisCode member (see the InputCode enum above for more information). However, they do not allow you to avoid using Unity's
 *  InputManager entirely. You will need it to make an inverted axis or create an axis that uses keys and buttons.
 */ 
	
	MouseX = 500, MouseY, MouseScroll, KeyboardWS, KeyboardAD, JoystickX, JoystickY,
	Joystick3, Joystick4, NegJoystick4, Joystick5, Joystick6, Joystick7, Joystick8, Joystick9, Joystick10
}