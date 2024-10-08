using System;
using UnityEngine;
using UnityEngine.UIElements;
using Graphlit.Nodes;
using Graphlit.Nodes.PortType;

namespace Graphlit
{
    [NodeInfo("Input/View Direction"), Serializable]
    public class ViewDirectionNode : ShaderNode
    {
        public override PreviewType DefaultPreviewOverride => PreviewType.Preview3D;
        [SerializeField] BindingSpace _space = BindingSpace.World;

        public override void Initialize()
        {
            AddPort(new(PortDirection.Output, new Float(3), 0));
            Bind(0, PortBindings.ViewBindingFromSpace(_space));

            var dropdown = new EnumField(_space);

            dropdown.RegisterValueChangedCallback((evt) =>
            {
                _space = (BindingSpace)evt.newValue;
                Bind(0, PortBindings.ViewBindingFromSpace(_space));
                GeneratePreviewForAffectedNodes();
            });
            inputContainer.Add(dropdown);
        }

        protected override void Generate(NodeVisitor visitor)
        {
        }
    }
}