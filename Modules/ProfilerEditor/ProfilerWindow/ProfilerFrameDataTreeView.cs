// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using UnityEditorInternal.Profiling;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using System.Runtime.CompilerServices;

namespace UnityEditorInternal
{
    internal class ProfilerFrameDataTreeView : TreeView
    {
        public static readonly GUIContent kFrameTooltip = EditorGUIUtility.TrTextContent("", "Press 'F' to frame selection");

        const int kMaxPooledRowsCount = 1000000;

        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(1000);
        ProfilerFrameDataMultiColumnHeader m_MultiColumnHeader;

        FrameDataView m_FrameDataView;
        FrameDataView.MarkerPath? m_SelectedItemMarkerIdPath;
        string m_LegacySelectedItemMarkerNamePath;

        // Tree of expanded nodes.
        // Each level has a set of expanded marker ids, which are equivalent to sample name.
        class ExpandedMarkerIdHierarchy
        {
            public Dictionary<int, ExpandedMarkerIdHierarchy> expandedMarkers;
        }
        [NonSerialized]
        ExpandedMarkerIdHierarchy m_ExpandedMarkersHierarchy;

        [NonSerialized]
        List<TreeViewItem> m_RowsPool = new List<TreeViewItem>();
        [NonSerialized]
        int m_RowsPoolLastItemIndex = -1;
        [NonSerialized]
        Stack<List<TreeViewItem>> m_ChildrenPool = new Stack<List<TreeViewItem>>();
        [NonSerialized]
        LinkedList<TreeTraversalState> m_ReusableVisitList = new LinkedList<TreeTraversalState>();
        [NonSerialized]
        List<int> m_ReusableChildrenIds = new List<int>(1024);
        [NonSerialized]
        Stack<LinkedListNode<TreeTraversalState>> m_TreeTraversalStatePool = new Stack<LinkedListNode<TreeTraversalState>>();

        public delegate void SelectionChangedCallback(int id);
        public event SelectionChangedCallback selectionChanged;

        public delegate void SearchChangedCallback(string newSearch);
        public event SearchChangedCallback searchChanged;

        public ProfilerColumn sortedProfilerColumn
        {
            get { return m_MultiColumnHeader.sortedProfilerColumn; }
        }

        public bool sortedProfilerColumnAscending
        {
            get { return m_MultiColumnHeader.sortedProfilerColumnAscending; }
        }

        class FrameDataTreeViewItem : TreeViewItem
        {
            FrameDataView m_FrameDataView;
            bool m_Initialized;
            string[] m_StringProperties;
            string m_ResolvedCallstack;

            public string[] columnStrings
            {
                get { return m_StringProperties; }
            }

            public string resolvedCallstack
            {
                get
                {
                    // Lazy callstack resolution (only when requested)
                    if (m_ResolvedCallstack == null)
                        m_ResolvedCallstack = m_FrameDataView.ResolveItemCallstack(id);
                    return m_ResolvedCallstack;
                }
            }
            public int samplesCount
            {
                get
                {
                    return m_FrameDataView.GetItemSamplesCount(id);
                }
            }

            public FrameDataTreeViewItem(FrameDataView frameDataView, int id, int depth, TreeViewItem parent)
                : base(id, depth, parent, null)
            {
                m_FrameDataView = frameDataView;
                m_Initialized = false;
            }

            [MethodImpl((MethodImplOptions)256)]
            internal void Init(FrameDataView frameDataView, int id, int depth, TreeViewItem parent)
            {
                this.id = id;
                this.depth = depth;
                this.parent = parent;
                this.displayName = null;
                m_FrameDataView = frameDataView;
                m_Initialized = false;
            }

            public void Init(ProfilerFrameDataMultiColumnHeader.Column[] columns)
            {
                if (m_Initialized)
                    return;

                m_StringProperties = new string[columns.Length];
                for (var i = 0; i < columns.Length; i++)
                {
                    var data = m_FrameDataView.GetItemColumnData(id, columns[i].profilerColumn);
                    m_StringProperties[i] = data;
                    if (columns[i].profilerColumn == ProfilerColumn.FunctionName)
                        displayName = data;
                }

                m_Initialized = true;
            }
        }

        public ProfilerFrameDataTreeView(TreeViewState state, ProfilerFrameDataMultiColumnHeader multicolumnHeader) : base(state, multicolumnHeader)
        {
            Assert.IsNotNull(multicolumnHeader);
            m_MultiColumnHeader = multicolumnHeader;
            m_MultiColumnHeader.sortingChanged += OnSortingChanged;
        }

