using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using z3y.ShaderGraph.Nodes;

namespace z3y.ShaderGraph
{
    public enum PropertyType
    {
        Float,
        Float2,
        Float3,
        Float4,
        Range,
        Color,
        Intiger,
        Texture2D,
        TextureCube,
    }

    public class PropertyDescriptor
    {
        public PropertyDescriptor(PropertyType type, string displayName, string name = null, string defaultValue = "0.0", List<string> attributes = null)
        {
            Type = type;
            DisplayName = displayName;
            DefaultValue = defaultValue;
            Attributes = attributes;
            if (name is null)
            {
                Name = "_" + displayName.RemoveWhitespace();
            }
            else
            {
                Name = name;
            }
        }

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string DefaultValue { get; set; }
        public PropertyType Type { get; set; }
        public List<string> Attributes { get; set; }
        public Vector2 Range { get; set; }

        public void SetPreviewName(SerializableNode serializableNode)
        {
            Name = "_" + serializableNode.guid;
        }

        public string TypeToString()
        {
            return Type switch
            {
                PropertyType.Float => "Float",
                PropertyType.Float2 => "Vector4",
                PropertyType.Float3 => "Vector4",
                PropertyType.Float4 => "Vector4",
                PropertyType.Range => $"Range ({Range.x.ToString("R")}, {Range.y.ToString("R")})",
                PropertyType.Color => "Color",
                PropertyType.Intiger => "Intiger",
                PropertyType.Texture2D => "2D",
                PropertyType.TextureCube => "Cube",
                _ => throw new System.NotImplementedException()
            };
        }

        public string Declaration()
        {
            return Type switch
            {
                PropertyType.Float => $"float {Name};",
                PropertyType.Float2 => $"float2 {Name};",
                PropertyType.Float3 => $"float3 {Name};",
                PropertyType.Float4 => $"float4 {Name};",
                PropertyType.Range => $"float {Name};",
                PropertyType.Color => $"float4 {Name};",
                PropertyType.Intiger => $"int {Name};",
                PropertyType.Texture2D => $"Texture2D {Name}; SamplerState sampler{Name};",
                PropertyType.TextureCube => $"TextureCube {Name}; SamplerState sampler{Name};",
                _ => throw new System.NotImplementedException()
            };
        }

        public string AttributesToString()
        {
            if (Attributes is null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var attribute in Attributes)
            {
                sb.Append("[");
                sb.Append(attribute.ToString());
                sb.Append("] ");
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            var name = Name;
            var type = TypeToString();
            var attributes = AttributesToString();
            return $"{attributes} {name} (\"{DisplayName}\", {type}) = {DefaultValue}";
        }
    }
}