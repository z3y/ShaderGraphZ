using Enlit.Nodes;

namespace Enlit
{
    [NodeInfo("Math/Length")]
    public class LengthhNode : PassthroughNode
    {
        protected override void Generate(NodeVisitor visitor)
        {
            base.Generate(visitor);
            Output(visitor, OUT, $"length({PortData[IN].Name})");
        }
    }
}