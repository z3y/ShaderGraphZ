using ZSG.Nodes;

namespace ZSG
{
    [NodeInfo("Math/Saturate")]
    public class SaturateNode : PasstroughNode
    {
        protected override void Generate(NodeVisitor visitor)
        {
            base.Generate(visitor);
            Output(visitor, OUT, $"saturate({PortData[IN].Name})");
        }
    }
}