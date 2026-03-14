using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Runtime
{
    public class TreeViewManager
    {
        private readonly CustomTreeView _customTreeView;

        private readonly string fileName;

        private TreeView treeView;

        private List<TreeViewItemData<DataItem>> items;

        private int currentId;

        public void AddItem(DataItem newItem)
        {
            var newId = currentId++;
            var itemData = new TreeViewItemData<DataItem>(newId, newItem, new List<TreeViewItemData<DataItem>>());
            items.Add(itemData);
        }

        public TreeViewManager(CustomTreeView customTreeView, string fileName)
        {
            _customTreeView = customTreeView;
            this.fileName = fileName;
        }

        public TreeView SetupTreeView()
        {
            treeView = new TreeView(_customTreeView.ItemHeight, MakeItem, (element, index) => BindItems(element, index, treeView))
            {
                reorderable = _customTreeView.Reorderable,
                horizontalScrollingEnabled = true,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight
            };
            treeView.SetRootItems(items);
            treeView.handleDrop += args => HandleDrop(args, _customTreeView.Data.Items, items);
            
            return treeView;
        }

        private DragVisualMode HandleDrop(HandleDragAndDropArgs args, List<DataItem> dataItems, List<TreeViewItemData<DataItem>> viewItemDatas)
        {
            // Only handle Move operations
            if (args.dragAndDropData.visualMode != DragVisualMode.Move)
                return DragVisualMode.None;

            var expandedItems = GetExpandedItemIds(viewItemDatas);

            // Build list of dragged DataItem references from current selection
            var dropSourceView = args.dragAndDropData.source as TreeView;
            var draggedIndices = new List<int>(dropSourceView.selectedIndices);
            if (draggedIndices.Count == 0) return DragVisualMode.None;

            var draggedItems = new List<DataItem>(draggedIndices.Count);
            foreach (var idx in draggedIndices)
                draggedItems.Add(dropSourceView.GetItemDataForIndex<DataItem>(idx));

            // Resolve target parent and target list
            List<DataItem> targetList = dataItems;
            DataItem targetParent = null;
            if (args.parentId != -1)
            {
                var parentIndex = dropSourceView.viewController.GetIndexForId(args.parentId);
                if (parentIndex >= 0)
                {
                    targetParent = dropSourceView.GetItemDataForIndex<DataItem>(parentIndex);
                    if (targetParent.Children == null)
                        targetParent.Children = new List<DataItem>();
                    targetList = targetParent.Children;
                }
            }

            // Determine insertion index
            int insertAt = args.insertAtIndex;
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
                if (FindParentListAndIndex(dataItems, d, out var pList, out var pIdx))
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
                RemoveDataItemRecursive(dataItems, d);

            // Insert items into target list preserving order
            if (insertAt > targetList.Count) insertAt = targetList.Count;
            InsertDataItemsAt(targetList, insertAt, draggedItems);

            // Rebuild tree view data and refresh
            PopulateList();
            dropSourceView.SetRootItems(viewItemDatas);
            dropSourceView.RefreshItems();

            foreach (var item in expandedItems)
            {
                dropSourceView.ExpandItem(item);
            }

            SaveRuntimeData();

            return DragVisualMode.Move;
        }

        public void SaveRuntimeData()
        {
            Debug.Log("save called");
            
            if (_customTreeView.Data == null)
                return;

            if (_customTreeView.Data.Items == null) _customTreeView.Data.Items = new List<DataItem>();

            if (!TreeViewRuntimeStorage.TrySave(
                    fileName,
                    new TreeViewRuntimeData { items = _customTreeView.Data.Items },
                    out var error))
            {
                Debug.LogWarning($"Runtime tree data save failed: {error}");
            }
        }

        public void PopulateList()
        {
            currentId = 0;
            items = new List<TreeViewItemData<DataItem>>();
            items = BuildTreeItems(_customTreeView.Data.Items, ref currentId);
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
                // dataItem.Value = currentLocalId;
                list.Add(item);
            }

            return list;
        }

        private TemplateContainer MakeItem()
        {
            var item = _customTreeView.ItemTemplate.CloneTree();

            return item;
        }

        private void BindItems(VisualElement element, int index, TreeView treeView)
        {
            var dataItem = treeView.GetItemDataForIndex<DataItem>(index);
            element.dataSource = dataItem;

            var archiveToggle = element.Q<Toggle>("archiveToggle");
            archiveToggle.RegisterValueChangedCallback(evt =>
            {
                Debug.Log("toggled");
            });

            var nameLabel = element.Q<TextField>("name");
            UIHelper.BindTwoWay(nameLabel, nameof(dataItem.Name));

            var idLabel = element.Q<TextField>("description");
            UIHelper.BindTwoWay(idLabel, nameof(dataItem.Description));
            
            var estMinField = element.Q<UnsignedIntegerField>("estimatedMinutes");
            UIHelper.BindTwoWay(estMinField, nameof(dataItem.EstimatedMinutes));
            
            var expChallengesField = element.Q<TextField>("expectedChallanges");
            UIHelper.BindTwoWay(expChallengesField, nameof(dataItem.ExpectedChallenges));
            
            var encChallengesField = element.Q<TextField>("encounteredChallenges");
            UIHelper.BindTwoWay(encChallengesField, nameof(dataItem.EncounteredChallenges));
            
            var actMinField = element.Q<UnsignedIntegerField>("actualMinutes");
            UIHelper.BindTwoWay(actMinField, nameof(dataItem.ActualMinutes));
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
            if (list == null) return; 
            if (index < 0) index = 0; 
            if (index > list.Count) index = list.Count;
            for (int i = 0; i < itemsToInsert.Count; i++) 
                list.Insert(index + i, itemsToInsert[i]);
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

        public void LoadRuntimeData(Data data)
        {
            if (data == null)
                return;

            if (!TreeViewRuntimeStorage.TryLoad(fileName, out var state, out var error))
            {
                if (data.Items == null) data.Items = new List<DataItem>();

                if (error != "No persisted file exists yet.")
                    Debug.LogWarning($"Runtime tree data load failed: {error}");
                return;
            }

            if (state == null || state.items == null)
            {
                Debug.LogWarning("Runtime tree data load failed: persisted payload was null.");
                data.Items = new List<DataItem>();
                return;
            }

            data.Items = state.items;
        }

        public void UpdateView()
        {
            treeView.SetRootItems(items);
            treeView.Rebuild();
        }
    }
}