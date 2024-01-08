using System.Collections.Generic;
using UnityEditor.Hardware;

namespace z3y.ShaderGraph
{
    public class PassBuilder
    {
        public PassBuilder(string name, string vertexShaderPath, string fragmentShaderPath, params int[] ports)
        {
            this.name = name;
            this.vertexShaderPath = vertexShaderPath;
            this.fragmentShaderPath = fragmentShaderPath;

            Ports = ports;
        }
        public string name;
        public Dictionary<string, string> tags = new();
        public Dictionary<string, string> renderStates = new();
        public List<string> pragmas = new();
        public List<string> attributes = new();
        public List<string> varyings = new();
        public List<string> cbuffer = new();
        public List<string> objectDecleration = new();
        public HashSet<string> functions = new();
        public List<string> vertexDescription = new();
        public List<string> surfaceDescription = new();
        public HashSet<PropertyDescriptor> properties = new();

        public string vertexShaderPath;
        public string fragmentShaderPath;

        public int[] Ports { get; }

        public void AppendPass(ShaderStringBuilder sb)
        {
            sb.AppendLine("Name \"" + name + "\"");
            ShaderBuilder.AppendTags(sb, tags);

            sb.AppendLine("// Render States");

            sb.AppendLine("HLSLPROGRAM");
            AppendPassHLSL(sb);
            sb.AppendLine("ENDHLSL");
        }
        public void AppendPassHLSL(ShaderStringBuilder sb)
        {
            sb.AppendLine("// Pragmas");

            sb.AppendLine("struct Attributes");
            sb.Indent();
            sb.UnIndent("};");

            sb.AppendLine("struct Varyings");
            sb.Indent();
            sb.UnIndent("};");

            sb.AppendLine("// CBUFFER");
            foreach (var property in properties)
            {
                if (property.Type == PropertyType.Texture2D || property.Type == PropertyType.TextureCube)
                {
                    continue;
                }

                sb.AppendLine(property.Declaration());
            }
            sb.AppendLine("// CBUFFER END");
            sb.AppendLine();

            foreach (var function in functions)
            {
                var lines = function.Split('\n');
                foreach (var line in lines)
                {
                    sb.AppendLine(line);
                }
            }

            AppendVertexDescription(sb);
            AppendSurfaceDescription(sb);

            sb.AppendLine("#include \"" + vertexShaderPath + '"');
            sb.AppendLine("#include \"" + fragmentShaderPath + '"');
        }

        public void AppendSurfaceDescription(ShaderStringBuilder sb)
        {
            sb.AppendLine("SurfaceDescription SurfaceDescriptionFunction(Varyings varyings)");
            sb.Indent();
            foreach (var line in surfaceDescription)
            {
                sb.AppendLine(line);
            }
            sb.UnIndent();
        }

        public void AppendVertexDescription(ShaderStringBuilder sb)
        {
            sb.AppendLine("VertexDescription VertexDescriptionFunction(Attributes attributes)");
            sb.Indent();
            foreach (var line in vertexDescription)
            {
                sb.AppendLine(line);
            }
            sb.UnIndent();
        }
    }
}
