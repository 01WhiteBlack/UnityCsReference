// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Collections;

namespace UnityEditor.UIElements.Bindings
{
    class ListViewSerializedObjectBinding : SerializedObjectBindingBase, IInternalListViewBinding
    {
        ListView listView
        {
            get { return boundElement as ListView; }
            set { boundElement = value; }
        }

        SerializedObjectList m_DataList;

        SerializedProperty m_ArraySize;
        int m_ListViewArraySize;

        bool m_MakeItemSet;
        bool m_BindItemSet;
        bool m_UnbindItemSet;

        public static void CreateBind(ListView listView,
            SerializedObjectBindingContext context,
            SerializedProperty prop)
        {
            var newBinding = new ListViewSerializedObjectBinding();
            newBinding.SetBinding(listView, context, prop);
        }

        protected void SetBinding(ListView listView, SerializedObjectBindingContext context,
            SerializedProperty prop)
        {
            m_DataList = new SerializedObjectList(prop, listView.sourceIncludesArraySize);
            m_ArraySize = m_DataList.ArraySize;

            m_ListViewArraySize = m_DataList.ArraySize.intValue;
            m_LastSourceIncludesArraySize = listView.sourceIncludesArraySize;
            SetListView(listView);
            SetContext(context, m_ArraySize);


            if (m_ListViewArraySize == -1)
                UpdateArraySize();
        }

        private void SetListView(ListView lv)
        {
            if (listView != null)
            {
                if (m_BindItemSet)
                    listView.SetBindItemWithoutNotify(null);
                if (m_MakeItemSet)
                    listView.SetMakeItemWithoutNotify(null);
                if (m_UnbindItemSet)
                    listView.unbindItem = null;

                listView.itemsSource = null;
                listView.SetViewController(null);
                listView.SetDragAndDropController(null);
            }

            listView = lv;

            m_MakeItemSet = m_BindItemSet = m_UnbindItemSet = false;
            if (listView != null)
            {
                if (listView.makeItem == null)
                {
                    listView.SetMakeItemWithoutNotify(MakeListViewItem);
                    m_MakeItemSet = true;
                }

                if (listView.bindItem == null)
                {
                    listView.SetBindItemWithoutNotify(BindListViewItem);
                    m_BindItemSet = true;
                }

                if (listView.unbindItem == null)
                {
                    listView.unbindItem = UnbindListViewItem;
                    m_UnbindItemSet = true;
                }

                listView.SetViewController(new EditorListViewController());
                listView.SetDragAndDropController(new SerializedObjectListReorderableDragAndDropController(listView));
                listView.itemsSource = m_DataList;
            }
        }

        VisualElement MakeListViewItem()
        {
            return new PropertyField();
        }

        void BindListViewItem(VisualElement ve, int index)
        {
            if (m_ArraySize.intValue != m_ListViewArraySize)
            {
                // We need to wait for array size to be updated, which triggers a refresh anyway.
                return;
            }

            var field = ve as IBindable;
            if (field == null)
            {
                //we find the first Bindable
                field = ve.Query().Where(x => x is IBindable).First() as IBindable;
            }

            if (field == null)
            {
                //can't default bind to anything!
                throw new InvalidOperationException("Can't find BindableElement: please provide BindableVisualElements or provide your own Listview.bindItem callback");
            }

            var item = listView.itemsSource[index];
            var itemProp = item as SerializedProperty;

            // No need to rebind to the same path. We should use a Rebuild if we need to force it.
            if (field.bindingPath == itemProp.propertyPath)
                return;

            field.bindingPath = itemProp.propertyPath;
            bindingContext.ContinueBinding(ve, itemProp);
        }

        void UnbindListViewItem(VisualElement ve, int index)
        {
            var field = ve as IBindable;
            if (field == null)
            {
                //we find the first Bindable
                field = ve.Query().Where(x => x is IBindable).First() as IBindable;
            }

            if (field == null)
            {
                //can't default unbind anything!
                throw new InvalidOperationException("Can't find BindableElement: please provide BindableVisualElements or provide your own Listview.unbindItem callback");
            }

            ve.Unbind();
            field.bindingPath = null;
        }

        void UpdateArraySize()
        {
            m_DataList.RefreshProperties(listView.sourceIncludesArraySize);
            m_ArraySize = m_DataList.ArraySize;
            m_ListViewArraySize = m_ArraySize.intValue;
            m_LastSourceIncludesArraySize = listView.sourceIncludesArraySize;

            var isOverMaxMultiEditLimit = m_DataList.IsOverMaxMultiEditLimit;
            listView.footer?.SetEnabled(!isOverMaxMultiEditLimit);
            listView.SetOverMaxMultiEditLimit(isOverMaxMultiEditLimit, m_DataList.ArrayProperty.serializedObject.maxArraySizeForMultiEditing);

            listView.RefreshItems();

            if (listView.arraySizeField != null)
                listView.arraySizeField.showMixedValue = m_ArraySize.hasMultipleDifferentValues;
        }

        public override void Release()
        {
            isReleased = true;

            SetListView(null);

            ResetContext();
            m_DataList = null;
            m_ArraySize = null;
            m_ListViewArraySize = -1;
        }

        private bool m_LastSourceIncludesArraySize;

        protected override void ResetCachedValues()
        {
            m_ListViewArraySize = -1;
            UpdateFieldIsAttached();
        }

        public override void OnPropertyValueChanged(SerializedProperty currentPropertyIterator)
        {
            if (isReleased)
            {
                return;
            }

            try
            {
                isUpdating = true;
                UpdateArraySize();
            }
            catch (ArgumentNullException)
            {
                //this can happen when serializedObject has been disposed of
            }
            finally
            {
                isUpdating = false;
            }
        }

