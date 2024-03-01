using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZSG
{
    [Serializable]
    public enum PropertyType
    {
        Float = 1,
        Float2 = 2,
        Float3 = 3,
        Float4 = 4,
        Color = 5,
        Integer = 6,
        Texture2D = 7,
        Texture2DArray = 8,
        TextureCube = 9,
        Texture3D = 10,
        TextureCubeArray = 11,
        Bool = 12,
        KeywordToggle = 13
    }

    [Serializable]
    public enum PropertyDeclaration
    {
        Local = 0,
        Global = 1,
        Instance = 2,
    }

    // thanks pema
    public enum DefaultTextureName
    {
        white,
        black,
        red,
        gray,
        grey,
        linearGray,
        linearGrey,
        grayscaleRamp,
        greyscaleRamp,
        bump,
        blackCube,
        lightmap,
        unity_Lightmap,
        unity_LightmapInd,
        unity_ShadowMask,
        unity_DynamicLightmap,
        unity_DynamicDirectionality,
        unity_DynamicNormal,
        unity_DitherMask,
        _DitherMaskLOD,
        _DitherMaskLOD2D,
        unity_RandomRotation16,
        unity_NHxRoughness,
        unity_SpecCube0,
        unity_SpecCube1,
        none
    }

    [Serializable, Flags]
    public enum MaterialPropertyAttribute
    {
        Gamma = 1 << 0,
        HDR = 1 << 1,
        HideInInspector = 1 << 2,
        MainTexture = 1 << 3,
        MainColor = 1 << 4,
        NoScaleOffset = 1 << 5,
        Normal = 1 << 6,
        PerRendererData = 1 << 7,
        NonModifiableTextureData = 1 << 8,
        SingleLineTexture = 1 << 9,
        IntRange = 1 << 10,
        Linear = 1 << 11,
    }

    [Serializable]
    public class PropertyDescriptor
    {
        [SerializeField] public string guid;
        [SerializeField] public string referenceName;
        [SerializeField] public string displayName;
        [SerializeField] public PropertyType type;
        [SerializeField] public string customAttributes;
        [SerializeField] public MaterialPropertyAttribute defaultAttributes;
        [SerializeField] public float rangeX;
        [SerializeField] public float rangeY;
        [SerializeField] string _value;
        [SerializeField] string _defaultTexture;
        [SerializeField] public PropertyDeclaration declaration = PropertyDeclaration.Local;

        [NonSerialized] public bool useReferenceName = false;
        public float FloatValue
        {
            get
            {
                float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value);
                return value;
            }
            set
            {
                _value = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        public Vector4 VectorValue
        {
            get
            {
                if (string.IsNullOrEmpty(_value))
                {
                    return Vector4.zero;
                }

                string withoutParens = _value.Replace(")", "").Replace("(", "");
                string[] split = withoutParens.Split(',');
                float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
                float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
                float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z);
                float.TryParse(split[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float w);
                return new Vector4(x, y, z, w);
            }
            set
            {
                _value = value.ToString();
            }
        }
        public Texture DefaultTextureValue
        {
            get
            {
                if (string.IsNullOrEmpty(_defaultTexture))
                {
                    return null;
                }
                return Helpers.SerializableReferenceToObject<Texture>(_defaultTexture);
            }
            set
            {
                _defaultTexture = Helpers.AssetSerializableReference(value);
            }
        }
        public Vector2 Range
        {
            get
            {
                return new(rangeX, rangeY);
            }
            set
            {
                rangeX = value.x;
                rangeY = value.y;
            }
        }
        public DefaultTextureName DefaultTextureEnum
        {
            get
            {
                Enum.TryParse(_value, out DefaultTextureName value);
                return value;
            }
            set
            {
                _value = value.ToString();
            }
        }

        public string KeywordName
        {
            get
            {
                string[] split = referenceName.Split(' ');
                if (split.Length < 2)
                {
                    Debug.LogError("Wrong keyword declaration");
                    return "_KEYWORD";
                }
                return split[1];
            }
        }

        public bool IsTextureType => type == PropertyType.Texture2D || type == PropertyType.Texture2DArray || type == PropertyType.TextureCube || type == PropertyType.TextureCubeArray || type == PropertyType.Texture3D;
        public bool SupportsGPUInstancing => type == PropertyType.Float || type == PropertyType.Float2 || type == PropertyType.Float4 || type == PropertyType.Float3 || type == PropertyType.Integer || type == PropertyType.Color || type == PropertyType.Bool;
        public bool HasRange => rangeX != rangeY;

        public PropertyDescriptor(PropertyType type, string displayName = null, string referenceName = "")
        {
            guid = Guid.NewGuid().ToString();
            this.type = type;
            this.displayName = string.IsNullOrEmpty(displayName) ? type.ToString() : displayName.Trim();
            this.referenceName = referenceName.Trim();
            if (type == PropertyType.Color)
            {
                VectorValue = Vector4.one;
            }
            if (type == PropertyType.KeywordToggle)
            {
                this.referenceName = "shader_feature_local _KEYWORD";
            }
        }

        public string GetDefaultValue()
        {
            if (IsTextureType)
            {
                if (DefaultTextureEnum == DefaultTextureName.none)
                {
                    return "\"\" {}";
                }
                return '"' + DefaultTextureEnum.ToString() + '"' + " {}";
            }
            return type switch
            {
                PropertyType.Float => FloatValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                PropertyType.Float2 => VectorValue.ToString(),
                PropertyType.Float3 => VectorValue.ToString(),
                PropertyType.Float4 => VectorValue.ToString(),
                PropertyType.Color => VectorValue.ToString(),
                PropertyType.Integer => FloatValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                PropertyType.Bool => FloatValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                PropertyType.KeywordToggle => FloatValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => throw new System.NotImplementedException(),
            };
        }

        public string TypeToString()
        {
            if (type == PropertyType.Float && HasRange)
            {
                var range = Range;
                return $"Range ({range.x.ToString("R", CultureInfo.InvariantCulture)}, {range.y.ToString("R", CultureInfo.InvariantCulture)})";
            }

            return type switch
            {
                PropertyType.Float => "Float",
                PropertyType.Float2 => "Vector",
                PropertyType.Float3 => "Vector",
                PropertyType.Float4 => "Vector",
                PropertyType.Color => "Color",
                PropertyType.Integer => "Integer",
                PropertyType.Texture2D => "2D",
                PropertyType.Texture3D => "3D",
                PropertyType.TextureCube => "Cube",
                PropertyType.Texture2DArray => "2DArray",
                PropertyType.TextureCubeArray => "CubeArray",
                PropertyType.Bool => "Float",
                PropertyType.KeywordToggle => "Float",
                _ => throw new System.NotImplementedException()
            };
        }

        public string GetFieldDeclaration(GenerationMode generationMode)
        {
            var referenceName = GetReferenceName(generationMode);

            if (type == PropertyType.KeywordToggle && generationMode == GenerationMode.Preview)
            {
                if (FloatValue > 0)
                {
                    return "#define " + KeywordName;
                }
                return string.Empty;
            }

            return type switch
            {
                PropertyType.Float => $"float {referenceName};",
                PropertyType.Float2 => $"float2 {referenceName};",
                PropertyType.Float3 => $"float3 {referenceName};",
                PropertyType.Float4 => $"float4 {referenceName};",
                PropertyType.Color => $"float4 {referenceName};",
                PropertyType.Integer => $"int {referenceName};",
                PropertyType.Bool => $"float {referenceName};",
                PropertyType.KeywordToggle => $"#pragma {referenceName}",
                PropertyType.Texture2D => $"TEXTURE2D({referenceName}); SAMPLER(sampler{referenceName});",
                PropertyType.TextureCube => $"TEXTURECUBE({referenceName}); SAMPLER(sampler{referenceName});",
                PropertyType.TextureCubeArray => $"TEXTURECUBE_ARRAY({referenceName}); SAMPLER(sampler{referenceName});",
                PropertyType.Texture2DArray => $"TEXTURE2D_ARRAY({referenceName}); SAMPLER(sampler{referenceName});",
                PropertyType.Texture3D => $"TEXTURE3D({referenceName}); SAMPLER(sampler{referenceName});",
                _ => throw new System.NotImplementedException()
            };
        }


        public string AttributesToString()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(customAttributes))
            {
                sb.Append(customAttributes.Replace("\n", ""));
            }


            foreach (MaterialPropertyAttribute attribute in Enum.GetValues(typeof(MaterialPropertyAttribute)))
            {
                if (defaultAttributes.HasFlag(attribute))
                {
                    sb.Append('[');
                    sb.Append(Enum.GetName(typeof(MaterialPropertyAttribute), attribute));
                    sb.Append(']');
                }
            }

            switch (type)
            {
                case PropertyType.Bool:
                    sb.Append("[ToggleUI]");
                    break;
                case PropertyType.KeywordToggle:
                    sb.Append($"[Toggle({KeywordName})]");
                    break;
                case PropertyType.Float2:
                    sb.Append($"[Vector2]");
                    break;
                case PropertyType.Float3:
                    sb.Append($"[Vector3]");
                    break;
            }

            return sb.ToString();
        }

        public string GetReferenceName(GenerationMode generationMode)
        {
            if (useReferenceName)
            {
                generationMode = GenerationMode.Final;
            }

            if (generationMode == GenerationMode.Preview)
            {
                return "_" + guid.RemoveWhitespace().Replace("-", "_");
            }
            if (!string.IsNullOrEmpty(referenceName))
            {
                return referenceName;
            }

            return "_" + guid.RemoveWhitespace().Replace("-", "_");
        }
        public bool ShouldDeclare() => declaration == PropertyDeclaration.Local;
        public string GetPropertyDeclaration(GenerationMode generationMode)
        {
            var referenceName = GetReferenceName(generationMode);
            var type = TypeToString();
            var attributes = AttributesToString();
            var defaultValue = GetDefaultValue();

            if (this.type == PropertyType.KeywordToggle)
            {
                referenceName = "_Toggle" + KeywordName;
            }

            return $"{attributes} {referenceName} (\"{displayName}\", {type}) = {defaultValue}";
        }

        void OnDefaultGUI()
        {
            EditorGUI.BeginChangeCheck();
            displayName = EditorGUILayout.TextField(new GUIContent("Display Name"), displayName);
            referenceName = EditorGUILayout.TextField(new GUIContent("Reference Name", guid), referenceName);
            if (type != PropertyType.Texture2D && type != PropertyType.TextureCube)
            {
                declaration = (PropertyDeclaration)EditorGUILayout.EnumPopup("Declaration", declaration);
            }
            if (EditorGUI.EndChangeCheck())
            {
                onValueChange?.Invoke();
            }

            defaultAttributes = (MaterialPropertyAttribute)EditorGUILayout.EnumFlagsField("Attributes", defaultAttributes);


            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Custom Attributes", GUILayout.Width(149));
            customAttributes = EditorGUILayout.TextArea(customAttributes);
            GUILayout.EndHorizontal();
        }

        void OnGUIFloat()
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Range", GUILayout.Width(149));
            Range = EditorGUILayout.Vector2Field("", Range);
            GUILayout.EndHorizontal();
            float newValue;
            if (HasRange)
            {
                newValue = EditorGUILayout.Slider("Value", FloatValue, rangeX, rangeY);
            }
            else
            {
                newValue = EditorGUILayout.FloatField("Value", FloatValue);
            }
            if (EditorGUI.EndChangeCheck())
            {
                FloatValue = newValue;
                UpdatePreviewMaterial();
            }
        }
        void OnGUIBool()
        {
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Toggle("Toggle", FloatValue == 1);
            if (EditorGUI.EndChangeCheck())
            {
                FloatValue = newValue ? 1 : 0;
                UpdatePreviewMaterial();
            }
        }
        void OnGUIVector()
        {
            EditorGUI.BeginChangeCheck();
            Vector4 newValue;
            newValue = type switch
            {
                PropertyType.Float2 => EditorGUILayout.Vector2Field("", VectorValue),
                PropertyType.Float3 => EditorGUILayout.Vector3Field("", VectorValue),
                PropertyType.Float4 => EditorGUILayout.Vector4Field("", VectorValue),
                _ => throw new NotImplementedException(),
            };
            if (EditorGUI.EndChangeCheck())
            {
                VectorValue = newValue;
                UpdatePreviewMaterial();
            }
        }

        Type TextureType(PropertyType type)
        {
            return type switch
            {
                PropertyType.Texture2D => typeof(Texture2D),
                PropertyType.Texture2DArray => typeof(Texture2DArray),
                PropertyType.TextureCube => typeof(Cubemap),
                PropertyType.Texture3D => typeof(Texture3D),
                PropertyType.TextureCubeArray => typeof(CubemapArray),
                _ => throw new NotImplementedException(),
            };
        }

        void OnGUITexture()
        {
            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Default Texture", GUILayout.Width(149));
            Texture newValue = (Texture)EditorGUILayout.ObjectField(DefaultTextureValue, TextureType(type), false);
            GUILayout.EndHorizontal();

            var defaultTex = EditorGUILayout.EnumPopup("Default Value", DefaultTextureEnum);

            if (EditorGUI.EndChangeCheck())
            {
                DefaultTextureValue = newValue;
                DefaultTextureEnum = (DefaultTextureName)defaultTex;
                UpdatePreviewMaterial();
            }
        }

        void OnGUIColor()
        {
            EditorGUI.BeginChangeCheck();
            Color newValue = EditorGUILayout.ColorField("Color", VectorValue);

            if (EditorGUI.EndChangeCheck())
            {

                VectorValue = newValue;
                UpdatePreviewMaterial();
            }
        }

        public void UpdatePreviewMaterial()
        {
            Material m = graphView.PreviewMaterial;
            string name = GetReferenceName(GenerationMode.Preview);
            if (type == PropertyType.Float || type == PropertyType.Bool) m.SetFloat(name, FloatValue);
            else if (type == PropertyType.Float2 || type == PropertyType.Float3 || type == PropertyType.Float4) m.SetVector(name, VectorValue);
            else if (type == PropertyType.Color) m.SetColor(name, VectorValue);
            else if (IsTextureType) m.SetTexture(name, DefaultTextureValue);

            if (type == PropertyType.KeywordToggle)
            {
                foreach (var element in graphView.graphElements)
                {
                    if (element is KeywordPropertyNode keywordProperyNode && keywordProperyNode.propertyDescriptor == this)
                    {
                        keywordProperyNode.GeneratePreviewForAffectedNodes();
                    }
                }
            }
        }

        [NonSerialized] public ShaderGraphView graphView;

        public VisualElement PropertyEditorGUI()
        {
            var imgui = new IMGUIContainer(OnDefaultGUI); // too much data to bind, easier to just use imgui
            if (type == PropertyType.Float) imgui.onGUIHandler += OnGUIFloat;
            else if (type == PropertyType.Float2 || type == PropertyType.Float3 || type == PropertyType.Float4) imgui.onGUIHandler += OnGUIVector;
            else if (type == PropertyType.Color) imgui.onGUIHandler += OnGUIColor;
            else if (type == PropertyType.Bool || type == PropertyType.KeywordToggle) imgui.onGUIHandler += OnGUIBool;
            else if (IsTextureType) imgui.onGUIHandler += OnGUITexture;

            var s = imgui.style;
            s.marginLeft = 6;
            s.marginRight = 6;
            s.marginTop = 6;
            s.marginBottom = 6;


            return imgui;
        }

        public Type GetNodeType()
        {
            return type switch
            {
                PropertyType.Float => typeof(FloatPropertyNode),
                PropertyType.Float2 => typeof(Float2PropertyNode),
                PropertyType.Float3 => typeof(Float3PropertyNode),
                PropertyType.Float4 => typeof(Float4PropertyNode),
                PropertyType.Color => typeof(ColorPropertyNode),
                PropertyType.Integer => typeof(IntegerPropertyNode),
                PropertyType.Bool => typeof(BooleanPropertyNode),
                PropertyType.Texture2D => typeof(Texture2DPropertyNode),
                PropertyType.KeywordToggle => typeof(KeywordPropertyNode),
                PropertyType.Texture2DArray => typeof(Texture2DArrayPropertyNode),
                PropertyType.Texture3D => typeof(Texture3DPropertyNode),
                PropertyType.TextureCube => typeof(TextureCubePropertyNode),
                PropertyType.TextureCubeArray => typeof(TextureCubeArrayPropertyNode),
                _ => throw new NotImplementedException(),
            };
        }

        public Action onValueChange = delegate { };
    }

}