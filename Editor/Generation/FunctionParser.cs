using System;
using System.Collections.Generic;
using UnityEngine;
using ZSG.Nodes.PortType;

namespace ZSG
{
    public class FunctionParser
    {
        public static IPortType StringToPortType(string type)
        {
            return type switch
            {
                "float" or "half" => new Float(1),
                "float2" or "half2" => new Float(2),
                "float3" or "half3" => new Float(3),
                "float4" or "half4" => new Float(4),
                "Texture2D" => new Texture2DObject(),
                "SamplerState" => new SamplerState(),
                _ => throw new NotImplementedException(),
            };
        }

        public List<PortDescriptor> descriptors = new List<PortDescriptor>();
        public string methodName;
        public Dictionary<int, PortBinding> bindings = new Dictionary<int, PortBinding>();
        public bool TryParse(string code)
        {
            descriptors.Clear();
            bindings.Clear();
            try
            {
                string[] split1 = code.Split('(');
                methodName = split1[0]["void ".Length..];

                string allargs = split1[1].Split(')')[0];
                bool emptyArts = string.IsNullOrEmpty(allargs);
                if (emptyArts) return false;

                string[] args = allargs.Split(',');
                for (int i = 0; i < args.Length; i++)
                {
                    PortDirection direction = PortDirection.Input;
                    string[] arg = args[i].Trim().Split(' ');

                    int typeArgIndex = 0;
                    if (arg[0] == "out")
                    {
                        direction = PortDirection.Output;
                        typeArgIndex++;
                    }
                    string type = arg[typeArgIndex].Trim();
                    string name = arg[typeArgIndex + 1].Trim();

                    int id = i;
                    if (direction == PortDirection.Output) id += 100;
                    descriptors.Add(new(direction, StringToPortType(type), id, name));
                    if (Enum.TryParse(name, true, out PortBinding binding))
                    {
                        bindings[id] = binding;
                    }

                    //Debug.Log($"PortDirection = '{direction}', Type = '{type}', PortName = '{name}'");
                }

                return true;
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }

            return false;

        }
    }
}