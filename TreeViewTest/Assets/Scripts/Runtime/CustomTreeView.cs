using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Runtime
{
    public class CustomTreeView : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset itemTemplate;
        [SerializeField] private UIDocument treeViewDocument;
        [SerializeField] private Data data;
        
        [Header("Settings")]
        [SerializeField] private int itemHeight;
        [SerializeField] private bool reorderable;
        
        private TreeView treeView;
        private VisualElement root;
        
        private List<TreeViewItemData<DataItem>> items;
        private int currentId;

        private void OnEnable()
        {
            PopulateList();

            root = treeViewDocument.rootVisualElement;
            
            SetupButton();

            Func<VisualElement> makeItem = () => MakeItem();
            
            treeView = new TreeView(itemHeight, makeItem, BindItems);
            treeView.reorderable = reorderable;
            treeView.horizontalScrollingEnabled = true;
            treeView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;

            treeView.SetRootItems(items);
            treeView.handleDrop += HandleDrop;
            
            root.Add(treeView);
        }

        private DragVisualMode HandleDrop(HandleDragAndDropArgs arg)
        {
            // Only handle Move operations
            if (arg.dragAndDropData.visualMode != DragVisualMode.Move)
                return DragVisualMode.None;

            // Capture expanded items (by DataItem reference) so we can restore after rebuilding
            var expandedItems = GetExpandedItemIds(items);
            Debug.Log(expandedItems.Count);
            
            // Build list of dragged DataItem references from current selection
            var draggedIndices = new List<int>(treeView.selectedIndices);
            if (draggedIndices.Count == 0) return DragVisualMode.None;

            var draggedItems = new List<DataItem>(draggedIndices.Count);
            foreach (var idx in draggedIndices)
                draggedItems.Add(treeView.GetItemDataForIndex<DataItem>(idx));

            // Resolve target parent and target list
            List<DataItem> targetList = data.Items;
            DataItem targetParent = null;
            if (arg.parentId != -1)
            {
                var parentIndex = treeView.viewController.GetIndexForId(arg.parentId);
                if (parentIndex >= 0)
                {
                    targetParent = treeView.GetItemDataForIndex<DataItem>(parentIndex);
                    if (targetParent.Children == null)
                        targetParent.Children = new List<DataItem>();
                    targetList = targetParent.Children;
                }
            }

            // Determine insertion index
            int insertAt = arg.insertAtIndex;
            if (insertAt < 0) insertAt = 0;

            // Prevent dropping into own descendant
            foreach (var d in draggedItems)
            {
                if (targetParent != null && IsDescendantOf(d, targetParent))
                {
                    Debug.LogWarning("Cannot move an item into one of its own descendants.");
                    return DragVisualMode.None;
                }
            }

            // Compute original parent lists and indexes to adjust insertion when moving within same list
            var originalParents = new List<List<DataItem>>();
            var originalIndexes = new List<int>();
            foreach (var d in draggedItems)
            {
                if (FindParentListAndIndex(data.Items, d, out var pList, out var pIdx))
                {
                    originalParents.Add(pList);
                    originalIndexes.Add(pIdx);
                }
                else
                {
                    originalParents.Add(null);
                    originalIndexes.Add(-1);
                }
            }

            if (originalParents.TrueForAll(pl => pl == targetList))
            {
                int shift = 0;
                foreach (var idx in originalIndexes)
                {
                    if (idx >= 0 && idx < insertAt) shift++;
                }
                insertAt -= shift;
                if (insertAt < 0) insertAt = 0;
            }

            // Remove dragged items from original parents
            foreach (var d in draggedItems)
                RemoveDataItemRecursive(data.Items, d);

            // Insert items into target list preserving order
            if (insertAt > targetList.Count) insertAt = targetList.Count;
            InsertDataItemsAt(targetList, insertAt, draggedItems);

            // Rebuild tree view data and refresh
            PopulateList();
            treeView.SetRootItems(items);
            treeView.RefreshItems();

            foreach (var item in expandedItems)
            {
                treeView.ExpandItem(item);
            }
            
            return DragVisualMode.Move;
        }

        private void SetupButton()
        {
            root.Q<Button>("addButton").clicked += () =>
            {
                var newItem = new DataItem { Name = data.Items.Count.ToString(), Value = 0 };
                data.Items.Add(newItem);
                // Use the running currentId so IDs remain unique across the whole tree
                var newId = currentId++;
                var itemData = new TreeViewItemData<DataItem>(newId, newItem, new List<TreeViewItemData<DataItem>>());
                items.Add(itemData);
                treeView.SetRootItems(items);
                treeView.Rebuild();
            };
        }

        private void PopulateList()
        {
            currentId = 0;
            items = new List<TreeViewItemData<DataItem>>();
            items = BuildTreeItems(data.Items, ref currentId);
        }

        private List<TreeViewItemData<DataItem>> BuildTreeItems(List<DataItem> dataItems, ref int id)
        {
            var list = new List<TreeViewItemData<DataItem>>();
            if (dataItems == null)
                return list;

            foreach (var dataItem in dataItems)
            {
                var currentLocalId = id++;

                List<TreeViewItemData<DataItem>> children = new List<TreeViewItemData<DataItem>>();
                if (dataItem.Children != null && dataItem.Children.Count > 0)
                {
                    children = BuildTreeItems(dataItem.Children, ref id);
                }

                var item = new TreeViewItemData<DataItem>(currentLocalId, dataItem, children);
                list.Add(item);
            }

            return list;
        }

        private void OnItemIndexChanged(int arg1, int arg2)
        {
            UnityEngine.Debug.Log("id" + arg1 + "moved to" + arg2);
        }

        private TemplateContainer MakeItem()
        {
            var item = itemTemplate.CloneTree();
            
            return item;
        }

        private void BindItems(VisualElement element, int index)
        {
            var dataItem = treeView.GetItemDataForIndex<DataItem>(index);
            var controller = treeView.viewController;
            dataItem.Value = controller.GetIdForIndex(index);

            var nameLabel = element.Q<Label>("name");
            nameLabel.SetBinding("value", new DataBinding
            {
                dataSource = dataItem,
                dataSourcePath = new PropertyPath(nameof(dataItem.Name)),
                bindingMode = BindingMode.ToTarget
            });

            var idLabel = element.Q<Label>("value");
            idLabel.SetBinding("value", new DataBinding
            {
                dataSource = dataItem,
                dataSourcePath = new PropertyPath(nameof(dataItem.Value)),
                bindingMode = BindingMode.ToTarget
            });
        }

        private bool FindParentListAndIndex(List<DataItem> list, DataItem target, out List<DataItem> parentList, out int index)
        {
            parentList = null; index = -1; if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], target)) { parentList = list; index = i; return true; }
                if (list[i].Children != null && FindParentListAndIndex(list[i].Children, target, out parentList, out index)) return true;
            }
            return false;
        }

        private bool RemoveDataItemRecursive(List<DataItem> list, DataItem target)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], target)) { list.RemoveAt(i); return true; }
                if (list[i].Children != null && RemoveDataItemRecursive(list[i].Children, target)) return true;
            }
            return false;
        }

        private void InsertDataItemsAt(List<DataItem> list, int index, List<DataItem> itemsToInsert)
        {
            if (list == null) return; if (index < 0) index = 0; if (index > list.Count) index = list.Count;
            for (int i = 0; i < itemsToInsert.Count; i++) list.Insert(index + i, itemsToInsert[i]);
        }

        private List<int> GetExpandedItemIds(IEnumerable<TreeViewItemData<DataItem>> treeItems)
        {
            var expanded = new List<int>();
            if (treeItems == null || treeView == null) return expanded;

            foreach (var item in treeItems)
            {
                try
                {
                    if (treeView.IsExpanded(item.id))
                        expanded.Add(item.id);
                }
                catch (Exception)
                {
                    // In some Unity versions or states IsExpanded may throw; ignore and continue
                }

                if (item.children != null)
                    expanded.AddRange(GetExpandedItemIds(item.children));
            }

            return expanded;
        }

        private void BuildDataItemMaps(IEnumerable<TreeViewItemData<DataItem>> treeItems, DataItem parent, Dictionary<DataItem, int> idMap, Dictionary<DataItem, DataItem> parentMap)
        {
            if (treeItems == null) return;
            foreach (var item in treeItems)
            {
                idMap[item.data] = item.id;
                if (parent != null) parentMap[item.data] = parent;
                if (item.children != null)
                    BuildDataItemMaps(item.children, item.data, idMap, parentMap);
            }
        }

        private bool IsDescendantOf(DataItem ancestor, DataItem possibleDescendant)
        {
            if (ancestor == null || possibleDescendant == null) return false;
            if (ancestor.Children == null) return false;
            foreach (var c in ancestor.Children)
            {
                if (ReferenceEquals(c, possibleDescendant)) return true;
                if (IsDescendantOf(c, possibleDescendant)) return true;
            }
            return false;
        }
    }
}