        public void SetFrameDataView(FrameDataView frameDataView)
        {
            var needReload = !Equals(m_FrameDataView, frameDataView);
            var needSorting = frameDataView != null && frameDataView.IsValid() &&
                (frameDataView.sortColumn != m_MultiColumnHeader.sortedProfilerColumn ||
                    frameDataView.sortColumnAscending != m_MultiColumnHeader.sortedProfilerColumnAscending);

            if (needReload)
            {
                StoreExpandedState();
                StoreSelectedState();
            }

            m_FrameDataView = frameDataView;
            if (needSorting)
                m_FrameDataView.Sort(m_MultiColumnHeader.sortedProfilerColumn, m_MultiColumnHeader.sortedProfilerColumnAscending);

            if (needReload || needSorting)
                Reload();
        }

        void AddExpandedChildrenRecursively(TreeViewItem item, ExpandedMarkerIdHierarchy expandedHierarchy)
        {
            if (item.children == null)
                return;

            for (var i = 0; i < item.children.Count; ++i)
            {
                var childItem = item.children[i];
                // Inlining !IsChildListForACollapsedParent without childList.Count == 1 check, as we only create list if we have children
                if (childItem != null && childItem.children != null && childItem.children[0] != null)
                {
                    var subHierarchy = new ExpandedMarkerIdHierarchy();
                    if (expandedHierarchy.expandedMarkers == null)
                        expandedHierarchy.expandedMarkers = new Dictionary<int, ExpandedMarkerIdHierarchy>();
                    try
                    {
                        expandedHierarchy.expandedMarkers.Add(m_FrameDataView.GetItemMarkerID(childItem.id), subHierarchy);
                    }
                    catch (ArgumentException)
                    {
                    }

                    AddExpandedChildrenRecursively(childItem, subHierarchy);
                }
            }
        }

        void StoreExpandedState()
        {
            if (m_ExpandedMarkersHierarchy != null)
                return;
            if (m_FrameDataView == null || !m_FrameDataView.IsValid())
                return;

            m_ExpandedMarkersHierarchy = new ExpandedMarkerIdHierarchy();
            AddExpandedChildrenRecursively(rootItem, m_ExpandedMarkersHierarchy);
        }

        public void SetSelectionFromLegacyPropertyPath(string selectedPropertyPath)
        {
            // if the path is the same as the current selection, don't change anything
            if (m_SelectedItemMarkerIdPath != null && PropertyPathMatchesSelectedIDs(selectedPropertyPath, state.selectedIDs))
                return;

            m_LegacySelectedItemMarkerNamePath = selectedPropertyPath;
            m_SelectedItemMarkerIdPath = null;
        }

        public void OnFrameDataViewAboutToBeDisposed()
        {
            if (m_FrameDataView == null || !m_FrameDataView.IsValid())
                return;
            StoreExpandedState();
            StoreSelectedState();
        }

        private bool PropertyPathMatchesSelectedIDs(string legacyPropertyPath, List<int> selectedIDs)
        {
            if (m_FrameDataView == null || !m_FrameDataView.IsValid())
                return false;

            if (string.IsNullOrEmpty(legacyPropertyPath) || selectedIDs == null || selectedIDs.Count == 0)
            {
                return string.IsNullOrEmpty(legacyPropertyPath) && (selectedIDs == null || selectedIDs.Count == 0);
            }

            return m_FrameDataView.GetItemPath(selectedIDs[0]) == legacyPropertyPath;
        }

        void StoreSelectedState()
        {
            if (m_SelectedItemMarkerIdPath != null || m_LegacySelectedItemMarkerNamePath != null)
                return;

            if (m_FrameDataView == null || !m_FrameDataView.IsValid())
                return;
            var oldSelection = GetSelection();
            if (oldSelection.Count == 0)
                return;

            m_SelectedItemMarkerIdPath = m_FrameDataView.GetItemMarkerIDPath(oldSelection[0]);
        }

        void MigrateExpandedState(List<int> newExpandedIds)
        {
            if (newExpandedIds == null)
                return;

            state.expandedIDs = newExpandedIds;
        }

