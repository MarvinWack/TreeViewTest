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

        private void OnEnable()
        {
            activeTasks = new TreeViewManager("currentTasks.json", data);
            activeTasks.LoadRuntimeData(data);
            activeTasks.PopulateList();

            background = treeViewDocument.rootVisualElement.Q("background");

            SetupAddButton();
            
            background.Add(activeTasks.SetupTreeView(itemHeight, itemTemplate));
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
