using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ZSG.Nodes.PortType;

namespace ZSG
{
    public abstract class TemplateOutput : ShaderNode
    {
        public abstract string Name { get; }
        public virtual void OnBeforeBuild(ShaderBuilder builder) { }
        public virtual void OnAfterBuild(ShaderBuilder builder) { }
        public abstract int[] VertexPorts { get; }
        public abstract int[] FragmentPorts { get; }

        public override Color Accent => Color.magenta;

        public override void AdditionalElements(VisualElement root)
        {
            var graphData = GraphView.graphData;
            var shaderName = new TextField("Shader Name") { value = graphData.shaderName };
            shaderName.RegisterValueChangedCallback((evt) =>
            {
                graphData.shaderName = evt.newValue;
                GraphView.SetDirty();
            });
            root.Add(shaderName);

            var customEditor = new TextField("Custom Editor") { value = graphData.customEditor };
            customEditor.RegisterValueChangedCallback(x => graphData.customEditor = x.newValue);
            root.Add(customEditor);

            var fallback = new TextField("Fallback") { value = graphData.fallback };
            fallback.RegisterValueChangedCallback(x => graphData.fallback = x.newValue);
            root.Add(fallback);

            var graphPrecisionSelection = new EnumField("Graph Precision", graphData.precision);
            graphPrecisionSelection.RegisterValueChangedCallback(x => graphData.precision = (GraphData.GraphPrecision)x.newValue);
            root.Add(graphPrecisionSelection);

            var defaultPreviewState = new EnumField("Default Preview", graphData.defaultPreviewState);
            defaultPreviewState.RegisterValueChangedCallback(x => graphData.defaultPreviewState = (GraphData.DefaultPreviewState)x.newValue);
            root.Add(defaultPreviewState);

            var include = new TextField("Include") { value = graphData.include, multiline = true };
            include.RegisterValueChangedCallback(x => graphData.include = x.newValue);
            root.Add(include);

            var outline = new EnumField("Outline Pass", graphData.outlinePass);
            outline.RegisterValueChangedCallback(x => graphData.outlinePass = (GraphData.OutlinePassMode)x.newValue);
            root.Add(outline);

            var stencil = new Toggle("Stencil") { value = graphData.stencil };
            stencil.RegisterValueChangedCallback(x => graphData.stencil = x.newValue);
            root.Add(stencil);

            AddVRCTagsElements(root, graphData);
            var propertyEditor = new VisualElement();

            var properties = new ListView()
            {
                headerTitle = "Properties",
                showAddRemoveFooter = true,
                reorderMode = ListViewReorderMode.Simple,
                showFoldoutHeader = true,
                reorderable = true,
                itemsSource = graphData.properties
            };
            properties.itemsSource = graphData.properties;
            properties.bindItem = (e, i) =>
            {
                var nameField = e.Q<TextField>("Name");
                var typeLabel = e.Q<Label>("Type");
                if (graphData.properties[i] is null)
                {
                    graphData.properties[i] = new PropertyDescriptor(PropertyType.Float);
                }
                nameField.value = graphData.properties[i].displayName;
                nameField.RegisterValueChangedCallback((evt) =>
                {
                    graphData.properties[i].displayName = evt.newValue;
                    graphData.properties[i].onValueChange?.Invoke();
                });

                typeLabel.text = graphData.properties[i].type.ToString();
                graphData.properties[i].graphView = GraphView;
            };
            properties.makeItem = () =>
            {
                var root = new VisualElement();
                root.style.flexDirection = FlexDirection.Row;
                var nameField = new TextField() { name = "Name"};
                nameField.style.width = 250;
                root.Add(nameField);
                root.Add(new Label("asd") { name = "Type" });
                return root;
            };
            var addButton = properties.Q<Button>("unity-list-view__add-button");
            addButton.clickable = new Clickable((evt) =>
            {
                void OnTypeSelected(object data)
                {
                    var type = (PropertyType)data;
                    GraphView.graphData.properties.Add(new PropertyDescriptor(type));
                    properties.RefreshItems();
                }

                var menu = new GenericMenu();
                foreach (PropertyType value in Enum.GetValues(typeof(PropertyType)))
                {
                    menu.AddItem(new GUIContent(Enum.GetName(typeof(PropertyType), value)), false, OnTypeSelected, value);
                }

                menu.ShowAsContext();
            });

            var removeButton = properties.Q<Button>("unity-list-view__remove-button");
            removeButton.clickable.clicked += OnRemovePropertyItem;

            root.Add(properties);
            root.Add(propertyEditor);

            properties.selectionChanged += objects =>
            {
                propertyEditor.Clear();
                foreach (PropertyDescriptor obj in objects)
                {
                    propertyEditor.Add(obj.PropertyEditorGUI());
                }
            };

        }
        void AddVRCTagsElements(VisualElement root, GraphData graphData)
        {
            var foldout = new Foldout
            {
                text = "VRChat Fallback",
                value = false
            };
            root.Add(foldout);

            var mode = new EnumField("Mode", graphData.vrcFallbackTags.mode);
            mode.RegisterValueChangedCallback(x => graphData.vrcFallbackTags.mode = (VRCFallbackTags.ShaderMode)x.newValue);
            foldout.Add(mode);

            var type = new EnumField("Type", graphData.vrcFallbackTags.type);
            type.RegisterValueChangedCallback(x => graphData.vrcFallbackTags.type = (VRCFallbackTags.ShaderType)x.newValue);
            foldout.Add(type);

            var doubleSided = new Toggle("Double-Sided") { value = graphData.vrcFallbackTags.doubleSided };
            doubleSided.RegisterValueChangedCallback(x => graphData.vrcFallbackTags.doubleSided = x.newValue);
            foldout.Add(doubleSided);
        }

        void OnRemovePropertyItem()
        {
            var propertyNodes = GraphView.graphElements.OfType<PropertyNode>();
            var properties = GraphView.graphData.properties;
            foreach (var node in propertyNodes)
            {
                if (properties.Any(x => x.guid == node.propertyDescriptor.guid))
                {
                    continue;
                }

                // remove for now, convert later when avaliable
                foreach (var port in node.PortElements)
                {
                    node.Disconnect(port);
                }
                GraphView.RemoveElement(node);
            }
        }

        public void VisitTemplate(NodeVisitor visitor, int[] ports)
        {
            var structField = visitor.Stage == ShaderStage.Fragment ?
                 visitor._shaderBuilder.passBuilders[visitor.Pass].surfaceDescriptionStruct
                 : visitor._shaderBuilder.passBuilders[visitor.Pass].vertexDescriptionStruct;

            foreach (var input in Inputs)
            {
                int currentID = input.GetPortID();

                if (!ports.Contains(currentID))
                {
                    continue;
                }

                var portDesc = portDescriptors[currentID];

                if (portDesc.Type is Float @float)
                {
                    var inputData = PortData[currentID];
                    visitor.AppendLine($"output.{portDesc.Name} = {inputData.Name};");

                    structField.Add($"{@float} {portDesc.Name};");
                }
            }
        }
        public override bool DisablePreview => true;

        protected sealed override void Generate(NodeVisitor visitor) { }
    }
}