        void MigrateSelectedState(bool expandIfNecessary)
        {
            if (m_SelectedItemMarkerIdPath == null && m_LegacySelectedItemMarkerNamePath == null)
                return;

            // Find view id which corresponds to markerPath
            var newSelectedId = m_FrameDataView.GetRootItemID();
            bool selectedItemsPathIsExpanded = true;
            if (m_SelectedItemMarkerIdPath != null)
            {
                foreach (var marker in m_SelectedItemMarkerIdPath.Value.markerIds)
                {
                    if (m_FrameDataView.HasItemChildren(newSelectedId))
                    {
                        m_FrameDataView.GetItemChildren(newSelectedId, m_ReusableChildrenIds);
                        foreach (var childId in m_ReusableChildrenIds)
                        {
                            if (marker == m_FrameDataView.GetItemMarkerID(childId))
                            {
                                // check if the parent is expanded
                                if (!IsExpanded(newSelectedId))
                                    selectedItemsPathIsExpanded = false;

                                newSelectedId = childId;
                                break;
                            }
                        }
                    }

                    if (newSelectedId == 0)
                        break;
                }
            }
            else if (m_LegacySelectedItemMarkerNamePath != null)
            {
                var markerIdPath = new List<int>();
                var markerNames = m_LegacySelectedItemMarkerNamePath.Split('/');
                foreach (var markerName in markerNames)
                {
                    if (m_FrameDataView.HasItemChildren(newSelectedId))
                    {
                        m_FrameDataView.GetItemChildren(newSelectedId, m_ReusableChildrenIds);
                        foreach (var childId in m_ReusableChildrenIds)
                        {
                            if (markerName == m_FrameDataView.GetItemFunctionName(childId))
                            {
                                // check if the parent is expanded
                                if (!IsExpanded(newSelectedId))
                                    selectedItemsPathIsExpanded = false;

                                newSelectedId = childId;
                                markerIdPath.Add(m_FrameDataView.GetItemMarkerID(childId));
                                break;
                            }
                        }
                    }

                    if (newSelectedId == 0)
                        break;
                }

                m_SelectedItemMarkerIdPath = new FrameDataView.MarkerPath(markerIdPath);
                m_LegacySelectedItemMarkerNamePath = null;
            }

            var newSelection = (newSelectedId == 0) ? new List<int>() : new List<int>() { newSelectedId };
            state.selectedIDs = newSelection;

            // Framing invalidates expanded state and this is very expensive operation to perform each frame.
            // Thus we auto frame selection only when we are not profiling.
            var collectingSamples = ProfilerDriver.enabled && (ProfilerDriver.profileEditor || EditorApplication.isPlaying);
            var isFramingAllowed = !collectingSamples;
            if (newSelectedId != 0 && isInitialized && isFramingAllowed && (selectedItemsPathIsExpanded || expandIfNecessary))
                FrameItem(newSelectedId);
        }

        public IList<int> GetSelectedInstanceIds()
        {
            if (m_FrameDataView == null || !m_FrameDataView.IsValid())
                return null;
            var selection = GetSelection();
            if (selection == null || selection.Count == 0)
                return null;

            var instanceIds = new List<int>();
            foreach (var selectedId in selection)
                instanceIds.AddRange(m_FrameDataView.GetItemInstanceIDs(selectedId));
            return instanceIds;
        }

        public void Clear()
        {
            if (m_FrameDataView == null)
                return;

            m_FrameDataView.Dispose();
            m_FrameDataView = null;

            m_RowsPool.Clear();
            m_RowsPoolLastItemIndex = -1;
            m_ChildrenPool.Clear();
            m_ReusableVisitList.Clear();
            m_ReusableChildrenIds.Clear();
            m_TreeTraversalStatePool.Clear();

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var rootID = m_FrameDataView != null ? m_FrameDataView.GetRootItemID() : 0;
            return new FrameDataTreeViewItem(m_FrameDataView, rootID, -1, null);
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (m_RowsPool.Count < kMaxPooledRowsCount)
            {
                // clean out items in the pool that where currently in use.
                if (m_RowsPoolLastItemIndex + 1 < m_RowsPool.Count)
                    m_RowsPool.RemoveRange(m_RowsPoolLastItemIndex + 1, m_RowsPool.Count - (m_RowsPoolLastItemIndex + 1));
                // pool current rows
                m_RowsPool.AddRange(m_Rows);
                m_RowsPoolLastItemIndex += m_Rows.Count;
            }
            m_Rows.Clear();

            if (m_FrameDataView == null || !m_FrameDataView.IsValid())
                return m_Rows;

            var newExpandedIds = m_ExpandedMarkersHierarchy == null ? null : new List<int>(state.expandedIDs.Count);
            if (!string.IsNullOrEmpty(searchString))
            {
                Search(root, searchString, m_Rows);
            }
            else
            {
                AddAllChildren((FrameDataTreeViewItem)root, m_ExpandedMarkersHierarchy, m_Rows, newExpandedIds);
            }

            MigrateExpandedState(newExpandedIds);
            MigrateSelectedState(false);

            return m_Rows;
        }

