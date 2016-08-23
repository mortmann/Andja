using UnityEngine;
using System;

public class InputX : MonoBehaviour 
{
	// InputX must be attached to a single gameobject in order to run. It doesn't matter which, but there should only be one.

	public Settings userSettings;

	//flow control and error handling
	private int prevFrameCount;
	private int framesSkipped;
	private long framesSkippedSinceStart;
	
	//Axis state flags. Necessary for extending Up() and Down() functionality to the axes
	[Flags] private enum AxisState : byte {NotPressed = 0, Pressed = 1, PressedLastFrame = 2, Up = 4, Down = 8, Trigger = 128}
	
	//script management
	private static int axisCodeCount = Enum.GetNames(typeof(AxisCode)).Length; 
	private static int firstAxisCodeValue = ((int[])Enum.GetValues (typeof(AxisCode)))[0];
	private static AxisState[] axisStates;
	private static float[] axisValues;
	private static AxisCode[] triggerAxes;
	private static int axisIndex;
	private const InputCode lastKeyCode = (InputCode)KeyCode.Joystick4Button19;
	private static InputCode namedInputCode;

	public static bool checkInput;
	public static Settings settings;
	
	void Awake()
	{
		axisStates = new AxisState[axisCodeCount];
		axisValues = new float[axisCodeCount];
		UpdateTriggerAxes ();
	}
	
	void FixedUpdate () 
	{
		if (prevFrameCount != Time.frameCount)
		{
			if (settings.trackAxisStates) SetAxisStates ();

			prevFrameCount = Time.frameCount;
		}
		
		else
		{	
			checkInput = false;
		}
	}
	
	void Update()
	{
		checkInput = true;

		if (prevFrameCount != Time.frameCount)
		{
			if (settings.trackAxisStates) SetAxisStates ();

			LogFixedUpdateSkip ();
		}

		if (settings.globalTimedInput) UpdateTimedInputClocks();

		if (settings.updateTriggerAxes) { UpdateTriggerAxes (); settings.updateTriggerAxes = false; }

		//Update intentionally does not update prevFrameCount in order to keep track of the number of missed FixedUpdates
	}
	
	//static methods and 	overloads

	public static bool Down (InputCode input, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
		{
			if (input <= lastKeyCode) 
				return Input.GetKeyDown ((KeyCode)input); 
			
			else
				return (axisStates[(int)input - firstAxisCodeValue] & AxisState.Down) == AxisState.Down;
		}

		return false;
	}
	
	public static bool Down (InputCode[] inputArray, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
			foreach (InputCode input in inputArray)
			{
				if (input <= lastKeyCode) 
					{ if (Input.GetKeyDown ((KeyCode)input)) return true; }

				else
					if ((axisStates[(int)input - firstAxisCodeValue] & AxisState.Down) == AxisState.Down) return true;
			}

		return false;
	}
	
	public static bool Down (KeyCode input, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
			return Input.GetKeyDown (input);

		return false;
	}
	
	public static bool Down (KeyCode[] inputArray, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
			foreach (KeyCode input in inputArray)
				if (Input.GetKeyDown (input)) return true;

		return false;
	}
	
	public bool Down (AxisCode input, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
			return (axisStates[(int)input - firstAxisCodeValue] & AxisState.Down) == AxisState.Down;

		return false;
	}
	
	public static bool Down (AxisCode[] inputArray, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
			foreach (InputCode input in inputArray)
				if ((axisStates[(int)input - firstAxisCodeValue] & AxisState.Down) == AxisState.Down) return true;
			
		return false;
	}

	public static bool Down (string inputName, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
			switch (ParseInputName(inputName))
			{	
			case 1: 
				return Down (namedInputCode);
			case 2: 
				return Input.GetKeyDown (inputName);
			case 3: 
				throw new InputXException ("InputX.Down() cannot be checked for the user-defined axis " + "\"" + inputName + "\"" + " because it has not been added as a member of the AxisCode enum. Go to the bottom of InputX.cs to add it.");
			default:
				throw new ArgumentException ("\"" + inputName + "\"" + " does not correspond to a member of any input type.");
			}
		
		return false;
	}

	public static bool Down (TimedInput timedInput, bool firstFixedOnly = true)
	{
		return Down (timedInput.inputs, firstFixedOnly);
	}
	
