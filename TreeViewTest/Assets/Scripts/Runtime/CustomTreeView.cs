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

        private VisualElement background;
        private TreeViewManager activeTasks;
        private TreeViewManager archivedTasks;

        private void OnEnable()
        {
            activeTasks = new TreeViewManager("activeTasks.json", data.Items);
            activeTasks.LoadRuntimeData();
            activeTasks.PopulateList();
            
            archivedTasks = new TreeViewManager("archivedTasks.json", data.Items);
            archivedTasks.LoadRuntimeData();
            archivedTasks.PopulateList();

            background = treeViewDocument.rootVisualElement.Q("background");

            SetupAddButton();
            
            background.Add(activeTasks.SetupTreeView(itemHeight, itemTemplate));
            SetupDivider();
            background.Add(archivedTasks.SetupTreeView(itemHeight, itemTemplate));
        }

        private void SetupDivider()
        {
            throw new System.NotImplementedException();
        }

        private void OnDisable()
        {
            activeTasks.SaveRuntimeData();
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
