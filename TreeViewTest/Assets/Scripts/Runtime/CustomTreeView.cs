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
            activeTasks = new TreeViewManager("activeTasks.json", data.activeTasks);
            activeTasks.LoadRuntimeData();
            activeTasks.PopulateList();
            
            archivedTasks = new TreeViewManager("archivedTasks.json", data.archivedTasks);
            archivedTasks.LoadRuntimeData();
            archivedTasks.PopulateList();

            background = treeViewDocument.rootVisualElement.Q("background");

            SetupAddButton();
            
            background.Add(activeTasks.SetupTreeView(itemHeight, itemTemplate));
            SetupDivider();
            background.Add(AddArchiveButton());

            var archivedTreeView = archivedTasks.SetupTreeView(itemHeight, itemTemplate);
            background.Add(archivedTreeView);
            SetupArchiveButton(archivedTreeView);
        }

        private void SetupArchiveButton(TreeView archivedTreeView)
        {
            background.Q<Button>("archiveButton").clicked += () =>
            {
                archivedTreeView.style.display = archivedTreeView.style.display == DisplayStyle.None
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            };
        }

        private VisualElement AddArchiveButton()
        {
            var button = new Button
            {
                text = "Archive",
                name = "archiveButton"
            };
            
            return button;
        }

        private void SetupDivider()
        {
            // Small visual divider between active and archived lists
            if (background == null) return;

            var divider = new VisualElement { name = "divider" };
            // Thin horizontal line
            divider.style.height = 1;
            divider.style.marginTop = 8;
            divider.style.marginBottom = 6;
            divider.style.backgroundColor = new StyleColor(new Color(0.6f, 0.6f, 0.6f, 1f));

            // Label for the archived section
            var archivedLabel = new Label("Archived Tasks") { name = "archivedLabel" };
            archivedLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            archivedLabel.style.marginTop = 4;
            archivedLabel.style.marginBottom = 4;
            archivedLabel.style.fontSize = 12;
            archivedLabel.style.color = new StyleColor(Color.white);

            background.Add(divider);
            background.Add(archivedLabel);
        }

        private void OnDisable()
        {
            activeTasks.SaveRuntimeData();
            archivedTasks.SaveRuntimeData();
        }

        private void SetupAddButton()
        {
            background.Q<Button>("addButton").clicked += () =>
            {
                var newItem = new DataItem { Name = data.activeTasks.Count.ToString()};
                data.activeTasks.Add(newItem);
                activeTasks.AddItem(newItem);
                activeTasks.UpdateView();
                activeTasks.SaveRuntimeData();
            };
        }
    }
}
