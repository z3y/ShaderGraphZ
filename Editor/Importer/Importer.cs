using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using UnityEditor.Callbacks;
using System.Linq;

namespace ZSG
{
    [ScriptedImporter(2, EXTENSION, 0)]
    public class ShaderGraphImporter : ScriptedImporter
    {
        public const string EXTENSION = "zsg";
        internal static Dictionary<string, ShaderGraphView> _graphViews = new();
        internal static string _lastImport;

        public static SerializableGraph ReadGraphData(string guid)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var text = File.ReadAllText(assetPath);
            var data = new SerializableGraph();
            if (!string.IsNullOrEmpty(text))
            {
                JsonUtility.FromJsonOverwrite(text, data);
            }
            return data;
        }


        public override void OnImportAsset(AssetImportContext ctx)
        {
            var target = ctx.selectedBuildTarget;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (_graphViews.TryGetValue(guid, out var graphView))
            {
            }
            else if (graphView is null)
            {
                var data = ReadGraphData(guid);
                graphView = new ShaderGraphView(null);
                data.PopulateGraph(graphView);
            }

            var builder = new ShaderBuilder(GenerationMode.Final, graphView, target);
            if (string.IsNullOrEmpty(builder.shaderName) || builder.shaderName == "Default Shader")
            {
                builder.shaderName = Path.GetFileNameWithoutExtension(ctx.assetPath);
            }
            builder.BuildTemplate();

            var scriptingDefines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone);
            foreach (var pass in builder.passBuilders)
            {
                pass.pragmas.Add("#define TARGET_" + target.ToString().ToUpper());

                if (!string.IsNullOrEmpty(scriptingDefines))
                {
                    foreach (var define in scriptingDefines.Split(';'))
                    {
                        pass.pragmas.Add("#define SCRIPTING_DEFINE_" + define);
                    }
                }

            }


            var result = builder.ToString();
            _lastImport = result;
            var shader = ShaderUtil.CreateShaderAsset(ctx, result, false);

            if (builder._nonModifiableTextures.Count > 0)
            {
                EditorMaterialUtility.SetShaderNonModifiableDefaults(shader, builder._nonModifiableTextures.Keys.ToArray(), builder._nonModifiableTextures.Values.ToArray());
            }
            if (builder._defaultTextures.Count > 0)
            {
                EditorMaterialUtility.SetShaderNonModifiableDefaults(shader, builder._defaultTextures.Keys.ToArray(), builder._defaultTextures.Values.ToArray());
            }

            foreach (var dependency in builder.dependencies)
            {
                ctx.DependsOnSourceAsset(dependency);
            }

            ctx.AddObjectToAsset("Main Asset", shader);


            /*var material = new Material(shader)
            {
                name = "Material"
            };*/
            //ctx.AddObjectToAsset("Material", material);

            //ctx.AddObjectToAsset("generation", new TextAsset(result));

            //var text = File.ReadAllText(assetPath);
            //ctx.AddObjectToAsset("json", new TextAsset(text));
            DefaultInspector.Reinitialize();
        }

        public static void CreateEmptyTemplate(TemplateOutput template, Action<ShaderGraphView> onCreate = null)
        {
            var graphView = new ShaderGraphView(null)
            {
                graphData = new GraphData()
            };
            graphView.CreateNode(template, Vector2.zero, false);
            var firstPort = template.Inputs.Where(x => x.GetPortID() == template.FragmentPorts[0]).First();

            var tex2d = new SampleTexture2DNode();
            graphView.CreateNode(tex2d, new Vector2(-100, 0), false);
            tex2d.Outputs.First().ConnectTo(firstPort);

            var prop = new Texture2DPropertyNode();
            graphView.CreateNode(prop, new Vector2(-200, 0), false);
            var desc = prop.propertyDescriptor;
            desc.displayName = "Main Texture";
            desc.referenceName = "_MainTex";
            prop.Outputs.First().ConnectTo(tex2d.Inputs.First());

            onCreate?.Invoke(graphView);

            // for some reason position is not applied
            var data = SerializableGraph.StoreGraph(graphView);
            for (int i = 0; i < data.nodes.Count(); i++)
            {
                if (data.nodes[i].guid == prop.viewDataKey)
                {
                    var a = data.nodes[i];
                    a.x = -500;
                    data.nodes[i] = a;
                }
                else if (data.nodes[i].guid == tex2d.viewDataKey)
                {
                    var a = data.nodes[i];
                    a.x = -300;
                    data.nodes[i] = a;
                }
            }

            var jsonData = JsonUtility.ToJson(data, true);
            ProjectWindowUtil.CreateAssetWithContent($"New Shader Graph.{EXTENSION}", jsonData);
        }
        public static void CreateEmptyTemplate<T>() where T : TemplateOutput, new()
        {
            var instance = new T();
            CreateEmptyTemplate(instance);
        }

        public static void OpenInGraphView(string guid)
        {
            if (ShaderGraphWindow.editorInstances.TryGetValue(guid, out var win))
            {
                if (!win.disabled)
                {
                    win.Focus();
                    return;
                }

                else
                {
                    ShaderGraphWindow.editorInstances.Remove(guid);
                    win.Close();
                }
            }
            win = EditorWindow.CreateWindow<ShaderGraphWindow>(typeof(ShaderGraphWindow), typeof(ShaderGraphWindow));
            win.Initialize(guid);

            _graphViews[guid] = win.graphView;
            ShaderBuilder.GenerateAllPreviews(win.graphView);
        }

        public static void SaveGraphAndReimport(ShaderGraphView graphView, string guid)
        {
            try
            {
                var importerPath = AssetDatabase.GUIDToAssetPath(guid);
                var data = SerializableGraph.StoreGraph(graphView);
                var jsonData = JsonUtility.ToJson(data, true);

                _graphViews[importerPath] = graphView;

                File.WriteAllText(importerPath, jsonData);
                AssetDatabase.ImportAsset(importerPath, ImportAssetOptions.ForceUpdate);

                graphView.MarkDirtyRepaint();
            }
            catch (Exception ex)
            {
                Debug.LogError("Serialization failed " + ex);
            }
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var unityObject = EditorUtility.InstanceIDToObject(instanceID);
            var path = AssetDatabase.GetAssetPath(unityObject);
            var importer = AssetImporter.GetAtPath(path);
            if (importer is not ShaderGraphImporter shaderGraphImporter)
            {
                return false;
            }

            var guid = AssetDatabase.GUIDFromAssetPath(shaderGraphImporter.assetPath);
            OpenInGraphView(guid.ToString());
            return true;
        }
    }
}