using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Graphlit.Nodes;
using Graphlit.Nodes.PortType;

namespace Graphlit
{
    [NodeInfo("Texture/Sample Texture 2D")]
    public class SampleTexture2DNode : SampleTextureNode
    {
    }
    [NodeInfo("Texture/Sample Texture 2D LOD")]
    public class SampleTexture2DLodNode : SampleTextureNode
    {
        public override bool HasLod => true;
        public override string SampleMethod => $"SAMPLE_TEXTURE2D_LOD({PortData[TEX].Name}, {GetSamplerName(PortData[TEX].Name)}, {PortData[UV].Name}, {PortData[LOD].Name})";
    }
}