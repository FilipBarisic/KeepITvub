using System.Collections.ObjectModel;

namespace KeepIT
{
    public static class TreeNode
    {
        public enum NodeType
        {
            Drive,
            Folder,
            DummyFolder
        }

        public sealed class NodeData
        {
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public NodeType Type { get; set; }

            public ObservableCollection<NodeData> Children { get; } = new();

            public bool IsLoaded { get; set; }
            public bool IsDummy => Type == NodeType.DummyFolder;

            public override string ToString() => Name;
        }
    }
}