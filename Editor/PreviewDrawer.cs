using System.ComponentModel;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Graphlit
{
    public enum PreviewType
    {
        Inherit = 0,
        Preview2D = 1,
        Preview3D = 2,
    }

    public class PreviewDrawer : ImmediateModeElement
    {
        int _resolution = 96;
        public string shaderLabString;
        public Shader cachedShader;
        Material _material;
        ShaderGraphView _graphView;
        static Shader _defaultShader = Shader.Find("Unlit/Color");
        private bool _disabled = false;
        Shader PreviewShader
        {
            get
            {
                if (cachedShader == null)
                {
                    CompileShader();
                }
                return cachedShader;
            }
        }

        void CompileShader()
        {
            if (string.IsNullOrEmpty(shaderLabString))
            {
                cachedShader = _defaultShader;
                return;
            }
            if (cachedShader != null)
            {
                ShaderUtil.UpdateShaderAsset(cachedShader, shaderLabString, false);
            }
            else
            {
                cachedShader = ShaderUtil.CreateShaderAsset(shaderLabString, false);
            }
        }

        PreviewDrawer _extensionPreviewDrawer;

        public PreviewDrawer GetExtensionPreview()
        {
            _extensionPreviewDrawer ??= new PreviewDrawer(_graphView, 389)
            {
                shaderLabString = shaderLabString,
                cachedShader = cachedShader
            };

            return _extensionPreviewDrawer;
        }

        public PreviewDrawer(ShaderGraphView graphView, int resolution = 96)
        {
            _graphView = graphView;
            _material = graphView.PreviewMaterial;
            _resolution = resolution;
            style.width = _resolution;
            style.height = _resolution;
            //cullingEnabled = true;

            name = "PreviewDrawer";
        }

        public void SetShader(string shader)
        {
            Dispose();
            shaderLabString = shader;
            CompileShader();
            if (_extensionPreviewDrawer is not null)
            {
                _extensionPreviewDrawer.shaderLabString = shaderLabString;
                _extensionPreviewDrawer.cachedShader = cachedShader;
            }
            MarkDirtyRepaint();
        }

        public void Disable()
        {
            _disabled = true;
            style.height = 0;
            style.width = 0;
            Dispose();
        }

        public void Enable()
        {
            _disabled = false;
            style.width = _resolution;
            style.height = _resolution;
        }

        public void Dispose()
        {
            if (cachedShader)
            {
                GameObject.DestroyImmediate(cachedShader);
            }
            shaderLabString = string.Empty;
        }
        int _graphTimeId = Shader.PropertyToID("_GraphTime");
        protected override void ImmediateRepaint()
        {
            if (_disabled)
            {
                return;
            }

            if (!_material)
            {
                _material = _graphView.PreviewMaterial;
                return;
            }

            if (!string.IsNullOrEmpty(shaderLabString))
            {
                _material.shader = PreviewShader;
            }

            if (_material.shader == null)
            {
                return;
            }

            float t = Time.realtimeSinceStartup;

            Vector4 time = new(t / 20.0f, t, t * 2.0f, t * 3.0f);
            //Vector4 timeParameters = new Vector4(t, Mathf.Sin(t), Mathf.Cos(t), 0.0f);

            _material.SetVector(_graphTimeId, time);
            Graphics.DrawTexture(contentRect, Texture2D.whiteTexture, _material, 0);

            Repaint();
        }

        async void Repaint()
        {
            await Task.Delay(16);
            MarkDirtyRepaint();
        }
    }
}