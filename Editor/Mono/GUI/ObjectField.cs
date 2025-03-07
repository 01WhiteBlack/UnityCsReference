// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public sealed partial class EditorGUI
    {
        static private GUIContent s_SceneMismatch = EditorGUIUtility.TrTextContent("Scene mismatch (cross scene references not supported)");
        static private GUIContent s_TypeMismatch = EditorGUIUtility.TrTextContent("Type mismatch");
        static private GUIContent s_Select = EditorGUIUtility.TrTextContent("Select");

        [Flags]
        internal enum ObjectFieldValidatorOptions
        {
            None = 0,
            ExactObjectTypeValidation = (1 << 0)
        }

        internal delegate Object ObjectFieldValidator(Object[] references, System.Type objType, SerializedProperty property, ObjectFieldValidatorOptions options);

        // Takes object directly, no SerializedProperty.
        internal static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, Object objBeingEdited, System.Type objType, ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style = null)
        {
            return DoObjectField(position, dropRect, id, obj, objBeingEdited, objType, null, validator, allowSceneObjects, style != null ? style : EditorStyles.objectField);
        }

        internal static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, Object objBeingEdited, System.Type objType, ObjectFieldValidator validator, bool allowSceneObjects, System.Type additionalType, GUIStyle style = null)
        {
            return DoObjectField(position, dropRect, id, obj, objBeingEdited, objType, additionalType, null, validator, allowSceneObjects, style != null ? style : EditorStyles.objectField);
        }

        // Takes SerializedProperty, no direct reference to object.
        internal static Object DoObjectField(Rect position, Rect dropRect, int id, System.Type objType, SerializedProperty property, ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style = null)
        {
            return DoObjectField(position, dropRect, id, null, null, objType, property, validator, allowSceneObjects, style != null ? style : EditorStyles.objectField);
        }

        internal enum ObjectFieldVisualType { IconAndText, LargePreview, MiniPreview }

        // when current event is mouse click, this function pings the object, or
        // if shift/control is pressed and object is a texture, pops up a large texture
        // preview window
        internal static void PingObjectOrShowPreviewOnClick(Object targetObject, Rect position)
        {
            if (targetObject == null)
                return;

            Event evt = Event.current;
            // ping object
            bool anyModifiersPressed = evt.shift || evt.control;
            if (!anyModifiersPressed)
            {
                EditorGUIUtility.PingObject(targetObject);
                return;
            }

            // show large object preview popup; right now only for textures
            if (targetObject is Texture)
            {
                PopupWindowWithoutFocus.Show(
                    new RectOffset(6, 3, 0, 3).Add(position),
                    new ObjectPreviewPopup(targetObject),
                    new[] { PopupLocation.Left, PopupLocation.Below, PopupLocation.Right });
            }
        }

        internal static void PingObjectInSceneViewOnClick(Material targetMaterial)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || targetMaterial == null)
                return;

            sceneView.isPingingObject = true;
            sceneView.pingStartTime = Time.realtimeSinceStartup;
            sceneView.submeshOutlineMaterialId = targetMaterial.GetInstanceID();
        }

        static Object AssignSelectedObject(SerializedProperty property, ObjectFieldValidator validator, System.Type objectType, Event evt)
        {
            Object[] references = { ObjectSelector.GetCurrentObject() };
            Object assigned = validator(references, objectType, property, ObjectFieldValidatorOptions.None);

            // Assign the value
            if (property != null)
                property.objectReferenceValue = assigned;

            GUI.changed = true;
            evt.Use();
            return assigned;
        }

        static private Rect GetButtonRect(ObjectFieldVisualType visualType, Rect position)
        {
            switch (visualType)
            {
                case ObjectFieldVisualType.IconAndText:
                    return new Rect(position.xMax - 19, position.y, 19, position.height);
                case ObjectFieldVisualType.MiniPreview:
                    return new Rect(position.xMax - 14, position.y, 14, position.height);
                case ObjectFieldVisualType.LargePreview:
                    return new Rect(position.xMax - 36, position.yMax - 14, 36, 14);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static bool HasValidScript(UnityEngine.Object obj)
        {
            MonoScript script = MonoScript.FromScriptedObject(obj);
            if (script == null)
            {
                return false;
            }
            Type type = script.GetClass();
            if (type == null)
            {
                return false;
            }
            return true;
        }

        static bool ValidDroppedObject(Object[] references, System.Type objType, out string errorString)
        {
            errorString = "";
            if (references == null || references.Length == 0)
            {
                return true;
            }

            var reference = references[0];
            Object obj = EditorUtility.InstanceIDToObject(reference.GetInstanceID());
            if (obj is MonoBehaviour || obj is ScriptableObject)
            {
                if (!HasValidScript(obj))
                {
                    errorString = $"Type cannot be found: {reference.GetType()}. Containing file and class name must match.";
                    return false;
                }
            }
            return true;
        }

        // Timeline package is using this internal overload so can't remove until that's fixed.
        internal static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, System.Type objType, SerializedProperty property, ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style)
        {
            return DoObjectField(position, dropRect, id, objType, property, validator, allowSceneObjects);
        }

        // This method takes either object reference directly, or via SerializedObject.
        // Since it's not easy to know which parameters are mutually exclusively used, this method is
        // private and internal/public methods instead EITHER take SerializedObject OR direct reference.
        static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, Object objBeingEdited, System.Type objType, SerializedProperty property, ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style)
        {
            return DoObjectField(position, dropRect, id, obj, objBeingEdited, objType, null, property, validator, allowSceneObjects, style, EditorStyles.objectFieldButton);
        }

        static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, Object objBeingEdited, System.Type objType, System.Type additionalType, SerializedProperty property, ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style, Action<Object> onObjectSelectorClosed, Action<Object> onObjectSelectedUpdated = null)
        {
            return DoObjectField(position, dropRect, id, obj, objBeingEdited, objType, additionalType, property, validator, allowSceneObjects, style, EditorStyles.objectFieldButton, onObjectSelectorClosed, onObjectSelectedUpdated);
        }

        static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, Object objBeingEdited, System.Type objType, System.Type additionalType, SerializedProperty property, ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style)
        {
            return DoObjectField(position, dropRect, id, obj, objBeingEdited, objType, additionalType, property, validator, allowSceneObjects, style, EditorStyles.objectFieldButton);
        }

        static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, Object objBeingEdited, System.Type objType, System.Type additionalType, SerializedProperty property, ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style, GUIStyle buttonStyle, Action<Object> onObjectSelectorClosed = null, Action<Object> onObjectSelectedUpdated = null)
        {
            if (validator == null)
                validator = ValidateObjectFieldAssignment;
            if (property != null)
                obj = property.objectReferenceValue;
            Event evt = Event.current;
            EventType eventType = evt.type;

            // special case test, so we continue to ping/select objects with the object field disabled
            if (!GUI.enabled && GUIClip.enabled && (Event.current.rawType == EventType.MouseDown))
                eventType = Event.current.rawType;

            bool hasThumbnail = EditorGUIUtility.HasObjectThumbnail(objType);

            // Determine visual type
            ObjectFieldVisualType visualType = ObjectFieldVisualType.IconAndText;
            if (hasThumbnail && position.height <= kObjectFieldMiniThumbnailHeight && position.width <= kObjectFieldMiniThumbnailWidth)
                visualType = ObjectFieldVisualType.MiniPreview;
            else if (hasThumbnail && position.height > kSingleLineHeight)
                visualType = ObjectFieldVisualType.LargePreview;

            Vector2 oldIconSize = EditorGUIUtility.GetIconSize();
            if (visualType == ObjectFieldVisualType.IconAndText)
                EditorGUIUtility.SetIconSize(new Vector2(12, 12));  // Have to be this small to fit inside a single line height ObjectField
            else if (visualType == ObjectFieldVisualType.LargePreview)
                EditorGUIUtility.SetIconSize(new Vector2(64, 64));

            if ((eventType == EventType.MouseDown && Event.current.button == 1 ||
                (eventType == EventType.ContextClick && visualType == ObjectFieldVisualType.IconAndText)) &&
                position.Contains(Event.current.mousePosition))
            {
                var actualObject = property != null ? property.objectReferenceValue : obj;
                var contextMenu = new GenericMenu();

                if (FillPropertyContextMenu(property, null, contextMenu) != null)
                    contextMenu.AddSeparator("");
                contextMenu.AddItem(GUIContent.Temp("Properties..."), false, () => PropertyEditor.OpenPropertyEditor(actualObject));
                contextMenu.DropDown(position);
                Event.current.Use();
            }

            switch (eventType)
            {
                case EventType.DragExited:
                    if (GUI.enabled)
                        HandleUtility.Repaint();

                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    if (eventType == EventType.DragPerform)
                    {
                        string errorString;
                        if (!ValidDroppedObject(DragAndDrop.objectReferences, objType, out errorString))
                        {
                            Object reference = DragAndDrop.objectReferences[0];
                            EditorUtility.DisplayDialog("Can't assign script", errorString, "OK");
                            break;
                        }
                    }

                    if (dropRect.Contains(Event.current.mousePosition) && GUI.enabled)
                    {
                        Object[] references = DragAndDrop.objectReferences;
                        Object validatedObject = validator(references, objType, property, ObjectFieldValidatorOptions.None);

                        if (validatedObject != null)
                        {
                            // If scene objects are not allowed and object is a scene object then clear
                            if (!allowSceneObjects && !EditorUtility.IsPersistent(validatedObject))
                                validatedObject = null;
                        }

                        if (validatedObject != null)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                            if (eventType == EventType.DragPerform)
                            {
                                if (property != null)
                                    property.objectReferenceValue = validatedObject;
                                else
                                    obj = validatedObject;

                                GUI.changed = true;
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.activeControlID = 0;
                            }
                            else
                            {
                                DragAndDrop.activeControlID = id;
                            }
                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.MouseDown:
                    if (position.Contains(Event.current.mousePosition) && Event.current.button == 0)
                    {
                        // Get button rect for Object Selector
                        Rect buttonRect = GetButtonRect(visualType, position);

                        EditorGUIUtility.editingTextField = false;

                        if (buttonRect.Contains(Event.current.mousePosition))
                        {
                            if (GUI.enabled)
                            {
                                GUIUtility.keyboardControl = id;
                                var types = additionalType == null ? new Type[] {objType} : new Type[] { objType, additionalType };
                                if (property != null)
                                    ObjectSelector.get.Show(types, property, allowSceneObjects, onObjectSelectorClosed: onObjectSelectorClosed, onObjectSelectedUpdated: onObjectSelectedUpdated);
                                else
                                    ObjectSelector.get.Show(obj, types, objBeingEdited, allowSceneObjects, onObjectSelectorClosed: onObjectSelectorClosed, onObjectSelectedUpdated: onObjectSelectedUpdated);
                                ObjectSelector.get.objectSelectorID = id;

                                evt.Use();
                                GUIUtility.ExitGUI();
                            }
                        }
                        else
                        {
                            Object actualTargetObject = property != null ? property.objectReferenceValue : obj;
                            Component com = actualTargetObject as Component;
                            if (com)
                                actualTargetObject = com.gameObject;
                            if (showMixedValue)
                                actualTargetObject = null;

                            // One click shows where the referenced object is, or pops up a preview
                            if (Event.current.clickCount == 1)
                            {
                                GUIUtility.keyboardControl = id;

                                PingObjectOrShowPreviewOnClick(actualTargetObject, position);
                                var selectedMaterial = actualTargetObject as Material;
                                if (selectedMaterial != null)
                                    PingObjectInSceneViewOnClick(selectedMaterial);
                                evt.Use();
                            }
                            // Double click opens the asset in external app or changes selection to referenced object
                            else if (Event.current.clickCount == 2)
                            {
                                if (actualTargetObject)
                                {
                                    AssetDatabase.OpenAsset(actualTargetObject);
                                    evt.Use();
                                    GUIUtility.ExitGUI();
                                }
                            }
                        }
                    }
                    break;
                case EventType.ExecuteCommand:
                    string commandName = evt.commandName;
                    if (commandName == ObjectSelector.ObjectSelectorUpdatedCommand && ObjectSelector.get.objectSelectorID == id && (property == null || !property.isScript))
                        return AssignSelectedObject(property, validator, objType, evt);
                    else if (commandName == ObjectSelector.ObjectSelectorClosedCommand && ObjectSelector.get.objectSelectorID == id && property != null && property.isScript)
                    {
                        if (ObjectSelector.get.GetInstanceID() == 0)
                        {
                            // User canceled object selection; don't apply
                            evt.Use();
                            break;
                        }
                        return AssignSelectedObject(property, validator, objType, evt);
                    }
                    else if ((evt.commandName == EventCommandNames.Delete || evt.commandName == EventCommandNames.SoftDelete) && GUIUtility.keyboardControl == id)
                    {
                        if (property != null)
                            property.objectReferenceValue = null;
                        else
                            obj = null;

                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.ValidateCommand:
                    if ((evt.commandName == EventCommandNames.Delete || evt.commandName == EventCommandNames.SoftDelete) &&  GUIUtility.keyboardControl == id)
                    {
                        evt.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id)
                    {
                        if (evt.keyCode == KeyCode.Backspace || (evt.keyCode == KeyCode.Delete && (evt.modifiers & EventModifiers.Shift) == 0))
                        {
                            if (property != null)
                            {
                                if (property.propertyPath.EndsWith("]"))
                                {
                                    var parentArrayPropertyPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf(".Array.data[", StringComparison.Ordinal));
                                    var parentArrayProperty = property.serializedObject.FindProperty(parentArrayPropertyPath);
                                    bool isReorderableList = PropertyHandler.s_reorderableLists.ContainsKey(ReorderableListWrapper.GetPropertyIdentifier(parentArrayProperty));

                                    // If it's an element of an non-orderable array and it is displayed inside a list, remove that element from the array (cases 1379541 & 1335322)
                                    if (!isReorderableList && GUI.isInsideList && GetInsideListDepth() == parentArrayProperty.depth)
                                        TargetChoiceHandler.DeleteArrayElement(property);
                                    else
                                        property.objectReferenceValue = null;
                                }
                                else
                                {
                                    property.objectReferenceValue = null;
                                }
                            }
                            else
                                obj = null;

                            GUI.changed = true;
                            evt.Use();
                        }

                        // Apparently we have to check for the character being space instead of the keyCode,
                        // otherwise the Inspector will maximize upon pressing space.
                        if (evt.MainActionKeyForControl(id))
                        {
                            var types = additionalType == null ? new Type[] {objType} : new Type[] { objType, additionalType };
                            if (property != null)
                                ObjectSelector.get.Show(types, property, allowSceneObjects);
                            else
                                ObjectSelector.get.Show(obj, types, objBeingEdited, allowSceneObjects);
                            ObjectSelector.get.objectSelectorID = id;
                            evt.Use();
                            GUIUtility.ExitGUI();
                        }
                    }
                    break;
                case EventType.Repaint:
                    GUIContent temp;
                    if (showMixedValue)
                    {
                        temp = s_MixedValueContent;
                    }
                    else
                    {
                        temp = EditorGUIUtility.ObjectContent(obj, objType, property, validator);
                    }

                    switch (visualType)
                    {
                        case ObjectFieldVisualType.IconAndText:
                            BeginHandleMixedValueContentColor();
                            style.Draw(position, temp, id, DragAndDrop.activeControlID == id, position.Contains(Event.current.mousePosition));

                            Rect buttonRect = buttonStyle.margin.Remove(GetButtonRect(visualType, position));
                            buttonStyle.Draw(buttonRect, GUIContent.none, id, DragAndDrop.activeControlID == id, buttonRect.Contains(Event.current.mousePosition));
                            EndHandleMixedValueContentColor();
                            break;
                        case ObjectFieldVisualType.LargePreview:
                            DrawObjectFieldLargeThumb(position, id, obj, temp);
                            break;
                        case ObjectFieldVisualType.MiniPreview:
                            DrawObjectFieldMiniThumb(position, id, obj, temp);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
            }

            EditorGUIUtility.SetIconSize(oldIconSize);

            return obj;
        }

        private static void DrawObjectFieldLargeThumb(Rect position, int id, Object obj, GUIContent content)
        {
            GUIStyle thumbStyle = EditorStyles.objectFieldThumb;
            thumbStyle.Draw(position, GUIContent.none, id, DragAndDrop.activeControlID == id, position.Contains(Event.current.mousePosition));

            if (obj != null && !showMixedValue)
            {
                Matrix4x4 guiMatrix = GUI.matrix; // Initial matrix is saved in order to be able to reset it to default
                bool isSprite = obj is Sprite;
                bool alphaIsTransparencyTex2D = (obj is Texture2D && (obj as Texture2D).alphaIsTransparency);
                Rect thumbRect = thumbStyle.padding.Remove(position);

                Texture2D t2d = AssetPreview.GetAssetPreview(obj);
                if (t2d != null)
                {
                    // A checkerboard background is drawn behind transparent textures (for visibility)
                    if (isSprite || t2d.alphaIsTransparency || alphaIsTransparencyTex2D)
                        GUI.DrawTexture(thumbRect, EditorGUI.transparentCheckerTexture, ScaleMode.StretchToFill, false);

                    // Draw asset preview (scaled to fit inside the frame)
                    // GUIStyle.none.Draw is used to allow the asset preview to be caught by AutomatedWindow
                    Vector2 defaultSize = Vector2.one * EditorGUI.kObjectFieldThumbnailHeight;
                    GUIUtility.ScaleAroundPivot(thumbRect.size / defaultSize, thumbRect.position);
                    thumbRect.size = defaultSize; // GUIStyle.none.Draw does not scale, the matrix does. Omitting this reports an incorrect Rect size.
                    GUIStyle.none.Draw(thumbRect, t2d, false, false, false, false);
                    GUI.matrix = guiMatrix;
                }
                else
                {
                    // Preview not loaded -> Draw icon
                    if (isSprite || alphaIsTransparencyTex2D)
                    {
                        // A checkerboard background is drawn behind transparent textures (for visibility)
                        GUI.DrawTexture(thumbRect, EditorGUI.transparentCheckerTexture, ScaleMode.StretchToFill, false);
                        GUI.DrawTexture(thumbRect, content.image, ScaleMode.StretchToFill, true);
                    }
                    else
                        DrawPreviewTexture(thumbRect, content.image);

                    // Keep repainting until the object field has a proper preview
                    HandleUtility.Repaint();
                }
            }
            else
            {
                GUIStyle s2 = thumbStyle.name + "Overlay";
                BeginHandleMixedValueContentColor();

                s2.Draw(position, content, id);
                EndHandleMixedValueContentColor();
            }
            GUIStyle s3 = thumbStyle.name + "Overlay2";
            s3.Draw(position, s_Select, id);
        }

        private static void DrawObjectFieldMiniThumb(Rect position, int id, Object obj, GUIContent content)
        {
            GUIStyle thumbStyle = EditorStyles.objectFieldMiniThumb;
            position.width = EditorGUI.kObjectFieldMiniThumbnailWidth;
            BeginHandleMixedValueContentColor();
            bool hover = obj != null; // we use hover texture for enhancing the border if we have a reference
            bool on =  DragAndDrop.activeControlID == id;
            bool keyFocus = GUIUtility.keyboardControl == id;
            thumbStyle.Draw(position, hover, false, on, keyFocus);
            EndHandleMixedValueContentColor();

            if (obj != null && !showMixedValue)
            {
                Rect thumbRect = new Rect(position.x + 1, position.y + 1, position.height - 2, position.height - 2); // subtract 1 px border
                Texture2D t2d = content.image as Texture2D;
                if (t2d != null && t2d.alphaIsTransparency)
                    DrawTextureTransparent(thumbRect, t2d);
                else
                    DrawPreviewTexture(thumbRect, content.image);

                // Tooltip
                if (thumbRect.Contains(Event.current.mousePosition))
                    GUI.Label(thumbRect, GUIContent.Temp(string.Empty, "Ctrl + Click to show preview"));
            }
        }

        internal static Object DoDropField(Rect position, int id, System.Type objType, ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style)
        {
            if (validator == null)
                validator = ValidateObjectFieldAssignment;
            Event evt = Event.current;
            EventType eventType = evt.type;

            // special case test, so we continue to ping/select objects with the object field disabled
            if (!GUI.enabled && GUIClip.enabled && (Event.current.rawType == EventType.MouseDown))
                eventType = Event.current.rawType;

            switch (eventType)
            {
                case EventType.DragExited:
                    if (GUI.enabled)
                        HandleUtility.Repaint();
                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    if (position.Contains(Event.current.mousePosition) && GUI.enabled)
                    {
                        Object[] references = DragAndDrop.objectReferences;
                        Object validatedObject = validator(references, objType, null, ObjectFieldValidatorOptions.None);

                        if (validatedObject != null)
                        {
                            // If scene objects are not allowed and object is a scene object then clear
                            if (!allowSceneObjects && !EditorUtility.IsPersistent(validatedObject))
                                validatedObject = null;
                        }

                        if (validatedObject != null)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                            if (eventType == EventType.DragPerform)
                            {
                                GUI.changed = true;
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.activeControlID = 0;
                                Event.current.Use();
                                return validatedObject;
                            }
                            else
                            {
                                DragAndDrop.activeControlID = id;
                                Event.current.Use();
                            }
                        }
                    }
                    break;
                case EventType.Repaint:
                    style.Draw(position, GUIContent.none, id, DragAndDrop.activeControlID == id);
                    break;
            }
            return null;
        }
    }

    internal class ObjectPreviewPopup : PopupWindowContent
    {
        readonly Editor m_Editor;
        readonly GUIContent m_ObjectName;
        const float kToolbarHeight = 22f;

        internal class Styles
        {
            public readonly GUIStyle toolbar = "preToolbar";
            public readonly GUIStyle toolbarText = "ToolbarBoldLabel";
            public GUIStyle background = "preBackground";
        }
        Styles s_Styles;

        public ObjectPreviewPopup(Object previewObject)
        {
            if (previewObject == null)
            {
                Debug.LogError("ObjectPreviewPopup: Check object is not null, before trying to show it!");
                return;
            }
            m_ObjectName = new GUIContent(previewObject.name, AssetDatabase.GetAssetPath(previewObject));   // Show path as tooltip on label
            m_Editor = Editor.CreateEditor(previewObject);
        }

        public override void OnClose()
        {
            if (m_Editor != null)
                Editor.DestroyImmediate(m_Editor);
        }

        public override void OnGUI(Rect rect)
        {
            if (m_Editor == null)
            {
                editorWindow.Close();
                return;
            }

            if (s_Styles == null)
                s_Styles = new Styles();

            // Toolbar
            Rect toolbarRect = EditorGUILayout.BeginHorizontal(GUIContent.none, s_Styles.toolbar, GUILayout.Height(kToolbarHeight));
            {
                GUILayout.FlexibleSpace();
                Rect contentRect = EditorGUILayout.BeginHorizontal();
                m_Editor.OnPreviewSettings();
                EditorGUILayout.EndHorizontal();

                const float kPadding = 5f;
                Rect labelRect = new Rect(toolbarRect.x + kPadding, toolbarRect.y, toolbarRect.width - contentRect.width - 2 * kPadding, toolbarRect.height);
                Vector2 labelSize = s_Styles.toolbarText.CalcSize(m_ObjectName);
                labelRect.width = Mathf.Min(labelRect.width, labelSize.x);
                m_ObjectName.tooltip = m_ObjectName.text;
                GUI.Label(labelRect, m_ObjectName, s_Styles.toolbarText);
            }
            EditorGUILayout.EndHorizontal();

            // Object preview
            Rect previewRect = new Rect(rect.x, rect.y + kToolbarHeight, rect.width, rect.height - kToolbarHeight);
            m_Editor.OnPreviewGUI(previewRect, s_Styles.background);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(600f, 300f + kToolbarHeight);
        }
    }
}