	public static bool Up (InputCode input, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
		{
			if (input <= lastKeyCode) 
				return Input.GetKeyUp ((KeyCode)input); 
			
			else
				return (axisStates[(int)input - firstAxisCodeValue] & AxisState.Up) == AxisState.Up;
		}
		
		return false;
	}
	
	public static bool Up (InputCode[] inputArray, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
			foreach (InputCode input in inputArray)
			{
				if (input <= lastKeyCode) 
					{ if (Input.GetKeyUp ((KeyCode)input)) return true; }
				
				else
					if ((axisStates[(int)input - firstAxisCodeValue] & AxisState.Up) == AxisState.Up) return true;	
			}

		return false;
	}
	
	public static bool Up (KeyCode input, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
			return Input.GetKeyUp (input);

		return false;
	}
	
	public static bool Up (KeyCode[] inputArray, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
			foreach (KeyCode input in inputArray)
				if (Input.GetKeyUp (input)) return true;

		return false;
	}
	
	public static bool Up (AxisCode input, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
			return (axisStates[(int)input - firstAxisCodeValue] & AxisState.Up) == AxisState.Up;

		return false;
	}
	
	public static bool Up (AxisCode[] inputArray, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
			foreach (InputCode input in inputArray)
				if ((axisStates[(int)input - firstAxisCodeValue] & AxisState.Up) == AxisState.Up) return true;	
			
		return false;
	}

	public static bool Up (string inputName, bool firstFixedOnly = true)
	{
		if (checkInput || !firstFixedOnly)
		{
			switch (ParseInputName(inputName))
			{	
			case 1: 
				return Up (namedInputCode);
			case 2: 
				return Input.GetKeyUp (inputName);
			case 3: 
				throw new InputXException ("InputX.Up() cannot be checked for the user-defined axis " + "\"" + inputName + "\"" + " because it has not been added as a member of the AxisCode enum. Go to the bottom of InputX.cs to add it.");
			default:
				throw new ArgumentException ("\"" + inputName + "\"" + " does not correspond to a member of any input type defined in Unity Input Settings or InputX.");
			}
		}
		
		return false;
	}

	public static bool Up (TimedInput timedInput, bool firstFixedOnly = true)
	{
		return Up (timedInput.inputs, firstFixedOnly);
	}
	
	public static bool Pressed (InputCode input, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
		{	
			if (input <= lastKeyCode)
				return Input.GetKey ((KeyCode)input);
			
			else
				if (settings.trackAxisStates)
					return ((axisStates[(int)input - firstAxisCodeValue] & AxisState.Pressed) == AxisState.Pressed);
				else 
					return Input.GetAxisRaw ( ((AxisCode)input).ToString() ) != 0;
		}

		return false;
	}
	
	public static bool Pressed (InputCode[] inputArray, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
			foreach (InputCode input in inputArray)
			{
				if (input <= lastKeyCode)
					{ if (Input.GetKey ((KeyCode)input)) return true; }

				else 
					if (settings.trackAxisStates)
						return ((axisStates[(int)input - firstAxisCodeValue] & AxisState.Pressed) == AxisState.Pressed);
					else 
						return Input.GetAxisRaw ( ((AxisCode)input).ToString() ) != 0;
			}

		return false;
	}
	
	public static bool Pressed (KeyCode input, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
			return Input.GetKey ((KeyCode)input);
	
		return false;
	}
	
	public static bool Pressed (KeyCode[] inputArray, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
			foreach (KeyCode input in inputArray)
				if (Input.GetKey (input)) return true;
			
		return false;
	}
	
	public static bool Pressed (AxisCode input, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput) {
			if (settings.trackAxisStates)
				return ((axisStates[(int)input - firstAxisCodeValue] & AxisState.Pressed) == AxisState.Pressed);
			else 
				return Input.GetAxisRaw ( ((AxisCode)input).ToString() ) != 0;
		}