        void Search(TreeViewItem searchFromThis, string search, List<TreeViewItem> result)
        {
            if (searchFromThis == null)
                throw new ArgumentException("Invalid searchFromThis: cannot be null", "searchFromThis");
            if (string.IsNullOrEmpty(search))
                throw new ArgumentException("Invalid search: cannot be null or empty", "search");

            const int kItemDepth = 0; // tree is flattened when searching

            var stack = new Stack<int>();
            m_FrameDataView.GetItemChildren(searchFromThis.id, m_ReusableChildrenIds);
            foreach (var childId in m_ReusableChildrenIds)
                stack.Push(childId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                // Matches search?
                var functionName = m_FrameDataView.GetItemFunctionName(current);
                if (functionName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var item = AcquireFrameDataTreeViewItem(m_FrameDataView, current, kItemDepth, searchFromThis);
                    searchFromThis.AddChild(item);
                    result.Add(item);
                }

                m_FrameDataView.GetItemChildren(current, m_ReusableChildrenIds);
                foreach (var childId in m_ReusableChildrenIds)
                    stack.Push(childId);
            }
        }

        // Hierarchy traversal state.
        // Represents node to descent into and expansion state of its children.
        // This way we follow samples and expansion hierarchy simultaneously avoiding expensive
        // expansion lookup by a full sample path in a global table.
        struct TreeTraversalState
        {
            public FrameDataTreeViewItem item;
            public ExpandedMarkerIdHierarchy expandedHierarchy;
        }

        [MethodImpl((MethodImplOptions)256)]
        LinkedListNode<TreeTraversalState> AcquireTreeTraversalStateNode(FrameDataTreeViewItem item, ExpandedMarkerIdHierarchy expandedHierarchy)
        {
            if (m_TreeTraversalStatePool.Count == 0)
                return new LinkedListNode<TreeTraversalState>(new TreeTraversalState() { item = item, expandedHierarchy = expandedHierarchy });

            var node = m_TreeTraversalStatePool.Pop();
            node.Value = new TreeTraversalState() { item = item, expandedHierarchy = expandedHierarchy };
            return node;
        }

        [MethodImpl((MethodImplOptions)256)]
        FrameDataTreeViewItem AcquireFrameDataTreeViewItem(FrameDataView frameDataView, int id, int depth, TreeViewItem parent)
        {
            if (m_RowsPoolLastItemIndex > -1)
            {
                FrameDataTreeViewItem child = (FrameDataTreeViewItem)m_RowsPool[m_RowsPoolLastItemIndex];
                --m_RowsPoolLastItemIndex;
                child.Init(m_FrameDataView, id, depth, parent);
                var children = child.children;
                if (children != null)
                {
                    m_ChildrenPool.Push(children);
                    child.children = null;
                }

                return child;
            }

            return new FrameDataTreeViewItem(m_FrameDataView, id, depth, parent);
        }

