using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace ZSG
{
    public class ShaderVaryings
    {
        public ShaderVaryings(ShaderAttributes attributes)
        {
            _attributes = attributes;
        }

        readonly ShaderAttributes _attributes;

        public enum VaryingType
        {
            PositionCS,
            PositionWS,
            NormalWS,
            UV0,
            UV1,
            UV2,
            UV3,
            TangentWS,
            Color,
            Custom
        }

        public struct VaryingDescriptor
        {
            public string name;
            public string semantic;
            public string prefix;
            public VaryingType type;
            public string passthrough;
            public int channels;
        }

        public List<VaryingDescriptor> varyings = new();
        public HashSet<string> customVaryings = new();

        public string RequirePositionCS(int channels = 4)
        {
            _attributes.RequirePositionOS(3);
            return RequireInternal(VaryingType.PositionCS, "positionCS", "SV_POSITION", channels);
        }

        public void RequireCustomString(string varying)
        {
            customVaryings.Add(varying);
        }
        private int _interpCounter = 0;
        public string RequireCustom(int channels)
        {
            var desc = new VaryingDescriptor
            {
                name = "interp" + _interpCounter++,
                semantic = "TEXCOORD",
                type = VaryingType.Custom,
                channels = channels,
                passthrough = null
            };
            varyings.Add(desc);

            return desc.name;
        }

        /*        public string RequirePositionWS(int channels = 4)
                {
                    _attributes.RequirePositionOS(3);
                    return RequireInternal(VaryingType.PositionWS, "positionWS", "TEXCOORD", channels);
                }*/

        public string RequireUV(int texcoord, int channels = 4)
        {
            VaryingType type = VaryingType.UV0;
            switch (texcoord)
            {
                case 0: type = VaryingType.UV0; break;
                case 1: type = VaryingType.UV1; break;
                case 2: type = VaryingType.UV2; break;
                case 3: type = VaryingType.UV3; break;
            }
            return RequireInternal(type, "uv" + texcoord, "TEXCOORD", channels, _attributes.RequireUV(texcoord, channels));
        }

        private string RequireInternal(VaryingType type, string name, string semantic, int channels = 4, string passthrough = null)
        {
            int index = varyings.FindIndex(x => x.type == type);

            if (index < 0)
            {
                var desc = new VaryingDescriptor
                {
                    name = name,
                    semantic = semantic,
                    type = type,
                    channels = channels,
                    passthrough = passthrough
                };
                varyings.Add(desc);
            }
            else
            {
                var attr = varyings[index];
                attr.channels = Mathf.Max(attr.channels, channels);
                varyings[index] = attr;
            }

            return name;
        }

        public void AppendVaryings(ShaderStringBuilder sb)
        {
            int semanticCounter = 0;
            foreach (var vary in varyings)
            {
                var semantic = vary.semantic;
                if (semantic == "TEXCOORD")
                {
                    semantic += semanticCounter++;
                }
                sb.AppendLine($"float{vary.channels} {vary.name} : {semantic};");
            }
            foreach (var var in customVaryings)
            {
                sb.AppendLine(var);
            }
        }

        public void PackVaryings(ShaderStringBuilder sb)
        {
            foreach (var var in varyings)
            {
                if (!string.IsNullOrEmpty(var.passthrough))
                {
                    sb.AppendLine("varyings." + var.name + " = " + var.passthrough + ";");
                }
            }
        }

        private string Mask(string input, int count)
        {
            return input + ".xyzw".Substring(0, count + 1);
        }

        public void UnpackVaryings(ShaderStringBuilder sb)
        {
            foreach (var var in varyings)
            {
                if (!string.IsNullOrEmpty(var.passthrough))
                {
                    string input = Mask("varyings." + var.name, var.channels);
                    sb.AppendLine($"float{var.channels} {var.name} = {input};");
                    //sb.AppendLine("float" + var.name + " = " + var.passthrough + ";");
                }
            }
        }
    }
}