using System.Collections.ObjectModel;

namespace KeepIT
{
    public class TreeNode
    {

        public enum NodeType
        {
            Drive,
            Folder,
            DummyFolder,
        }

        public class NodeData
        {
            public string? Name { get; set; }
            public string? Path { get; set; }
            public NodeType Type { get; set; }

            public ObservableCollection<TreeNode.NodeData> Children { get; } = new();

            public bool IsLoaded { get; set; }
            public bool IsDummy => Type == NodeType.DummyFolder;

            public override string ToString()
            {
                return Name ?? "";
            }
        }

    }
}