        public override void Update()
        {
            if (isReleased)
            {
                return;
            }

            try
            {
                ResetUpdate();

                if (!IsSynced())
                    return;

                isUpdating = true;

                var currentArraySize = m_ArraySize.intValue;
                var listViewShowsMixedValue = listView.arraySizeField is {showMixedValue: true};
                if (listViewShowsMixedValue ||
                    (listView.arraySizeField == null || int.Parse(listView.arraySizeField.value) == currentArraySize) &&
                    listView.sourceIncludesArraySize == m_LastSourceIncludesArraySize)
                    return;

                if (currentArraySize != m_ListViewArraySize ||
                    listView.sourceIncludesArraySize != m_LastSourceIncludesArraySize)
                {
                    UpdateArraySize();
                }

                return;
            }
            catch (ArgumentNullException)
            {
                //this can happen when serializedObject has been disposed of
            }
            finally
            {
                isUpdating = false;
            }

            // We unbind here
            Release();
        }
    }

    class SerializedObjectListReorderableDragAndDropController : ListViewReorderableDragAndDropController
    {
        private SerializedObjectList objectList => m_ListView.itemsSource as SerializedObjectList;

        public SerializedObjectListReorderableDragAndDropController(ListView listView)
            : base(listView) {}

        public override void OnDrop(IListDragAndDropArgs args)
        {
            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.OutsideItems:
                case DragAndDropPosition.BetweenItems:
                    // we're ok'
                    break;
                default:
                    throw new ArgumentException($"{args.dragAndDropPosition} is not supported by {nameof(SerializedObjectListReorderableDragAndDropController)}.");
            }

            base.OnDrop(args);
        }
    }

    internal class SerializedObjectList : IList
    {
        public SerializedProperty ArrayProperty { get; private set; }
        public SerializedProperty ArraySize { get; private set; }

        List<SerializedProperty> properties;

        public SerializedObjectList(SerializedProperty parentProperty, bool includeArraySize)
        {
            ArrayProperty = parentProperty.Copy();
            RefreshProperties(includeArraySize);
        }

        public void RefreshProperties(bool includeArraySize)
        {

            var property = ArrayProperty.Copy();
            var endProperty = property.GetEndProperty();

            property.NextVisible(true); // Expand the first child.

            properties = new List<SerializedProperty>();
            do
            {
                if (SerializedProperty.EqualContents(property, endProperty))
                    break;

                if (property.propertyType == SerializedPropertyType.ArraySize)
                {
                    ArraySize = property.Copy();
                    if (includeArraySize)
                    {
                        properties.Add(ArraySize);
                    }
                }
                else
                {
                    properties.Add(property.Copy());
                }
            }
            while (property.NextVisible(false));   // Never expand children.

            if (ArraySize == null)
            {
                throw new ArgumentException("Can't find array size property!");
            }
        }

        public object this[int index]
        {
            get { return properties[index]; }
            set { throw new NotImplementedException(); }
        }

        public bool IsReadOnly => true;

        public bool IsFixedSize => true;

        public int Count => properties.Count;

        bool ICollection.IsSynchronized
        {
            get { return (properties as ICollection).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return (properties as ICollection).SyncRoot; }
        }

        internal bool IsOverMaxMultiEditLimit => ArrayProperty.minArraySize > ArrayProperty.serializedObject.maxArraySizeForMultiEditing && ArrayProperty.serializedObject.isEditingMultipleObjects;

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            return IndexOf(value) >= 0;
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            return properties.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            var prop = value as SerializedProperty;

            if (value != null && prop != null)
            {
                return properties.IndexOf(prop);
            }

            return -1;
        }

        public void Move(int srcIndex, int destIndex)
        {
            if (srcIndex == destIndex)
                return;

            var sourceExpanded = ArrayProperty.GetArrayElementAtIndex(srcIndex).isExpanded;
            if (srcIndex < destIndex)
            {
                var currentProperty = ArrayProperty.GetArrayElementAtIndex(srcIndex);
                for (var i = srcIndex + 1; i <= destIndex; i++)
                {
                    var nextProperty = ArrayProperty.GetArrayElementAtIndex(i);
                    currentProperty.isExpanded = nextProperty.isExpanded;
                    currentProperty = nextProperty;
                }
            }
            else
            {
                var currentPropertyExpanded = ArrayProperty.GetArrayElementAtIndex(destIndex).isExpanded;
                for (var i = destIndex + 1; i <= srcIndex; i++)
                {
                    var nextProperty = ArrayProperty.GetArrayElementAtIndex(i);
                    var nextPropertyExpanded = nextProperty.isExpanded;
                    nextProperty.isExpanded = currentPropertyExpanded;
                    currentPropertyExpanded = nextPropertyExpanded;
                }
            }

            ArrayProperty.GetArrayElementAtIndex(destIndex).isExpanded = sourceExpanded;
            ArrayProperty.MoveArrayElement(srcIndex, destIndex);
        }

        public void ApplyChanges()
        {
            ArrayProperty.serializedObject.ApplyModifiedProperties();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            RemoveAt(IndexOf(value));
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < Count)
            {
                var currentProperty = ArrayProperty.GetArrayElementAtIndex(index);
                for (var i = index + 1; i < ArraySize.intValue; i++)
                {
                    var nextProperty = ArrayProperty.GetArrayElementAtIndex(i);
                    if (nextProperty != null)
                    {
                        currentProperty.isExpanded = nextProperty.isExpanded;
                        currentProperty = nextProperty;
                    }
                }

                ArrayProperty.DeleteArrayElementAtIndex(index);
            }
        }
    }
}
