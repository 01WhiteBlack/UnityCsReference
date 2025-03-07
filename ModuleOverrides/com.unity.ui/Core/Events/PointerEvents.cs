// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A static class that holds pointer type values.
    /// </summary>
    /// <remarks>
    /// These values are used as the values for IPointerEvent.pointerType.
    /// </remarks>
    public static class PointerType
    {
        /// <summary>
        /// The pointer type for mouse events.
        /// </summary>
        public static readonly string mouse = "mouse";
        /// <summary>
        /// The pointer type for touch events.
        /// </summary>
        public static readonly string touch = "touch";
        /// <summary>
        /// The pointer type for pen events.
        /// </summary>
        public static readonly string pen = "pen";
        /// <summary>
        /// The pointer type for events created by unknown devices.
        /// </summary>
        public static readonly string unknown = "";

        internal static string GetPointerType(int pointerId)
        {
            if (pointerId == PointerId.mousePointerId)
                return mouse;
            else if (pointerId == PointerId.penPointerIdBase)
                return pen;
            else
                return touch;
        }

        // A direct manipulation device is a device where the user directly manipulates elements
        // (like a touch screen), without any cursor acting as an intermediate.
        internal static bool IsDirectManipulationDevice(string pointerType)
        {
            return ReferenceEquals(pointerType, touch)
                || ReferenceEquals(pointerType, pen);
        }
    }

    /// <summary>
    /// A static class that holds pointer ID values.
    /// </summary>
    /// <remarks>
    /// These values are used as the values for IPointerEvent.pointerId.
    /// </remarks>
    public static class PointerId
    {
        /// <summary>
        /// The maximum number of pointers the implementation supports.
        /// </summary>
        public static readonly int maxPointers = 32;
        /// <summary>
        /// Represents an invalid pointer ID value.
        /// </summary>
        public static readonly int invalidPointerId = -1;
        /// <summary>
        /// The mouse pointer ID.
        /// </summary>
        public static readonly int mousePointerId = 0;
        /// <summary>
        /// The base ID for touch pointers.
        /// </summary>
        /// <remarks>
        /// The pointer ID for a touch event is a number between touchPointerIdBase and touchPointerIdBase + touchPointerCount - 1.
        /// </remarks>
        public static readonly int touchPointerIdBase = 1;
        /// <summary>
        /// The number of touch pointers the implementation supports.
        /// </summary>
        /// <remarks>
        /// The pointer ID for a touch event is a number between touchPointerIdBase and touchPointerIdBase + touchPointerCount - 1.
        /// </remarks>
        public static readonly int touchPointerCount = 20;
        /// <summary>
        /// The base ID for pen pointers.
        /// </summary>
        /// <remarks>
        /// The pointer ID for a pen event is a number between penPointerIdBase and penPointerIdBase + penPointerCount - 1.
        /// </remarks>
        public static readonly int penPointerIdBase = touchPointerIdBase + touchPointerCount;
        /// <summary>
        /// The number of pen pointers the implementation supports.
        /// </summary>
        /// <remarks>
        /// The pointer ID for a pen event is a number between penPointerIdBase and penPointerIdBase + penPointerCount - 1.
        /// </remarks>
        public static readonly int penPointerCount = 2;

        internal static readonly int[] hoveringPointers =
        {
            mousePointerId
        };
    }

    /// <summary>
    /// This interface describes properties available to pointer events.
    /// </summary>
    public interface IPointerEvent
    {
        /// <summary>
        /// Gets the identifier of the pointer that sends the event.
        /// </summary>
        /// <remarks>
        /// If the mouse sends the event, the identifier is set to 0. If a touchscreen device sends the event, the identifier
        /// is set to the finger ID, which ranges from 1 to the number of touches the device supports.
        /// </remarks>
        int pointerId { get; }
        /// <summary>
        /// Gets the type of pointer that created the event.
        /// </summary>
        /// <remarks>
        /// This value is taken from the values defined in `PointerType`.
        /// </remarks>
        string pointerType { get; }
        /// <summary>
        /// Gets a boolean value that indicates whether the pointer is a primary pointer. True means the pointer is a primary
        /// pointer. False means it isn't.
        /// </summary>
        /// <remarks>
        /// A primary pointer is a pointer that manipulates the mouse cursor. The mouse pointer is always a primary pointer. For touch
        /// events, the first finger that touches the screen is the primary pointer. Once it is processed, a pointer event from a primary
        /// pointer generates compatibility mouse events.
        /// </remarks>
        bool isPrimary { get; }
        /// <summary>
        /// Gets a value that indicates which mouse button was pressed: 0 is the left button, 1 is the right button, 2 is the
        /// middle button.
        /// </summary>
        int button { get; }
        /// <summary>
        /// Gets a bitmask that describes the buttons that are currently pressed.
        /// </summary>
        /// <remarks>
        /// Pressing a mouse button sets a bit. Releasing the button clears the bit. The left mouse button sets/clears Bit 0.
        /// The right mouse button and pen barrel button set/clear Bit 1. The middle mouse button sets/clears Bit 2.
        /// The pen eraser sets/clears Bit 5. Additional buttons set/clear other bits.
        /// </remarks>
        int pressedButtons { get; }

        /// <summary>
        /// Gets the pointer position in the Screen or World coordinate system.
        /// </summary>
        Vector3 position { get; }
        /// <summary>
        /// Gets the pointer position in the current target's coordinate system.
        /// </summary>
        Vector3 localPosition { get; }
        /// <summary>
        /// Gets the difference between the pointer's position during the previous mouse event and its position during the
        /// current mouse event.
        /// </summary>
        Vector3 deltaPosition { get; }
        /// <summary>
        /// Gets the amount of time that has elapsed since the last recorded change in pointer values, in seconds.
        /// </summary>
        float deltaTime { get; }
        /// <summary>
        /// Gets the number of times the button was pressed.
        /// </summary>
        int clickCount { get; }
        /// <summary>
        /// Gets the amount of pressure currently applied by a touch.
        /// </summary>
        /// <remarks>
        /// If the device does not report pressure, the value of this property is 1.0f.
        /// </remarks>
        float pressure { get; }
        /// <summary>
        /// Gets the pressure applied to an additional pressure-sensitive control on the stylus.
        /// </summary>
        float tangentialPressure { get; }
        /// <summary>
        /// Gets the angle of the stylus relative to the surface, in radians
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates that the stylus is parallel to the surface. A value of pi/2 indicates that it is perpendicular to the surface.
        /// </remarks>
        float altitudeAngle { get; }
        /// <summary>
        /// Gets the angle of the stylus relative to the x-axis, in radians.
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates that the stylus is pointed along the x-axis of the device.
        /// </remarks>
        float azimuthAngle { get; }
        /// <summary>
        /// Gets the rotation of the stylus around its axis, in radians.
        /// </summary>
        float twist { get; }
        /// <summary>
        /// Specifies the angle of the pen relative to the X and Y axis respectively, in radians.
        /// </summary>
        Vector2 tilt { get; }
        /// <summary>
        /// Specifies the state of the pen. For example, whether the pen is in contact with the screen or tablet, whether the pen is inverted, and whether buttons are pressed.
        /// On macOS, penStatus will not reflect changes to button mappings.
        /// </summary>
        PenStatus penStatus { get; }
        /// <summary>
        /// Gets an estimate of the radius of a touch.
        /// </summary>
        /// <remarks>
        /// Add `radiusVariance` to get the maximum touch radius, subtract it to get the minimum touch radius.
        /// </remarks>
        Vector2 radius { get; }
        /// <summary>
        /// Gets the accuracy of the touch radius.
        /// </summary>
        /// <remarks>
        /// Add this value to the radius to get the maximum touch radius, subtract it to get the minimum touch radius.
        /// </remarks>
        Vector2 radiusVariance { get; }

        /// <summary>
        /// Gets flags that indicate whether modifier keys (Alt, Ctrl, Shift, Windows/Cmd) are pressed.
        /// </summary>
        EventModifiers modifiers { get; }
        /// <summary>
        /// Gets a boolean value that indicates whether the Shift key is pressed. True means the Shift key is pressed.
        /// False means it isn't.
        /// </summary>
        bool shiftKey { get; }
        /// <summary>
        /// Gets a boolean value that indicates whether the Ctrl key is pressed. True means the Ctrl key is pressed.
        /// False means it isn't.
        /// </summary>
        bool ctrlKey { get; }
        /// <summary>
        /// Gets a boolean value that indicates whether the Windows/Cmd key is pressed. True means the Windows/Cmd key
        /// is pressed. False means it isn't.
        /// </summary>
        bool commandKey { get; }
        /// <summary>
        /// Gets a boolean value that indicates whether the Alt key is pressed. True means the Alt key is pressed.
        /// False means it isn't.
        /// </summary>
        bool altKey { get; }
        /// <summary>
        /// Gets a boolean value that indicates whether the platform-specific action key is pressed. True means the action
        /// key is pressed. False means it isn't.
        /// </summary>
        /// <remarks>
        /// The platform-specific action key is Cmd on macOS, and Ctrl on all other platforms.
        /// </remarks>
        bool actionKey { get; }
    }

    internal interface IPointerEventInternal
    {
        bool triggeredByOS { get; set; }

        bool recomputeTopElementUnderPointer { get; set; }
    }

    internal static class PointerEventHelper
    {
        public static EventBase GetPooled(EventType eventType, Vector3 mousePosition, Vector2 delta, int button,
            int clickCount, EventModifiers modifiers)
        {
            if (eventType == EventType.MouseDown && !PointerDeviceState.HasAdditionalPressedButtons(PointerId.mousePointerId, button))
                return PointerDownEvent.GetPooled(eventType, mousePosition, delta, button, clickCount, modifiers);
            if (eventType == EventType.MouseUp && !PointerDeviceState.HasAdditionalPressedButtons(PointerId.mousePointerId, button))
                return PointerUpEvent.GetPooled(eventType, mousePosition, delta, button, clickCount, modifiers);
            return PointerMoveEvent.GetPooled(eventType, mousePosition, delta, button, clickCount, modifiers);
        }
    }

    /// <summary>
    /// This is the base class for pointer events.
    /// </summary>
    /// <remarks>
    /// Pointer events are sent by the mouse, touchscreen, or digital pens.
    ///
    /// By default, pointer events trickle down the hierarchy of VisualElements, and then bubble up
    /// back to the root. They are cancellable at any stage of the propagation.
    ///
    /// A cycle of pointer events occurs as follows:
    ///   - The user presses a mouse button, touches the screen, or otherwise causes a <see cref="PointerDownEvent"/> to be sent.
    ///   - If the user changes the pointer's state, a <see cref="PointerMoveEvent"/> is sent. Many PointerMove events can be sent.
    ///   - If the user doesn't change the pointer's state for a specific amount of time, a <see cref="PointerStationaryEvent"/> is sent.
    ///   - If the user cancels the loop, a <see cref="PointerCancelEvent"/> is sent.
    ///   - If the user doesn't cancel the loop, and either releases the last button pressed or releases the last touch, a <see cref="PointerUpEvent"/> is sent.
    ///   - If the initial PointerDownEvent and the PointerUpEvent occur on the same VisualElement, a <see cref="ClickEvent"/> is sent.
    ///
    /// </remarks>
    [EventCategory(EventCategory.Pointer)]
    public abstract class PointerEventBase<T> : EventBase<T>, IPointerEvent, IPointerEventInternal
        where T : PointerEventBase<T>, new()
    {
        // See HTML spec for pointer pressure: https://developer.mozilla.org/en-US/docs/Web/API/PointerEvent/pressure
        private const float k_DefaultButtonPressure = 0.5f;

        private bool m_AltitudeNeedsConversion = true;
        private bool m_AzimuthNeedsConversion = true;
        private float m_AltitudeAngle = 0f;
        private float m_AzimuthAngle = 0f;

        private bool m_TiltNeeded = true;
        private Vector2 m_Tilt = new Vector2(0, 0);

        /// <summary>
        /// Gets the identifier of the pointer that sent the event.
        /// </summary>
        /// <remarks>
        /// If the mouse sends the event, the identifier is set to 0. If a touchscreen device sends the event, the identifier
        /// is set to the finger ID, which ranges from 1 to the number of touches the device supports.
        /// </remarks>
        public int pointerId { get; protected set; }
        /// <summary>
        /// Gets the type of pointer that created the event.
        /// </summary>
        /// <remarks>
        /// This value is taken from the values defined in `PointerType`.
        /// </remarks>
        public string pointerType { get; protected set; }
        /// <summary>
        /// Gets a boolean value that indicates whether the pointer is a primary pointer. True means the pointer is a primary
        /// pointer. False means it isn't.
        /// </summary>
        /// <remarks>
        /// A primary pointer is a pointer that manipulates the mouse cursor. The mouse pointer is always a primary pointer. For touch
        /// events, the first finger that touches the screen is the primary pointer. Once it is processed, a pointer event from a primary
        /// pointer generates compatibility mouse events.
        /// </remarks>
        public bool isPrimary { get; protected set; }
        /// <summary>
        /// Gets a value that indicates which mouse button was pressed: 0 is the left button, 1 is the right button, 2 is the middle button.
        /// </summary>
        public int button { get; protected set; }
        /// <summary>
        /// Gets a bitmask that describes the buttons that are currently pressed.
        /// </summary>
        /// <remarks>
        /// Pressing a mouse button sets a bit. Releasing the button clears the bit. The left mouse button sets/clears Bit 0.
        /// The right mouse button sets/clears Bit 1. The middle mouse button sets/clears Bit 2. Additional buttons set/clear
        /// other bits.
        /// </remarks>
        public int pressedButtons { get; protected set; }
        /// <summary>
        /// Gets the pointer position in the Screen or World coordinate system.
        /// </summary>
        public Vector3 position { get; protected set; }
        /// <summary>
        /// Gets the pointer position in the current target's coordinate system.
        /// </summary>
        public Vector3 localPosition { get; protected set; }
        /// <summary>
        /// Gets the difference between the pointer's position during the previous mouse event and its position during the
        /// current mouse event.
        /// </summary>
        public Vector3 deltaPosition { get; protected set; }
        /// <summary>
        /// Gets the amount of time that has elapsed since the last recorded change in pointer values, in seconds.
        /// </summary>
        public float deltaTime { get; protected set; }
        /// <summary>
        /// Gets the number of times the button was pressed.
        /// </summary>
        public int clickCount { get; protected set; }
        /// <summary>
        /// Gets the amount of pressure currently applied by a touch.
        /// </summary>
        /// <remarks>
        /// If the device does not report pressure, the value of this property is 1.0f.
        /// </remarks>
        public float pressure { get; protected set; }
        /// <summary>
        /// Gets the pressure applied to an additional pressure-sensitive control on the stylus.
        /// </summary>
        public float tangentialPressure { get; protected set; }
        /// <summary>
        /// Gets the angle of the stylus relative to the surface, in radians
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates that the stylus is parallel to the surface. A value of pi/2 indicates that it is perpendicular to the surface.
        /// </remarks>
        public float altitudeAngle
        {
            // only calculate angle when requested
            get
            {
                if (m_AltitudeNeedsConversion)
                {
                    m_AltitudeAngle = TiltToAltitude(tilt);
                    m_AltitudeNeedsConversion = false;
                }
                return m_AltitudeAngle;
            }
            protected set
            {
                m_AltitudeNeedsConversion = true;
                m_AltitudeAngle  = value;
            }
        }
        /// <summary>
        /// Gets the angle of the stylus relative to the x-axis, in radians.
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates that the stylus is pointed along the x-axis of the device.
        /// </remarks>
        public float azimuthAngle
        {
            // only calculate angle when requested
            get
            {
                if (m_AzimuthNeedsConversion)
                {
                    m_AzimuthAngle = TiltToAzimuth(tilt);
                    m_AzimuthNeedsConversion = false;
                }
                return m_AzimuthAngle;
            }
            protected set
            {
                m_AzimuthNeedsConversion = true;
                m_AzimuthAngle  = value;
            }
        }
        /// <summary>
        /// Gets the rotation of the stylus around its axis, in radians.
        /// </summary>
        public float twist { get; protected set; }
        /// <summary>
        /// Specifies the angle of the pen relative to the X and Y axis respectively, in radians.
        /// </summary>
        public Vector2 tilt
        {
            // only calculate tilt when requested and not natively provided
            get
            {
                // windows does not provide altitude or azimuth for touch events, so there's nothing to convert
                if (!(Application.platform == RuntimePlatform.WindowsEditor ||
                      Application.platform == RuntimePlatform.WindowsPlayer) && this.pointerType == PointerType.touch &&  m_TiltNeeded)
                {
                    m_Tilt = AzimuthAndAlitutudeToTilt(m_AltitudeAngle, m_AzimuthAngle);
                    m_TiltNeeded = false;
                }
                return m_Tilt;
            }

            protected set
            {
                m_TiltNeeded = true;
                m_Tilt = value;
            }
        }
        /// <summary>
        /// Specifies the state of the pen. For example, whether the pen is in contact with the screen or tablet, whether the pen is inverted, and whether buttons are pressed.
        /// On macOS, penStatus will not reflect changes to button mappings.
        /// </summary>
        public PenStatus penStatus { get; protected set; }
        /// <summary>
        /// Gets an estimate of the radius of a touch.
        /// </summary>
        /// <remarks>
        /// Add `radiusVariance` to get the maximum touch radius, subtract it to get the minimum touch radius.
        /// </remarks>
        public Vector2 radius { get; protected set; }
        /// <summary>
        /// Gets the accuracy of the touch radius.
        /// </summary>
        /// <remarks>
        /// Add this value to the radius to get the maximum touch radius, subtract it to get the minimum touch radius.
        /// </remarks>
        public Vector2 radiusVariance { get; protected set; }

        /// <summary>
        /// Gets flags that indicate whether modifier keys (Alt, Ctrl, Shift, Windows/Cmd) are pressed.
        /// </summary>
        public EventModifiers modifiers { get; protected set; }

        /// <summary>
        /// Gets a boolean value that indicates whether the Shift key is pressed. True means the Shift key is pressed.
        /// False means it isn't.
        /// </summary>
        public bool shiftKey
        {
            get { return (modifiers & EventModifiers.Shift) != 0; }
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the Ctrl key is pressed. True means the Ctrl key is pressed.
        /// False means it isn't.
        /// </summary>
        public bool ctrlKey
        {
            get { return (modifiers & EventModifiers.Control) != 0; }
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the Windows/Cmd key is pressed. True means the Windows/Cmd key
        /// is pressed. False means it isn't.
        /// </summary>
        public bool commandKey
        {
            get { return (modifiers & EventModifiers.Command) != 0; }
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the Alt key is pressed. True means the Alt key is pressed.
        /// False means it isn't.
        /// </summary>
        public bool altKey
        {
            get { return (modifiers & EventModifiers.Alt) != 0; }
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the platform-specific action key is pressed. True means the action
        /// key is pressed. False means it isn't.
        /// </summary>
        /// <remarks>
        /// The platform-specific action key is Cmd on macOS, and Ctrl on all other platforms.
        /// </remarks>
        public bool actionKey
        {
            get
            {
                if (Application.platform == RuntimePlatform.OSXEditor ||
                    Application.platform == RuntimePlatform.OSXPlayer)
                {
                    return commandKey;
                }
                else
                {
                    return ctrlKey;
                }
            }
        }

        bool IPointerEventInternal.triggeredByOS { get; set; }

        bool IPointerEventInternal.recomputeTopElementUnderPointer { get; set; }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown | EventPropagation.Cancellable;
            propagateToIMGUI = false;

            pointerId = 0;
            pointerType = PointerType.unknown;
            isPrimary = false;
            button = -1;
            pressedButtons = 0;
            position = Vector3.zero;
            localPosition = Vector3.zero;
            deltaPosition = Vector3.zero;
            deltaTime = 0;
            clickCount = 0;
            pressure = 0;
            tangentialPressure = 0;

            altitudeAngle = 0;
            azimuthAngle = 0;
            tilt = new Vector2(0f, 0f);
            twist = 0.0f;
            penStatus = PenStatus.None;
            radius = Vector2.zero;
            radiusVariance = Vector2.zero;

            modifiers = EventModifiers.None;

            ((IPointerEventInternal)this).triggeredByOS = false;
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = false;
        }

        /// <summary>
        /// Gets the current target of the event.
        /// </summary>
        /// <remarks>
        /// The current target is the element in the propagation path for which event handlers are currently being executed.
        /// </remarks>
        public override IEventHandler currentTarget
        {
            get { return base.currentTarget; }
            internal set
            {
                base.currentTarget = value;

                var element = currentTarget as VisualElement;
                if (element != null)
                {
                    localPosition = element.WorldToLocal(position);
                }
                else
                {
                    localPosition = position;
                }
            }
        }

        private static bool IsMouse(Event systemEvent)
        {
            EventType t = systemEvent.rawType;
            return t == EventType.MouseMove
                || t == EventType.MouseDown
                || t == EventType.MouseUp
                || t == EventType.MouseDrag
                || t == EventType.ContextClick
                || t == EventType.MouseEnterWindow
                || t == EventType.MouseLeaveWindow;
        }

        private static bool IsTouch(Event systemEvent)
        {
            EventType t = systemEvent.rawType;
            return t == EventType.TouchMove
                || t == EventType.TouchDown
                || t == EventType.TouchUp
                || t == EventType.TouchStationary
                || t == EventType.TouchEnter
                || t == EventType.TouchLeave;
        }

        /// <summary>
        /// Converts touch or stylus tilt to azimuth angle.
        /// </summary>
        /// <param name="tilt">Angle relative to the X and Y axis, in radians. abs(tilt.y) must be < pi/2</param>
        /// <returns>Azimuth angle as determined by tilt along x and y axese.</returns>
        private static float TiltToAzimuth(Vector2 tilt)
        {
            float azimuth = 0f;
            if (tilt.x != 0)
            {
                azimuth = Mathf.PI / 2 - Mathf.Atan2(-Mathf.Cos(tilt.x) * Mathf.Sin(tilt.y), Mathf.Cos(tilt.y) * Mathf.Sin(tilt.x));
                if (azimuth < 0) // fix range to [0, 2*pi)
                    azimuth += 2 * Mathf.PI;
                // follow UIKit conventions where azimuth is 0 when the cap end of the stylus points along the positive x axis of the device's screen
                if (azimuth >= (Mathf.PI / 2))
                    azimuth -= Mathf.PI / 2;
                else
                    azimuth += (3 * Mathf.PI / 2);
            }

            return azimuth;
        }

        /// <summary>
        /// Converts touch or stylus azimuth and altitude to tilt
        /// </summary>
        /// <param name="tilt">Angle relative to the X and Y axis, in radians. abs(tilt.y) must be < pi/2</param>
        /// <returns>Azimuth angle as determined by tilt along x and y axese.</returns>
        private static Vector2 AzimuthAndAlitutudeToTilt(float altitude, float azimuth)
        {
            Vector2 t = new Vector2(0, 0);

            t.x = Mathf.Atan(Mathf.Cos(azimuth) * Mathf.Cos(altitude) / Mathf.Sin(azimuth));
            t.y = Mathf.Atan(Mathf.Cos(azimuth) * Mathf.Sin(altitude) / Mathf.Sin(azimuth));

            return t;
        }

        /// <summary>
        /// Converts touch or stylus tilt to altitude angle.
        /// </summary>
        /// <param name="tilt">Angle relative to the X and Y axis, in radians. abs(tilt.y) must be < pi/2</param>
        /// <returns>Altitude angle as determined by tilt along x and y axese.</returns>
        private static float TiltToAltitude(Vector2 tilt)
        {
            return Mathf.PI / 2 - Mathf.Acos(Mathf.Cos(tilt.x) * Mathf.Cos(tilt.y));
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events.
        /// Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">An IMGUI mouse event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(Event systemEvent)
        {
            T e = GetPooled();

            if (!(IsMouse(systemEvent) || IsTouch(systemEvent) || systemEvent.rawType == EventType.DragUpdated))
            {
                Debug.Assert(false, "Unexpected event type: " + systemEvent.rawType + " (" + systemEvent.type + ")");
            }

            switch (systemEvent.pointerType)
            {
                default:
                    e.pointerType = PointerType.mouse;
                    e.pointerId = PointerId.mousePointerId;
                    break;
                case UnityEngine.PointerType.Touch:
                    e.pointerType = PointerType.touch;
                    e.pointerId = PointerId.touchPointerIdBase;
                    break;
                case UnityEngine.PointerType.Pen:
                    e.pointerType = PointerType.pen;
                    e.pointerId = PointerId.penPointerIdBase;
                    // system events are not fired for pen buttons, so in order to keep buttonsate up to date we need to check the status of these buttons for all pen events
                    if (systemEvent.penStatus == PenStatus.Barrel)
                        PointerDeviceState.PressButton(e.pointerId, (int) PenButton.PenBarrel);
                    else
                        PointerDeviceState.ReleaseButton(e.pointerId, (int) PenButton.PenBarrel);
                    if (systemEvent.penStatus == PenStatus.Eraser)
                        PointerDeviceState.PressButton(e.pointerId, (int) PenButton.PenEraser);
                    else
                        PointerDeviceState.ReleaseButton(e.pointerId, (int) PenButton.PenEraser);
                    break;
            }

            e.isPrimary = true;

            // calculate these on demand
            e.altitudeAngle = 0f;
            e.azimuthAngle = 0f;

            e.radius = Vector2.zero;
            e.radiusVariance = Vector2.zero;

            e.imguiEvent = systemEvent;

            if (systemEvent.rawType == EventType.MouseDown || systemEvent.rawType == EventType.TouchDown)
            {
                PointerDeviceState.PressButton(e.pointerId, systemEvent.button);
                e.button = systemEvent.button;
            }
            else if (systemEvent.rawType == EventType.MouseUp || systemEvent.rawType == EventType.TouchUp)
            {
                PointerDeviceState.ReleaseButton(e.pointerId, systemEvent.button);
                e.button = systemEvent.button;
            }
            else if (systemEvent.rawType == EventType.MouseMove || systemEvent.rawType == EventType.TouchMove)
            {
                e.button = -1;
            }

            e.pressedButtons = PointerDeviceState.GetPressedButtons(e.pointerId);
            e.position = systemEvent.mousePosition;
            e.localPosition = systemEvent.mousePosition;
            e.deltaPosition = systemEvent.delta;
            e.clickCount = systemEvent.clickCount;
            e.modifiers = systemEvent.modifiers;
            e.tilt = systemEvent.tilt;
            e.penStatus = systemEvent.penStatus;
            e.twist = systemEvent.twist;

            switch (systemEvent.pointerType)
            {
                default:
                    e.pressure = e.pressedButtons == 0 ? 0f : k_DefaultButtonPressure;
                    break;
                case UnityEngine.PointerType.Touch:
                    e.pressure = systemEvent.pressure;
                    break;
                case UnityEngine.PointerType.Pen:
                    e.pressure = systemEvent.pressure;
                    break;
            }

            e.tangentialPressure = 0;

            ((IPointerEventInternal)e).triggeredByOS = true;

            return e;
        }

        internal static T GetPooled(EventType eventType, Vector3 mousePosition, Vector2 delta, int button, int clickCount, EventModifiers modifiers)
        {
            T e = GetPooled();

            e.pointerId = PointerId.mousePointerId;
            e.pointerType = PointerType.mouse;
            e.isPrimary = true;

            if (eventType == EventType.MouseDown)
            {
                PointerDeviceState.PressButton(e.pointerId, button);
                e.button = button;
            }
            else if (eventType == EventType.MouseUp)
            {
                PointerDeviceState.ReleaseButton(e.pointerId, button);
                e.button = button;
            }
            else
            {
                e.button = -1;
            }

            e.pressedButtons = PointerDeviceState.GetPressedButtons(e.pointerId);
            e.position = mousePosition;
            e.localPosition = mousePosition;
            e.deltaPosition = delta;
            e.clickCount = clickCount;
            e.modifiers = modifiers;
            e.pressure = e.pressedButtons == 0 ? 0f : k_DefaultButtonPressure;
            ((IPointerEventInternal)e).triggeredByOS = true;

            return e;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events.
        /// Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="touch">A <see cref="Touch"/> structure from the InputManager.</param>
        /// <param name="modifiers">The modifier keys held down during the event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(Touch touch, EventModifiers modifiers = EventModifiers.None)
        {
            T e = GetPooled();

            e.pointerId = touch.fingerId + PointerId.touchPointerIdBase;
            e.pointerType = PointerType.touch;

            // TODO: Rethink this logic. When two fingers are down, PointerMoveEvents should still have 1 primary touch.
            bool otherTouchDown = false;
            for (var i = PointerId.touchPointerIdBase;
                 i < PointerId.touchPointerIdBase + PointerId.touchPointerCount;
                 i++)
            {
                if (i != e.pointerId && PointerDeviceState.GetPressedButtons(i) != 0)
                {
                    otherTouchDown = true;
                    break;
                }
            }
            e.isPrimary = !otherTouchDown;

            if (touch.phase == TouchPhase.Began)
            {
                PointerDeviceState.PressButton(e.pointerId, 0);
                e.button = 0;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                PointerDeviceState.ReleaseButton(e.pointerId, 0);
                e.button = 0;
            }
            else
            {
                e.button = -1;
            }

            e.pressedButtons = PointerDeviceState.GetPressedButtons(e.pointerId);
            e.position = touch.position;
            e.localPosition = touch.position;
            e.deltaPosition = touch.deltaPosition;
            e.deltaTime = touch.deltaTime;
            e.clickCount = touch.tapCount;
            e.pressure = Mathf.Abs(touch.maximumPossiblePressure) > UIRUtility.k_Epsilon ? touch.pressure / touch.maximumPossiblePressure : 1f;
            e.tangentialPressure = 0;

            e.altitudeAngle = touch.altitudeAngle;
            e.azimuthAngle = touch.azimuthAngle;
            e.twist = 0.0f;
            e.tilt = new Vector2(0f, 0f);
            e.penStatus = PenStatus.None;
            e.radius = new Vector2(touch.radius, touch.radius);
            e.radiusVariance = new Vector2(touch.radiusVariance, touch.radiusVariance);

            e.modifiers = modifiers;

            ((IPointerEventInternal)e).triggeredByOS = true;

            return e;
        }

        /// <summary>
        /// Gets a pointer event from the event pool and initializes it with the given values. Use this function instead of creating new events.
        /// Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="pen">A <see cref="PenData"/> structure from the InputManager containing pen event information.</param>
        /// <param name="modifiers">The modifier keys held down during the event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(PenData pen, EventModifiers modifiers = EventModifiers.None)
        {
            T e = GetPooled();

            e.pointerId = PointerId.penPointerIdBase;
            e.pointerType = PointerType.pen;

            e.isPrimary = true;

            if (pen.contactType == PenEventType.PenDown)
            {
                PointerDeviceState.PressButton(e.pointerId, 0);
                e.button = 0;
            }
            else if (pen.contactType == PenEventType.PenUp)
            {
                PointerDeviceState.ReleaseButton(e.pointerId, 0);
                e.button = 0;
            }
            else
            {
                e.button = -1;
            }

            // system events are not fired for pen buttons, so in order to keep buttonsate up to date we need to check the status of these buttons for all pen events
            if (pen.penStatus == PenStatus.Barrel)
                PointerDeviceState.PressButton(e.pointerId, (int) PenButton.PenBarrel);
            else
                PointerDeviceState.ReleaseButton(e.pointerId, (int) PenButton.PenBarrel);
            if (pen.penStatus == PenStatus.Eraser)
                PointerDeviceState.PressButton(e.pointerId, (int) PenButton.PenEraser);
            else
                PointerDeviceState.ReleaseButton(e.pointerId, (int) PenButton.PenEraser);

            e.pressedButtons = PointerDeviceState.GetPressedButtons(e.pointerId);
            e.position = pen.position;
            e.localPosition = pen.position;
            e.deltaPosition = pen.deltaPos;
            e.clickCount = 0;
            e.pressure = pen.pressure;
            e.tangentialPressure = 0;

            e.twist = pen.twist;
            e.tilt = pen.tilt;
            e.penStatus = pen.penStatus;
            e.radius = Vector2.zero;
            e.radiusVariance = Vector2.zero;

            e.modifiers = modifiers;

            ((IPointerEventInternal)e).triggeredByOS = true;

            return e;
        }

        internal static T GetPooled(IPointerEvent triggerEvent, Vector2 position, int pointerId)
        {
            if (triggerEvent != null)
            {
                return GetPooled(triggerEvent);
            }

            T e = GetPooled();
            e.position = position;
            e.localPosition = position;
            e.pointerId = pointerId;
            e.pointerType = PointerType.GetPointerType(pointerId);
            return e;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events.
        /// Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="triggerEvent">The event that sent this event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(IPointerEvent triggerEvent)
        {
            T e = GetPooled();
            if (triggerEvent != null)
            {
                e.pointerId = triggerEvent.pointerId;
                e.pointerType = triggerEvent.pointerType;
                e.isPrimary = triggerEvent.isPrimary;
                e.button = triggerEvent.button;
                e.pressedButtons = triggerEvent.pressedButtons;
                e.position = triggerEvent.position;
                e.localPosition = triggerEvent.localPosition;
                e.deltaPosition = triggerEvent.deltaPosition;
                e.deltaTime = triggerEvent.deltaTime;
                e.clickCount = triggerEvent.clickCount;
                e.pressure = triggerEvent.pressure;
                e.tangentialPressure = triggerEvent.tangentialPressure;

                e.altitudeAngle = triggerEvent.altitudeAngle;
                e.azimuthAngle = triggerEvent.azimuthAngle;
                e.twist = triggerEvent.twist;
                e.tilt = triggerEvent.tilt;
                e.penStatus = triggerEvent.penStatus;
                e.radius = triggerEvent.radius;
                e.radiusVariance = triggerEvent.radiusVariance;

                e.modifiers = triggerEvent.modifiers;

                IPointerEventInternal pointerEventInternal = triggerEvent as IPointerEventInternal;
                if (pointerEventInternal != null)
                {
                    ((IPointerEventInternal)e).triggeredByOS |= pointerEventInternal.triggeredByOS;
                }
            }
            return e;
        }

        protected internal override void PreDispatch(IPanel panel)
        {
            base.PreDispatch(panel);

            if (((IPointerEventInternal)this).triggeredByOS)
            {
                PointerDeviceState.SavePointerPosition(pointerId, position, panel, panel.contextType);
            }
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            for (var i = 0; i < PointerId.maxPointers; i++)
            {
                panel.ProcessPointerCapture(i);
            }

            // If ShouldSendCompatibilityMouseEvents == true, mouse event will take care of this.
            if (!panel.ShouldSendCompatibilityMouseEvents(this) && ((IPointerEventInternal)this).triggeredByOS)
            {
                (panel as BaseVisualElementPanel)?.CommitElementUnderPointers();
            }

            base.PostDispatch(panel);
        }

        protected PointerEventBase()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// This event is sent when a pointer is pressed.
    /// </summary>
    /// <remarks>
    /// A PointerDownEvent is sent the first time a finger touches the screen or a mouse button is
    /// pressed. Additional button presses and touches with additional fingers trigger PointerMoveEvents.
    ///
    /// A PointerDownEvent uses the default pointer event propagation path: it trickles down, bubbles up and
    /// can be cancelled.
    /// Disabled elements won't receive this event by default.
    ///
    /// See <see cref="UIElements.PointerEventBase{T}"/> to see how PointerDownEvent relates to other pointer events.
    /// </remarks>
    public sealed class PointerDownEvent : PointerEventBase<PointerDownEvent>
    {
        static PointerDownEvent()
        {
            SetCreateFunction(() => new PointerDownEvent());
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown | EventPropagation.Cancellable |
                EventPropagation.SkipDisabledElements;
            ((IPointerEventInternal)this).triggeredByOS = true;
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerDownEvent()
        {
            LocalInit();
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            if (!isDefaultPrevented)
            {
                if (panel.ShouldSendCompatibilityMouseEvents(this))
                {
                    using (var evt = MouseDownEvent.GetPooled(this))
                    {
                        evt.target = target;
                        evt.target.SendEvent(evt);
                    }
                }
            }
            else
            {
                panel.PreventCompatibilityMouseEvents(pointerId);
            }

            base.PostDispatch(panel);
        }
    }

    /// <summary>
    /// This event is sent when a pointer changes state.
    /// </summary>
    /// <remarks>
    /// The state of a pointer changes when one or more of its properties changes after a <see cref="PointerDownEvent"/> but before a
    /// <see cref="PointerUpEvent"/>. For example if its position or pressure change, or a different button is pressed.
    ///
    /// A PointerMoveEvent uses the default pointer event propagation path: it trickles down, bubbles up and
    /// can be cancelled.
    /// Disabled elements receive this event by default.
    ///
    /// See <see cref="UIElements.PointerEventBase{T}"/> to see how PointerMoveEvent relates to other pointer events.
    /// </remarks>
    [EventCategory(EventCategory.PointerMove)]
    public sealed class PointerMoveEvent : PointerEventBase<PointerMoveEvent>
    {
        static PointerMoveEvent()
        {
            SetCreateFunction(() => new PointerMoveEvent());
        }

        /// <summary>
        /// Set this variable if the target should have priority when used in a touch enabled scroll view.
        /// </summary>
        internal bool isHandledByDraggable { get; set; }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown | EventPropagation.Cancellable;
            ((IPointerEventInternal)this).triggeredByOS = true;
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
            isHandledByDraggable = false;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerMoveEvent()
        {
            LocalInit();
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            if (panel.ShouldSendCompatibilityMouseEvents(this))
            {
                if (imguiEvent != null && imguiEvent.rawType == EventType.MouseDown)
                {
                    using (var evt = MouseDownEvent.GetPooled(this))
                    {
                        evt.target = target;
                        evt.target.SendEvent(evt);
                    }
                }
                else if (imguiEvent != null && imguiEvent.rawType == EventType.MouseUp)
                {
                    using (var evt = MouseUpEvent.GetPooled(this))
                    {
                        evt.target = target;
                        evt.target.SendEvent(evt);
                    }
                }
                else
                {
                    using (var evt = MouseMoveEvent.GetPooled(this))
                    {
                        evt.target = target;
                        evt.target.SendEvent(evt);
                    }
                }
            }

            base.PostDispatch(panel);
        }
    }

    /// <summary>
    /// This event is sent when a pointer does not change for a set amount of time, determined by the operating system.
    /// </summary>
    /// <remarks>
    /// After a <see cref="PointerDownEvent"/> is sent, this event is sent if a <see cref="PointerMoveEvent"/> or
    /// a <see cref="PointerUpEvent"/> is not sent before a set amount of time.
    ///
    /// A PointerStationaryEvent uses the default pointer event propagation path: it trickles down, bubbles up
    /// and can be cancelled.
    /// Disabled elements receive this event by default.
    ///
    /// See <see cref="UIElements.PointerEventBase{T}"/> to see how PointerStationaryEvent relates to other pointer events.
    /// </remarks>
    public sealed class PointerStationaryEvent : PointerEventBase<PointerStationaryEvent>
    {
        static PointerStationaryEvent()
        {
            SetCreateFunction(() => new PointerStationaryEvent());
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown | EventPropagation.Cancellable;
            ((IPointerEventInternal)this).triggeredByOS = true;
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerStationaryEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// This event is sent when a pointer's last pressed button is released.
    /// </summary>
    /// <remarks>
    /// The last pressed button may or may not be the same button that triggered the <see cref="PointerDownEvent"/>.
    ///
    /// A PointerUpEvent uses the default pointer event propagation path: it is trickled down, bubbled up
    /// and can be cancelled.
    /// Disabled elements won't receive this event by default.
    ///
    /// See <see cref="UIElements.PointerEventBase{T}"/> to see how PointerUpEvent relates to other pointer events.
    /// </remarks>
    public sealed class PointerUpEvent : PointerEventBase<PointerUpEvent>
    {
        static PointerUpEvent()
        {
            SetCreateFunction(() => new PointerUpEvent());
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown | EventPropagation.Cancellable |
                EventPropagation.SkipDisabledElements;
            ((IPointerEventInternal)this).triggeredByOS = true;
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerUpEvent()
        {
            LocalInit();
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            if (PointerType.IsDirectManipulationDevice(pointerType))
            {
                panel.ReleasePointer(pointerId);
                BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;
                basePanel?.ClearCachedElementUnderPointer(pointerId, this);
            }

            if (panel.ShouldSendCompatibilityMouseEvents(this))
            {
                using (var evt = MouseUpEvent.GetPooled(this))
                {
                    evt.target = target;
                    evt.target.SendEvent(evt);
                }
            }

            base.PostDispatch(panel);

            panel.ActivateCompatibilityMouseEvents(pointerId);
        }
    }

    /// <summary>
    /// This event is sent when pointer interaction is cancelled.
    /// </summary>
    /// <remarks>
    /// A PointerCancelEvent can trickle down or bubble up, but cannot be cancelled.
    /// Disabled elements won't receive this event by default.
    ///
    /// See <see cref="UIElements.PointerEventBase{T}"/> to see how PointerCancelEvent relates to other pointer events.
    /// </remarks>
    public sealed class PointerCancelEvent : PointerEventBase<PointerCancelEvent>
    {
        static PointerCancelEvent()
        {
            SetCreateFunction(() => new PointerCancelEvent());
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown |
                EventPropagation.SkipDisabledElements;
            ((IPointerEventInternal)this).triggeredByOS = true;
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerCancelEvent()
        {
            LocalInit();
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            if (PointerType.IsDirectManipulationDevice(pointerType))
            {
                panel.ReleasePointer(pointerId);
                BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;
                basePanel?.ClearCachedElementUnderPointer(pointerId, this);
            }

            if (panel.ShouldSendCompatibilityMouseEvents(this))
            {
                using (var evt = MouseUpEvent.GetPooled(this))
                {
                    evt.target = target;
                    target.SendEvent(evt);
                }
            }

            base.PostDispatch(panel);

            panel.ActivateCompatibilityMouseEvents(pointerId);
        }
    }

    /// <summary>
    /// This event is sent when the left mouse button is clicked.
    /// </summary>
    /// <remarks>
    /// A click consists of a mouse down event followed by a mouse up event on the same VisualElement.
    /// The mouse might move between the two events but the move is ignored as long as the mouse down
    /// and mouse up events occur on the same VisualElement.
    ///
    /// A ClickEvent uses the default pointer event propagation path: it trickles down, bubbles up
    /// and can be cancelled.
    /// Disabled elements won't receive this event by default.
    ///
    /// See <see cref="UIElements.PointerEventBase{T}"/> to see how ClickEvent relates to other pointer events.
    ///
    /// </remarks>
    public sealed class ClickEvent : PointerEventBase<ClickEvent>
    {
        static ClickEvent()
        {
            SetCreateFunction(() => new ClickEvent());
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown | EventPropagation.Cancellable |
                EventPropagation.SkipDisabledElements;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public ClickEvent()
        {
            LocalInit();
        }

        internal static ClickEvent GetPooled(PointerUpEvent pointerEvent, int clickCount)
        {
            var evt = PointerEventBase<ClickEvent>.GetPooled((IPointerEvent)pointerEvent);
            evt.clickCount = clickCount;
            return evt;
        }
    }

    /// <summary>
    /// This event is sent when a pointer enters a VisualElement or one of its descendants.
    /// </summary>
    [EventCategory(EventCategory.EnterLeave)]
    public sealed class PointerEnterEvent : PointerEventBase<PointerEnterEvent>
    {
        static PointerEnterEvent()
        {
            SetCreateFunction(() => new PointerEnterEvent());
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown | EventPropagation.IgnoreCompositeRoots;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerEnterEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// This event is sent when a pointer exits an element and all of its descendants.
    /// </summary>
    [EventCategory(EventCategory.EnterLeave)]
    public sealed class PointerLeaveEvent : PointerEventBase<PointerLeaveEvent>
    {
        static PointerLeaveEvent()
        {
            SetCreateFunction(() => new PointerLeaveEvent());
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown | EventPropagation.IgnoreCompositeRoots;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerLeaveEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// This event is sent when a pointer enters an element.
    /// </summary>
    [EventCategory(EventCategory.EnterLeave)]
    public sealed class PointerOverEvent : PointerEventBase<PointerOverEvent>
    {
        static PointerOverEvent()
        {
            SetCreateFunction(() => new PointerOverEvent());
        }
    }

    /// <summary>
    /// This event is sent when a pointer exits an element.
    /// </summary>
    [EventCategory(EventCategory.EnterLeave)]
    public sealed class PointerOutEvent : PointerEventBase<PointerOutEvent>
    {
        static PointerOutEvent()
        {
            SetCreateFunction(() => new PointerOutEvent());
        }


    }
}
