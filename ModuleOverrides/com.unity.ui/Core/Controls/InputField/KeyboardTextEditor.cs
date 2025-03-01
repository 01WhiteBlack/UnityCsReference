// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    internal class KeyboardTextEditorEventHandler : TextEditorEventHandler
    {
        readonly Event m_ImguiEvent = new Event();

        // Used by our automated tests.
        internal bool m_Changed;

        const int k_LineFeed = 10;
        const int k_Space = 32;

        public KeyboardTextEditorEventHandler(TextElement textElement, TextEditingUtilities editingUtilities)
            : base(textElement, editingUtilities)
        {
            editingUtilities.multiline = textElement.edition.multiline;
        }

        public override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            switch (evt)
            {
                case FocusEvent fe:
                    OnFocus(fe);
                    break;

                case BlurEvent be:
                    OnBlur(be);
                    break;

                case KeyDownEvent kde:
                    OnKeyDown(kde);
                    break;

                case ValidateCommandEvent vce:
                    OnValidateCommandEvent(vce);
                    break;

                case ExecuteCommandEvent ece:
                    OnExecuteCommandEvent(ece);
                    break;

                case NavigationMoveEvent ne:
                    OnNavigationEvent(ne);
                    break;
                case NavigationSubmitEvent ne:
                    OnNavigationEvent(ne);
                    break;
                case NavigationCancelEvent ne:
                    OnNavigationEvent(ne);
                    break;
            }
        }

        void OnFocus(FocusEvent _)
        {
            GUIUtility.imeCompositionMode = IMECompositionMode.On;
            textElement.edition.SaveValueAndText();
        }

        void OnBlur(BlurEvent _)
        {
            GUIUtility.imeCompositionMode = IMECompositionMode.Auto;
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (!textElement.edition.hasFocus)
                return;

            m_Changed = false;

            evt.GetEquivalentImguiEvent(m_ImguiEvent);
            if (editingUtilities.HandleKeyEvent(m_ImguiEvent, false))
            {
                if (textElement.text != editingUtilities.text)
                    m_Changed = true;
                evt.StopPropagation();
            }
            else
            {
                char c = evt.character;

                // Ignore command and control keys, but not AltGr characters
                if (evt.actionKey && !(evt.altKey && c != '\0'))
                    return;

                if (!textElement.edition.multiline && (evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return))
                    textElement.edition.UpdateValueFromText?.Invoke();

                // Ignore tab & shift-tab in single-line text fields, modifier+tab in multiline text fields
                if ((evt.keyCode == KeyCode.Tab || c == '\t') &&
                    (!textElement.edition.multiline || evt.modifiers != EventModifiers.None))
                {
                    // Do the navigation manually since NavigationTabEvent doesn't pass through
                    if (c == '\t')
                    {
                        textElement.focusController.FocusNextInDirection(evt.shiftKey
                            ? VisualElementFocusChangeDirection.left
                            : VisualElementFocusChangeDirection.right);
                    }
                    return;
                }

                evt.StopPropagation();

                // When the newline character is sent, we have to check if the shift key is down also...
                // In the multiline case, this is like a return on a single line
                if (textElement.edition.multiline
                    ? c == '\n' && evt.shiftKey
                    : (c == '\n' || c == '\r' || c == k_LineFeed) && !evt.altKey)
                {
                    textElement.edition.MoveFocusToCompositeRoot?.Invoke();
                    return;
                }

                if (evt.keyCode == KeyCode.Escape)
                {
                    textElement.edition.RestoreValueAndText();
                    textElement.edition.UpdateValueFromText?.Invoke();
                    textElement.edition.MoveFocusToCompositeRoot?.Invoke();
                }

                if (!textElement.edition.AcceptCharacter(c))
                    return;

                if (c >= k_Space || c == '\t' || c == '\n' || c == '\r' || c == k_LineFeed)
                {
                    editingUtilities.Insert(c);
                    m_Changed = true;
                }
                // On windows, key presses also send events with keycode but no character. Eat them up here.
                else
                {
                    // if we have a composition string, make sure we clear the previous selection.
                    var oldIsCompositionActive = editingUtilities.isCompositionActive;
                    if(editingUtilities.UpdateImeState() || oldIsCompositionActive != editingUtilities.isCompositionActive)
                        m_Changed = true;
                }
            }
            if (m_Changed)
                UpdateLabel();
        }

        void UpdateLabel()
        {
            var oldText = editingUtilities.text;

            var imeEnabled = editingUtilities.UpdateImeState();
            if (imeEnabled && editingUtilities.ShouldUpdateImeWindowPosition())
                editingUtilities.SetImeWindowPosition(new Vector2(textElement.worldBound.x, textElement.worldBound.y));

            var fullText = editingUtilities.GeneratePreviewString(textElement.enableRichText);

            //Note that UpdateText will update editingUtilities with the latest text if validations were made.
            textElement.edition.UpdateText(fullText);

            if (!textElement.edition.isDelayed)
                textElement.edition.UpdateValueFromText?.Invoke();

            if (imeEnabled)
            {
                // Reset back to the original string. We need to do this after UpdateText as it sends a change event that will update editingUtilities.text.
                editingUtilities.text = oldText;

                // We need to move the cursor so that it appears at the end of the composition string when rendered in generateVisualContent.
                editingUtilities.EnableCursorPreviewState();
            }

            // UpdateScrollOffset needs the new geometry of the text to compute the new scrollOffset.
            // The latest text geometry is also required for updating the cusor and selection position.
            textElement.uitkTextHandle.Update();
        }

        void OnValidateCommandEvent(ValidateCommandEvent evt)
        {
            if (!textElement.edition.hasFocus)
                return;

            switch (evt.commandName)
            {
                // Handled in TextSelectingManipulator
                case EventCommandNames.Copy:
                case EventCommandNames.SelectAll:
                    return;

                case EventCommandNames.Cut:
                    if (!textElement.selection.HasSelection())
                        return;
                    break;
                case EventCommandNames.Paste:
                    if (!editingUtilities.CanPaste())
                        return;
                    break;
                case EventCommandNames.Delete:
                    break;
                case EventCommandNames.UndoRedoPerformed:
                    // TODO: ????? editor.text = text; --> see EditorGUI's DoTextField
                    break;
            }
            evt.StopPropagation();
        }

        void OnExecuteCommandEvent(ExecuteCommandEvent evt)
        {
            if (!textElement.edition.hasFocus)
                return;


            m_Changed = false;
            bool mayHaveChanged = false;
            string oldText = editingUtilities.text;
            switch (evt.commandName)
            {
                case EventCommandNames.OnLostFocus:
                    evt.StopPropagation();
                    return;
                case EventCommandNames.Cut:
                    editingUtilities.Cut();
                    mayHaveChanged = true;
                    evt.StopPropagation();
                    break;
                case EventCommandNames.Paste:
                    editingUtilities.Paste();
                    mayHaveChanged = true;
                    evt.StopPropagation();
                    break;
                case EventCommandNames.Delete:
                    // This "Delete" command stems from a Shift-Delete in the text
                    // On Windows, Shift-Delete in text does a cut whereas on Mac, it does a delete.
                    editingUtilities.Cut();
                    mayHaveChanged = true;
                    evt.StopPropagation();
                    break;
            }

            if (mayHaveChanged)
            {
                if (oldText != editingUtilities.text)
                    m_Changed = true;
                evt.StopPropagation();
            }

            if (m_Changed)
                UpdateLabel();
        }

        void OnNavigationEvent<TEvent>(NavigationEventBase<TEvent> evt) where TEvent : NavigationEventBase<TEvent>, new()
        {
            // Prevent navigation events, since we're consuming KeyDownEvents directly
            if (evt.deviceType == NavigationDeviceType.Keyboard || evt.deviceType == NavigationDeviceType.Unknown)
            {
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }
    }
}
