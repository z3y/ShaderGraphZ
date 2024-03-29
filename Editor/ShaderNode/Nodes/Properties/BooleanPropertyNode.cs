using System;
using ZSG.Nodes;
using ZSG.Nodes.PortType;

namespace ZSG
{
    [NodeInfo("Input/Boolean Property"), Serializable]
    public class BooleanPropertyNode : PropertyNode
    {
        protected override PropertyType propertyType => PropertyType.Bool;
        public override void Initialize()
        {
            base.Initialize();
            AddPort(new(PortDirection.Output, new Bool(), OUT, "Bool"));
        }
    }
}