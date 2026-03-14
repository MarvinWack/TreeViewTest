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
        [Header("Runtime Persistence")]
        [SerializeField] private bool persistRuntimeData = true;

        private VisualElement background;

        private readonly TreeViewManager activeTasks;

        public CustomTreeView()
        {
            activeTasks = new TreeViewManager(this, "currentTasks.json");
        }
        
        public Data Data
        {
            set { data = value; }
            get { return data; }
        }
        public Data activeTasksData
        {
            set { data = value; }
            get { return data; }
        }

        private void OnEnable()
        {
            if (persistRuntimeData) 
                activeTasks.LoadRuntimeData(activeTasksData);

            activeTasks.PopulateList();

            background = treeViewDocument.rootVisualElement.Q("background");

            SetupAddButton();
            
            background.Add(activeTasks.SetupTreeView(itemHeight, itemTemplate));
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
                activeTasks.AddItem(newItem);
                activeTasks.UpdateView();
                activeTasks.SaveRuntimeData();
            };
        }
    }
}
