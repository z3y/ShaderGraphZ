using System;
using UnityEngine;

namespace z3y.ShaderGraph.Nodes
{
    public struct PortType
    {
        public struct DynamicFloat
        {
            public int components;
            public bool fullPrecision;
            public DynamicFloat(int components, bool fullPrecision = true)
            {
                if (components < 1 || components > 4)
                {
                    Debug.LogError("Invalid component count");
                }
                this.components = components;
                this.fullPrecision = fullPrecision;
            }

            public override readonly string ToString()
            {
                if (fullPrecision)
                {
                    return components switch
                    {
                        1 => "float",
                        2 => "float2",
                        3 => "float3",
                        4 => "float4",
                        _ => null,
                    };
                }
                else
                {
                    return components switch
                    {
                        1 => "half",
                        2 => "half2",
                        3 => "half3",
                        4 => "half4",
                        _ => null,
                    };
                }
            }
        };

        public enum Texture2D { }
        public enum SamplerState { }

        public static Color Float1Color = Color.grey;
        public static Color Float2Color = new Color(232 / 255.0f, 255 / 255.0f, 183 / 255.0f); // yellow
        public static Color Float3Color = new Color(196 / 255.0f, 245 / 255.0f, 252 / 255.0f); // cyan
        public static Color Float4Color = new Color(226 / 255.0f, 160 / 255.0f, 255 / 255.0f); // magenta
        public static Color GetComponentColor(int component)
        {
            return component switch
            {
                1 => Float1Color,
                2 => Float2Color,
                3 => Float3Color,
                4 => Float4Color,
                _ => Color.white,
            };
        }

        public static Color GetPortColor(Type type)
        {
            if (type == typeof(DynamicFloat))
            {
                return Float1Color;
            }
            else if (type == typeof(Texture2D))
            {
                return Color.cyan;
            }
            else if (type == typeof(SamplerState))
            {
                return Color.gray;
            }


            return Color.white;
        }
    }

    public enum PropertyType
    {
        Float,
        Float2,
        Float3,
        Float4,
        Color,
        Intiger,
        Texture2D,
        TextureAny,
        TextureCube,
        // etc
    }
}