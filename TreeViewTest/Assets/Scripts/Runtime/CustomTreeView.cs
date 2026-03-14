using System.Collections.Generic;
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
        [Header("Runtime Persistence")]
        [SerializeField] private bool persistRuntimeData = true;

        private VisualElement background;

        private readonly TreeViewManager activeTasks;

        public CustomTreeView()
        {
            activeTasks = new TreeViewManager(this, "currentTasks.json");
        }

        public VisualTreeAsset ItemTemplate
        {
            set { itemTemplate = value; }
            get { return itemTemplate; }
        }
        public Data Data
        {
            set { data = value; }
            get { return data; }
        }
        public int ItemHeight
        {
            set { itemHeight = value; }
            get { return itemHeight; }
        }
        public bool Reorderable
        {
            set { reorderable = value; }
            get { return reorderable; }
        }

        private void OnEnable()
        {
            if (persistRuntimeData)
                LoadRuntimeData();

            activeTasks.PopulateList();

            background = treeViewDocument.rootVisualElement.Q("background");

            SetupAddButton();

            activeTasks.currentTasksView = activeTasks.SetupTreeView();

            background.Add(activeTasks.currentTasksView);
        }

        private void OnDisable()
        {
            if (persistRuntimeData) activeTasks.SaveRuntimeData();
        }

        private void SetupAddButton()
        {
            background.Q<Button>("addButton").clicked += () =>
            {
                var newItem = new DataItem { Name = data.Items.Count.ToString()};
                data.Items.Add(newItem);
                // Use the running currentId so IDs remain unique across the whole tree
                var newId = activeTasks.currentId++;
                var itemData = new TreeViewItemData<DataItem>(newId, newItem, new List<TreeViewItemData<DataItem>>());
                activeTasks.items.Add(itemData);
                activeTasks.currentTasksView.SetRootItems(activeTasks.items);
                activeTasks.currentTasksView.Rebuild();

                activeTasks.SaveRuntimeData();
            };
        }

        private void LoadRuntimeData()
        {
            if (data == null)
                return;

            if (!TreeViewRuntimeStorage.TryLoad(activeTasks.fileName, out var state, out var error))
            {
                if (data.Items == null)
                    data.Items = new List<DataItem>();

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

        private void OnItemIndexChanged(int arg1, int arg2)
        {
            UnityEngine.Debug.Log("id" + arg1 + "moved to" + arg2);
        }
    }
}
