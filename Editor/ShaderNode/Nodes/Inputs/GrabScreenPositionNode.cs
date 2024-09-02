using Enlit.Nodes;
using Enlit.Nodes.PortType;

namespace Enlit
{
    [NodeInfo("Input/Grab Screen Position")]
    public class GrabScreenPositionNode : ShaderNode
    {
        protected const int OUT = 0;

        public override void Initialize()
        {
            AddPort(new(PortDirection.Output, new Float(2), OUT));
            Bind(OUT, PortBinding.GrabScreenPosition);
        }

        protected override void Generate(NodeVisitor visitor)
        {
        }
    }
}