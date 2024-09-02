using Enlit.Nodes;

namespace Enlit
{
    [NodeInfo("Math/Saturate")]
    public class SaturateNode : PassthroughNode
    {
        protected override void Generate(NodeVisitor visitor)
        {
            base.Generate(visitor);
            Output(visitor, OUT, $"saturate({PortData[IN].Name})");
        }
    }
}