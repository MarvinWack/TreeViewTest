using Unity.Properties;
using UnityEngine.UIElements;

namespace Runtime
{
    public static class UIHelper
    {
        public static void BindTwoWay(VisualElement element, string path)
        {
            element.SetBinding("value", new DataBinding
            {
                dataSourcePath = new PropertyPath(path),
                bindingMode = BindingMode.TwoWay
            });

        }
    }
}