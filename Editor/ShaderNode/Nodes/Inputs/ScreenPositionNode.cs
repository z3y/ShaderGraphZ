using Graphlit.Nodes;
using Graphlit.Nodes.PortType;

namespace Graphlit
{
    [NodeInfo("Input/Screen Position")]
    public class ScreenPositionNode : ShaderNode
    {
        protected const int OUT = 0;

        public override void Initialize()
        {
            AddPort(new(PortDirection.Output, new Float(2), OUT));
            Bind(OUT, PortBinding.ScreenPosition);
        }

        protected override void Generate(NodeVisitor visitor)
        {
        }
    }
}