        void AddAllChildren(FrameDataTreeViewItem parent, ExpandedMarkerIdHierarchy parentExpandedHierararchy, IList<TreeViewItem> newRows, List<int> newExpandedIds)
        {
            m_ReusableVisitList.AddFirst(AcquireTreeTraversalStateNode(parent, parentExpandedHierararchy));

            // Depth-first traversal.
            // Think of it as an unrolled recursion where stack state is defined by TreeTraversalState.
            while (m_ReusableVisitList.First != null)
            {
                var currentItem = m_ReusableVisitList.First.Value.item;
                var currentExpandedHierarchy =  m_ReusableVisitList.First.Value.expandedHierarchy;
                {
                    // scope firstItem to avoid reusing it accidentally
                    var firstItem = m_ReusableVisitList.First;
                    firstItem.Value = new TreeTraversalState();
                    m_TreeTraversalStatePool.Push(firstItem);
                    m_ReusableVisitList.RemoveFirst();
                }

                // This loop can be a bit of a hot path so, micro optimizations like property caching and inlinig have some effect:
                // So... cache depth
                var currentItemDepth = currentItem.depth;
                // and cache children, remember to also set currentItem.children if a new list is assigned
                var currentItemChildren = currentItem.children;

                if (currentItemDepth != -1)
                    newRows.Add(currentItem);

                m_FrameDataView.GetItemChildren(currentItem.id, m_ReusableChildrenIds);
                var childrenCount = m_ReusableChildrenIds.Count;
                if (childrenCount == 0)
                    continue;

                if (currentItemDepth != -1)
                {
                    // Check expansion state from a previous frame view state (marker id path) or current tree view state (frame-specific id).
                    bool needsExpansion;
                    if (m_ExpandedMarkersHierarchy == null)
                    {
                        // When we alter expansion state of the currently selected frame,
                        // we rely on TreeView's IsExpanded functionality.
                        needsExpansion = IsExpanded(currentItem.id);
                    }
                    else
                    {
                        // When we switch to another frame, we rebuild expanded state based on stored m_ExpandedMarkersHierarchy
                        // which represents tree of expanded nodes.
                        needsExpansion = currentExpandedHierarchy != null;
                    }

                    if (!needsExpansion)
                    {
                        if (currentItemChildren == null)
                        {
                            // Lets create a fake laze child list.
                            // Not using CreateChildListForCollapsedParent as that list returns a static list and will cause funkyness later on.
                            // We have a pool, so we're gonna use that.
                            if (m_ChildrenPool.Count > 0)
                            {
                                currentItem.children = currentItemChildren = m_ChildrenPool.Pop();
                                currentItemChildren.Clear();
                            }
                            else
                                currentItem.children = currentItemChildren = new List<TreeViewItem>();
                            currentItemChildren.Add(null);
                        }
                        continue;
                    }

                    if (newExpandedIds != null)
                        newExpandedIds.Add(currentItem.id);
                }

                // Generate children based on the view data.
                if (currentItemChildren == null)
                {
                    // Reuse existing list.
                    if (m_ChildrenPool.Count > 0)
                        currentItem.children = currentItemChildren = m_ChildrenPool.Pop();
                    else
                        currentItem.children = currentItemChildren = new List<TreeViewItem>();
                }
                currentItemChildren.Clear();
                const int k_MaxSuperflousCapacity = 32 - 1;
                // only reset capacity if needed or if too much space would be wasted. Otherwise, what's the point of pooling them?
                if (currentItemChildren.Capacity < childrenCount || currentItemChildren.Capacity > childrenCount + k_MaxSuperflousCapacity)
                    currentItemChildren.Capacity = childrenCount;

                for (var i = 0; i < childrenCount; ++i)
                {
                    var child = AcquireFrameDataTreeViewItem(m_FrameDataView, m_ReusableChildrenIds[i], currentItemDepth + 1, currentItem);
                    currentItemChildren.Add(child);
                }

                // Add children to the traversal list.
                // We add all of them in front, so it is depth search, but with preserved siblings order.
                LinkedListNode<TreeTraversalState> prev = null;
                foreach (var child in currentItemChildren)
                {
                    var childMarkerId = m_FrameDataView.GetItemMarkerID(child.id);
                    ExpandedMarkerIdHierarchy childExpandedHierarchy = null;
                    if (currentExpandedHierarchy != null && currentExpandedHierarchy.expandedMarkers != null)
                        currentExpandedHierarchy.expandedMarkers.TryGetValue(childMarkerId, out childExpandedHierarchy);

                    var traversalState = AcquireTreeTraversalStateNode((FrameDataTreeViewItem)child, childExpandedHierarchy);
                    if (prev == null)
                        m_ReusableVisitList.AddFirst(traversalState);
                    else
                        m_ReusableVisitList.AddAfter(prev, traversalState);
                    prev = traversalState;
                }
            }

            if (newExpandedIds != null)
                newExpandedIds.Sort();
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            // When we navigate through frames and there is no path exists,
            // we still want to be able to frame and select proper sample once it is present again.
            // Thus we invalidate selection only if user selected new item.
            // Same applies to expanded state.
            if (selectedIds.Count > 0)
            {
                m_SelectedItemMarkerIdPath = null;
                m_LegacySelectedItemMarkerNamePath = null;
            }

            var id = selectedIds.Count > 0 ? selectedIds[0] : -1;
            if (selectionChanged != null)
                selectionChanged.Invoke(id);

            if (selectedIds.Count > 0)
                // Selection change event handling will set this path and cause a selection migration, which is unnecessary and will fail in Raw Hierarchy
                m_LegacySelectedItemMarkerNamePath = null;
        }