		return false;	
	}
	
	public static bool Pressed (AxisCode[] inputArray, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
			foreach (InputCode input in inputArray) {
				if (settings.trackAxisStates)
					return ((axisStates[(int)input - firstAxisCodeValue] & AxisState.Pressed) == AxisState.Pressed);
				else 
					return Input.GetAxisRaw ( ((AxisCode)input).ToString() ) != 0;
			}

		return false;	
	}

	public static bool Pressed (string inputName, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
			switch (ParseInputName(inputName))
			{	
			case 1: 
				return Pressed (namedInputCode);
			case 2: 
				return Input.GetKey (inputName);
			case 3: 
				return (Input.GetAxisRaw (inputName) != 0);
			default:
				throw new ArgumentException ("\"" + inputName + "\"" + " does not correspond to a member of any input type defined in Unity Input Settings or InputX.");
			}
		
		return false;
	}

	public static bool Pressed (TimedInput timedInput, bool firstFixedOnly = false)
	{
		return Pressed (timedInput.inputs, firstFixedOnly);
	}
	
	public static float Axis (InputCode input, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
		{
			if (input <= lastKeyCode)
				return Input.GetKey ((KeyCode)input) ? 1 : 0;

			else {
				if (settings.trackAxisStates)
					return axisValues[(int)input - firstAxisCodeValue];
				else
					return Input.GetAxis( ((AxisCode)input).ToString() );
			}
		}

		return 0;
	}
	
	public static float Axis (InputCode[] inputArray, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
			foreach (InputCode input in inputArray)
			{
				if (input <= lastKeyCode)
					{ if (Input.GetKey ((KeyCode)input)) return 1; }

				else {
					if (settings.trackAxisStates)
						return axisValues[(int)input - firstAxisCodeValue];
					else
						return Input.GetAxis( ((AxisCode)input).ToString() );
				}
			}
		
		return 0;
	}
	
	public static float Axis (KeyCode input, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
			return Input.GetKey (input) ? 1 : 0;

		return 0;
	}
	
	public static float Axis (KeyCode[] inputArray, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
			foreach (KeyCode input in inputArray)
				if (Input.GetKey (input)) return 1;

		return 0;
	}
	
	public static float Axis (AxisCode input, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
		{
			if (settings.trackAxisStates)
				return axisValues[(int)input - firstAxisCodeValue];
			else
				return Input.GetAxis( ((AxisCode)input).ToString() );
		}

		return 0;
	}
	
	public static float Axis (AxisCode[] inputArray, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
			foreach (AxisCode input in inputArray)
			{
				if (settings.trackAxisStates)
					return axisValues[(int)input - firstAxisCodeValue];
				else
					return Input.GetAxis( ((AxisCode)input).ToString() );
			}
			
		return 0;
	}

	public static float Axis (string inputName, bool firstFixedOnly = false)
	{
		if (!firstFixedOnly || checkInput)
			switch (ParseInputName(inputName))
			{	
				case 1: 
					return Axis (namedInputCode);
				case 2: 
					return Input.GetKey (inputName) ? 1 : 0;
				case 3: 
					return Input.GetAxis (inputName);
				default:
					throw new ArgumentException ("\"" + inputName + "\"" + " does not correspond to a member of any input type.");
			}

		return 0;
	}

	public static float Axis (TimedInput timedInput, bool firstFixedOnly = false)
	{
		return Axis (timedInput.inputs, firstFixedOnly);
	}

	//system handling

	void SetAxisStates()
	{			
		for (int i = 0; i < axisCodeCount; i++)
		{
			//prepares frame by setting Pressed flag to PressedLastFrame, while preserving Trigger flag
			if ((axisStates[i] & AxisState.Pressed) == AxisState.Pressed)
				axisStates[i] = AxisState.PressedLastFrame | (axisStates[i] & AxisState.Trigger);
			else 
				axisStates[i] = AxisState.NotPressed | (axisStates[i] & AxisState.Trigger);

			if ((axisStates[i] & AxisState.Trigger) == AxisState.Trigger)
			{
				axisValues[i] = (Input.GetAxis(((AxisCode)(firstAxisCodeValue + i)).ToString()) + 1) / 2;
				
				if (axisValues[i] != .5f && axisValues[i] != 0) 
				{
					if ((axisStates[i] & AxisState.PressedLastFrame) == AxisState.PressedLastFrame)
						axisStates[i] |= (AxisState.Pressed);
					else 
						axisStates[i] |= (AxisState.Pressed | AxisState.Down);
				}
				else if (axisValues[i] != 0)
				{
					if ((axisStates[i] & AxisState.PressedLastFrame) == AxisState.PressedLastFrame)
						axisStates[i] |= (AxisState.Pressed);
					else 
						axisValues[i] = 0;
				}
				else if (axisValues[i] == 0)
				{
					if ((axisStates[i] & AxisState.PressedLastFrame) == AxisState.PressedLastFrame)
						axisStates[i] |= (AxisState.Up);;
				}

				axisStates[i] |= AxisState.Trigger;
			}
			else if ((axisValues[i] = Input.GetAxis ( ((AxisCode)(firstAxisCodeValue + i)).ToString() )) != 0) 
			{
				if ((axisStates[i] & AxisState.PressedLastFrame) == AxisState.PressedLastFrame)
					axisStates[i] |= (AxisState.Pressed);
				else 
					axisStates[i] |= (AxisState.Pressed | AxisState.Down);
			}
			else if ((axisStates[i] & AxisState.PressedLastFrame) == AxisState.PressedLastFrame)
				axisStates[i] |= (AxisState.Up);
		}
	}

	public static void UpdateTriggerAxes()
	{
		for (int i = 0; i < axisCodeCount; i++)
			axisStates[i] &= ~AxisState.Trigger;
		for (int i = 0; i < settings.triggers.Length; i++)
			axisStates[((int)settings.triggers[i]) - firstAxisCodeValue] |= AxisState.Trigger;

		if (settings.updateTriggerAxes)
			Debug.Log ("InputX has updated the trigger axes.");
	}
	
	private static byte ParseInputName (string inputName)
	{
		namedInputCode = 0;
		byte switchRoute = 0;
		try { namedInputCode = (InputCode)Enum.Parse (typeof(InputCode), inputName); }
		catch (ArgumentException)
		{
			try { Input.GetKey(inputName); }
			catch (UnityException)
			{
				try { Input.GetAxis (inputName); }
				catch (UnityException) { switchRoute = 4; }
				finally { if (switchRoute == 0) switchRoute = 3; }
			}
			finally { if (switchRoute == 0) switchRoute = 2; }
		}
		finally { if (switchRoute == 0) switchRoute = 1; }

		return switchRoute;
	}

	public static TimedInput[] timedInputArray;
	private static int timedInputIndex;

	private static void UpdateTimedInputClocks()
	{
		if (timedInputArray != null)
		{
			while (timedInputIndex < timedInputArray.Length)
			{
				if (timedInputArray[timedInputIndex].updateTimeGlobally)
					if (InputX.Pressed (timedInputArray[timedInputIndex].inputs))
						timedInputArray[timedInputIndex].clock += Time.unscaledDeltaTime;
					else
						timedInputArray[timedInputIndex].clock = 0;

				timedInputIndex++;
			}

			timedInputIndex = 0;
		}
	}

	void LogFixedUpdateSkip ()
	{
		framesSkipped = Time.frameCount - prevFrameCount;
		framesSkippedSinceStart += framesSkipped;
		
		if (settings.logDebugWarnings) 
			Debug.LogWarning ("Time: " + Time.time + "  InputX noticed that FixedUpdate skipped " + framesSkipped + 
			                  " frame(s). If your game is input sensitive, you may want to decrease your Fixed time step in Edit -> " +
			                  "Project Settings -> Time. There have been " + framesSkippedSinceStart + " total skipped frames " +
			                  "at a rate of " + (framesSkippedSinceStart / Time.timeSinceLevelLoad) + " skips per second.");
	}

	public static InputCode[] CopyArray (InputCode[] array)
	{
		InputCode[] newArray = new InputCode[array.Length];
		for (int i = 0; i < array.Length; i++) 
			newArray[i] = array[i];

		return newArray;
	}

	private class InputXException : Exception
	{
		public InputXException(string message) : base(message) {}
	}

	[Serializable]
	public class Settings
	{
		public bool logDebugWarnings = false;

		[Tooltip("Should InputX keep track of Up and Down events for axes, and translate marked trigger axes?")]
		public bool trackAxisStates = true;

		[Tooltip("Should InputX maintain clocks for TimedInput instances even when they are not being checked?")]
		public bool globalTimedInput = true;

		[Tooltip("What should be the default TimedInput tap limit/hold threshold?")]
		public float timedInputDefault = .25f;

		[Tooltip("Unity maps axis values to a range of -1 and 1. This means that on some controllers the triggers will problematically start at -1 with " +
		         "light pressure, and scale upward towards 1. This means 0 can alternately represent either a middle value or a zero value (no input). " +
		         "By adding an AxisCode member to this list, the script will resolve these problems and return readings of that axis in a range of 0 to 1 " +
		         "when the InputX.Axis() method is used.")]

		public AxisCode[] triggers;

		private Settings()
		{
				InputX.settings = this;
		}

		[Tooltip("Set to true to update the axes that should be translated to trigger values. Must be called manually after taggedTriggerAxis changes.")]
		public bool updateTriggerAxes;
	}
}

