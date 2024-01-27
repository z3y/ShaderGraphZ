using System;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using ZSG.Nodes;
using ZSG.Nodes.PortType;

namespace ZSG
{
    [NodeInfo("Texture 2D Property"), Serializable]
    public class Texture2DPropertyNode : PropertyNode
    {
        protected override PropertyType propertyType => PropertyType.Texture2D;
        const int samplerID = 1;
        const int scaleOffsetID = 2;
        Port _scaleOffsetPort;
        public override void AddElements()
        {
            base.AddElements();

            AddPort(new(PortDirection.Output, new Texture2DObject(), OUT, "Texture 2D"));
            AddPort(new(PortDirection.Output, new SamplerState(), samplerID, "Sampler State"));
            _scaleOffsetPort = AddPort(new(PortDirection.Output, new Float(4, false), scaleOffsetID, "Scale Offset"));

            InitializeTexture(); // TODO: figure out why textures arent set on time
        }
        async void InitializeTexture()
        {
            await Task.Delay(1000);
            propertyDescriptor.UpdatePreviewMaterial();
        }

        protected override void Generate(NodeVisitor visitor)
        {
            base.Generate(visitor);
            var generation = visitor.GenerationMode;
            if (_scaleOffsetPort.connected)
            {
                var scaleOffsetProperty = new PropertyDescriptor(PropertyType.Float4, "ScaleOffset", propertyDescriptor.GetReferenceName(generation) + "_ST")
                {
                    declaration = PropertyDeclaration.Global
                };
                visitor.AddProperty(scaleOffsetProperty);

                if (generation == GenerationMode.Preview)
                {
                    PortData[scaleOffsetID] = new GeneratedPortData(portDescriptors[scaleOffsetID].Type, "float4(1,1,0,0)");
                }
                else
                {
                    PortData[scaleOffsetID] = new GeneratedPortData(portDescriptors[scaleOffsetID].Type, scaleOffsetProperty.GetReferenceName(GenerationMode.Final));
                }
            }
        }
    }
}