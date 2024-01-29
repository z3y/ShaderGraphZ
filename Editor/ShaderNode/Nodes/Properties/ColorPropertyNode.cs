using System;
using ZSG.Nodes;
using ZSG.Nodes.PortType;

namespace ZSG
{
    [NodeInfo("_/Color Property"), Serializable]
    public class ColorPropertyNode : PropertyNode
    {
        protected override PropertyType propertyType => PropertyType.Color;
        public override void AddElements()
        {
            base.AddElements();
            AddPort(new(PortDirection.Output, new Float(4), OUT));
        }
    }
}