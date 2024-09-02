using Enlit.Nodes;

namespace Enlit
{
    [NodeInfo("Math/Square Root")]
    public class SqrtNode : PassthroughNode
    {
        protected override void Generate(NodeVisitor visitor)
        {
            base.Generate(visitor);
            Output(visitor, OUT, $"sqrt({PortData[IN].Name})");
        }
    }
}