        protected override void ExpandedStateChanged()
        {
            // Invalidate saved expanded state if user altered current state.
            m_ExpandedMarkersHierarchy = null;
        }

        protected override void DoubleClickedItem(int id)
        {
        }

        protected override void ContextClickedItem(int id)
        {
        }

        protected override void ContextClicked()
        {
        }

        protected override void SearchChanged(string newSearch)
        {
            if (searchChanged != null)
                searchChanged.Invoke(newSearch);
        }

        protected override IList<int> GetAncestors(int id)
        {
            if (m_FrameDataView == null)
                return new List<int>();

            return m_FrameDataView.GetItemAncestors(id);
        }

        protected override IList<int> GetDescendantsThatHaveChildren(int id)
        {
            if (m_FrameDataView == null)
                return new List<int>();

            return m_FrameDataView.GetItemDescendantsThatHaveChildren(id);
        }

        void OnSortingChanged(MultiColumnHeader header)
        {
            if (m_FrameDataView == null || multiColumnHeader.sortedColumnIndex == -1)
                return; // No column to sort for (just use the order the data are in)

            m_FrameDataView.Sort(m_MultiColumnHeader.sortedProfilerColumn, m_MultiColumnHeader.sortedProfilerColumnAscending);
            Reload();
        }

        public override void OnGUI(Rect rect)
        {
            if (m_LegacySelectedItemMarkerNamePath != null)
                MigrateSelectedState(true);

            base.OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (Event.current.rawType != EventType.Repaint)
                return;

            var item = (FrameDataTreeViewItem)args.item;
            item.Init(m_MultiColumnHeader.columns);

            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var cellRect = args.GetCellRect(i);
                CellGUI(cellRect, item, i == 0, args.GetColumn(i), ref args);
            }

            // Tooltip logic only when item is selected
            if (!args.selected)
                return;

            var hovered = args.rowRect.Contains(Event.current.mousePosition);
            if (!hovered)
                return;

            // Framing hint when searching
            if (hasSearch)
            {
                GUIStyle.SetMouseTooltip(kFrameTooltip.tooltip, args.rowRect);
                return;
            }
        }

        void CellGUI(Rect cellRect, FrameDataTreeViewItem item, bool needsIndent, int column, ref RowGUIArgs args)
        {
            if (needsIndent)
            {
                var indent = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                cellRect.xMin += indent;
            }
            CenterRectUsingSingleLineHeight(ref cellRect);

            var content = GUIContent.Temp(item.columnStrings[column], string.Empty);
            DefaultStyles.label.Draw(cellRect, content, false, false, args.selected, args.focused);
        }
    }

    public class ProfilerFrameDataMultiColumnHeader : MultiColumnHeader
    {
        public struct Column
        {
            public ProfilerColumn profilerColumn;
            public GUIContent headerLabel;
        }
        Column[] m_Columns;

        public Column[] columns
        {
            get { return m_Columns; }
        }

        public ProfilerColumn sortedProfilerColumn
        {
            get { return GetProfilerColumn(sortedColumnIndex); }
        }
        public bool sortedProfilerColumnAscending
        {
            get { return IsSortedAscending(sortedColumnIndex); }
        }

        public ProfilerFrameDataMultiColumnHeader(MultiColumnHeaderState state, Column[] columns)
            : base(state)
        {
            Assert.IsNotNull(columns);
            m_Columns = columns;
        }

        public int GetMultiColumnHeaderIndex(ProfilerColumn profilerColumn)
        {
            for (var i = 0; i < m_Columns.Length; ++i)
            {
                if (m_Columns[i].profilerColumn == profilerColumn)
                    return i;
            }

            return 0;
        }

        public static int GetMultiColumnHeaderIndex(Column[] columns, ProfilerColumn profilerColumn)
        {
            for (var i = 0; i < columns.Length; ++i)
            {
                if (columns[i].profilerColumn == profilerColumn)
                    return i;
            }

            return 0;
        }

        public ProfilerColumn GetProfilerColumn(int multiColumnHeaderIndex)
        {
            return m_Columns[multiColumnHeaderIndex].profilerColumn;
        }
    }
}
