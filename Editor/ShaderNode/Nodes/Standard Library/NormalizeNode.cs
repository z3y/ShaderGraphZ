using ZSG.Nodes;

namespace ZSG
{
    [NodeInfo("Math/Normalize", "normalize(a)")]
    public class NormalizeNode : PasstroughNode
    {
        protected override void Generate(NodeVisitor visitor)
        {
            base.Generate(visitor);
            Output(visitor, OUT, $"normalize({PortData[IN].Name})");
        }
    }
}