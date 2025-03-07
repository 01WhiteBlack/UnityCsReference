// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class Trigger
    {
        IDirectory m_Directory;
        IConflictResolver m_ConflictResolver;
        IContextManager m_CurrentContextManager;

        List<KeyCombination> m_KeyCombinationSequence = new List<KeyCombination>();
        List<ShortcutEntry> m_Entries = new List<ShortcutEntry>();
        Dictionary<KeyCode, KeyValuePair<ShortcutEntry, object>> m_ActiveClutches = new Dictionary<KeyCode, KeyValuePair<ShortcutEntry, object>>();

        public event Action<ShortcutEntry, ShortcutArguments> invokingAction;

        public Trigger(IDirectory directory, IConflictResolver conflictResolver)
        {
            m_Directory = directory;
            m_ConflictResolver = conflictResolver;
        }

        public void HandleKeyEvent(Event evt, IContextManager contextManager)
        {
            if (evt == null) return;

            KeyCode keyCode = evt.isMouse ? evt.button + KeyCode.Mouse0 : evt.keyCode;

            if (keyCode == KeyCode.None) return;

            if (evt.type == EventType.KeyUp || evt.type == EventType.MouseUp)
            {
                KeyValuePair<ShortcutEntry, object> clutchKeyValuePair;
                if (m_ActiveClutches.TryGetValue(keyCode, out clutchKeyValuePair))
                {
                    var clutchContext = m_ActiveClutches[keyCode].Value;

                    m_ActiveClutches.Remove(keyCode);
                    var args = new ShortcutArguments
                    {
                        context = clutchContext,
                        stage = ShortcutStage.End
                    };
                    invokingAction?.Invoke(clutchKeyValuePair.Key, args);
                    clutchKeyValuePair.Key.action(args);
                    evt.Use();
                }
                return;
            }

            // Use the event and return if the key is currently used in an active clutch
            if (m_ActiveClutches.ContainsKey(keyCode))
            {
                evt.Use();
                return;
            }

            var keyCodeCombination = KeyCombination.FromInput(evt);
            m_KeyCombinationSequence.Add(keyCodeCombination);

            // Ignore event if sequence is empty
            if (m_KeyCombinationSequence.Count == 0)
                return;

            m_Directory.FindShortcutEntries(m_KeyCombinationSequence, contextManager, m_Entries);
            IEnumerable<ShortcutEntry> entries = m_Entries;

            // Deal ONLY with prioritycontext
            if (entries.Count() > 1 && contextManager.HasAnyPriorityContext())
            {
                m_CurrentContextManager = contextManager;
                entries = m_Entries.FindAll(CurrentContextManagerHasPriorityContextFor);
                if (!entries.Any())
                    entries = m_Entries;
            }

            switch (entries.Count())
            {
                case 0:
                    Reset();
                    break;

                case 1:
                    var shortcutEntry = entries.Single();
                    if (ShortcutFullyMatchesKeyCombination(shortcutEntry))
                    {
                        if (keyCode != m_KeyCombinationSequence.Last().keyCode)
                            break;

                        var args = new ShortcutArguments();
                        args.context = contextManager.GetContextInstanceOfType(shortcutEntry.context);
                        switch (shortcutEntry.type)
                        {
                            case ShortcutType.Action:
                                args.stage = ShortcutStage.End;
                                invokingAction?.Invoke(shortcutEntry, args);
                                shortcutEntry.action(args);
                                evt.Use();
                                Reset();
                                break;

                            case ShortcutType.Clutch:
                                if (!m_ActiveClutches.ContainsKey(keyCode))
                                {
                                    m_ActiveClutches.Add(keyCode, new KeyValuePair<ShortcutEntry, object>(shortcutEntry, args.context));
                                    args.stage = ShortcutStage.Begin;
                                    invokingAction?.Invoke(shortcutEntry, args);
                                    shortcutEntry.action(args);
                                    evt.Use();
                                    Reset();
                                }
                                break;
                            case ShortcutType.Menu:
                                args.stage = ShortcutStage.End;
                                invokingAction?.Invoke(shortcutEntry, args);
                                shortcutEntry.action(args);
                                evt.Use();
                                Reset();
                                break;
                        }
                    }
                    break;

                default:
                    if (HasConflicts(entries, m_KeyCombinationSequence))
                    {
                        m_ConflictResolver.ResolveConflict(m_KeyCombinationSequence, entries);
                        evt.Use();
                        Reset();
                    }
                    break;
            }
        }

        public void ResetActiveClutches()
        {
            foreach (var clutchKeyValuePair in m_ActiveClutches.Values)
            {
                var args = new ShortcutArguments
                {
                    context = clutchKeyValuePair.Value,
                    stage = ShortcutStage.End,
                };
                invokingAction?.Invoke(clutchKeyValuePair.Key, args);
                clutchKeyValuePair.Key.action(args);
            }

            m_ActiveClutches.Clear();
        }

        public bool HasAnyEntries()
        {
            return m_Entries.Any();
        }

        // filtered entries are expected to all be in the same context and/or null context and they all are known to share the prefix
        bool HasConflicts(IEnumerable<ShortcutEntry> filteredEntries, List<KeyCombination> prefix)
        {
            if (filteredEntries.Any(e => e.FullyMatches(prefix)))
                return filteredEntries.Count() > 1;
            return false;
        }

        bool ShortcutFullyMatchesKeyCombination(ShortcutEntry shortcutEntry)
        {
            return shortcutEntry.FullyMatches(m_KeyCombinationSequence);
        }

        void Reset()
        {
            m_KeyCombinationSequence.Clear();
        }

        bool CurrentContextManagerHasPriorityContextFor(ShortcutEntry entry)
        {
            return m_CurrentContextManager.HasPriorityContextOfType(entry.context);
        }
    }
}
