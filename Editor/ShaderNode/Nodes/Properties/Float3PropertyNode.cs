using System;
using Graphlit.Nodes;
using Graphlit.Nodes.PortType;

namespace Graphlit
{
    [NodeInfo("Input/Float3 Property"), Serializable]
    public class Float3PropertyNode : PropertyNode
    {
        protected override PropertyType propertyType => PropertyType.Float3;
        public override void Initialize()
        {
            base.Initialize();
            AddPort(new(PortDirection.Output, new Float(3), OUT, "Float3"));
        }
    }
}