[Serializable] //An input class that simplifies various time sensitive input detection needs
public class TimedInput
{
	public InputCode[] inputs = {InputCode.None};
	public float holdThreshold;
	public float tapLimit;
	public bool updateTimeGlobally;
	[HideInInspector] public float clock;
	private int lastFrameUpdated = -1;
	private bool inStaticArray;

	//instance methods
	public bool Held
	{
		get
		{
			if (updateTimeGlobally && InputX.settings.trackAxisStates) 
			{
				if (!inStaticArray) 
					{AddToStaticArray(); inStaticArray = true;}

				return clock > holdThreshold;
			}
			else if (InputX.Pressed(inputs))
			{
				if (lastFrameUpdated != Time.frameCount) 
				{
					clock += Time.unscaledDeltaTime;
					lastFrameUpdated = Time.frameCount;
				}
				return clock > holdThreshold;
			}
			
			else clock = 0;
			
			return false;
		}
	}
	
	public bool Tapped
	{
		get
		{
			if (updateTimeGlobally && InputX.settings.globalTimedInput) 
			{
				if (!inStaticArray) 
					{AddToStaticArray(); inStaticArray = true;}

				return (clock <= tapLimit) && InputX.Up(inputs);
			}
			else
			{
				if (InputX.Pressed(inputs))
				{
					if (lastFrameUpdated != Time.frameCount) 
					{
						clock += Time.unscaledDeltaTime;
						lastFrameUpdated = Time.frameCount;
					}
				}
				else if (InputX.Up(inputs))
				{   
					bool returnVal = clock <= tapLimit;
					
					clock = 0;
					
					return returnVal;
				}
			}
			
			return false;
		}
	}

	//InputX method shortcuts
	public bool Down (bool firstFixedOnly = false) {
		return InputX.Down (this.inputs, firstFixedOnly);
	}

	public bool Up (bool firstFixedOnly = false) {
		return InputX.Up (this.inputs, firstFixedOnly);
	}

	public bool Pressed (bool firstFixedOnly = false) {
		return InputX.Pressed (this.inputs, firstFixedOnly);
	}

	public float Axis (bool firstFixedOnly = false) {
		return InputX.Axis (this.inputs, firstFixedOnly);
	}

	//system handling
	private void AddToStaticArray()
	{
		if (InputX.timedInputArray == null)
			InputX.timedInputArray = new TimedInput[] {this};
		
		else
		{
			TimedInput[] newArray = new TimedInput[InputX.timedInputArray.Length + 1];
			for (int i = 0; i < InputX.timedInputArray.Length; i++)
			{
				newArray[i] = InputX.timedInputArray[i];
			}
			newArray[InputX.timedInputArray.Length] = this;
			InputX.timedInputArray = newArray;
		}
	}

	//constructors
	public TimedInput()
	{
		//does not call AddToStaticArray() because the serializer calls the default constructor multiple times
		tapLimit = InputX.settings != null ? InputX.settings.timedInputDefault : .25f;
		holdThreshold = InputX.settings != null ? InputX.settings.timedInputDefault : .25f;
		inputs = new InputCode[] {InputCode.None};
	}

	public TimedInput (params InputCode[] argInputSelection)
	{
		if (!inStaticArray) {AddToStaticArray(); inStaticArray = true;}
		tapLimit = InputX.settings != null ? InputX.settings.timedInputDefault : .25f;
		holdThreshold = InputX.settings != null ? InputX.settings.timedInputDefault : .25f;
		inputs = argInputSelection;
	}

	public TimedInput (float tapLimitHoldThreshold, params InputCode[] argInputSelection)
	{
		if (!inStaticArray) {AddToStaticArray(); inStaticArray = true;}
		tapLimit = tapLimitHoldThreshold;
		holdThreshold = tapLimitHoldThreshold;
		inputs = argInputSelection;
	}

	public TimedInput (float argTapLimit, float argHoldThreshhold, params InputCode[] argInputSelection)
	{
		if (!inStaticArray) {AddToStaticArray(); inStaticArray = true;}
		tapLimit = argTapLimit;
		holdThreshold = argHoldThreshhold;
		inputs = argInputSelection;
	}
}