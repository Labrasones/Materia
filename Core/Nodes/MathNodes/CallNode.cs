﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Materia.Nodes.Attributes;
using Newtonsoft.Json;

namespace Materia.Nodes.MathNodes
{
    public class CallNode : MathNode
    {
        protected int selectedIndex;

        public int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                selectedIndex = value;

                if (topGraph != null)
                {
                    if (selectedIndex >= 0 && selectedIndex < topGraph.CustomFunctions.Count)
                    {
                        if (selectedFunction != null)
                        {
                            var o = selectedFunction.OutputNode;
                            selectedFunction.OnGraphUpdated -= SelectedFunction_OnGraphUpdated;

                            if(o != null)
                            {
                                if(o.Outputs.Count > 1)
                                {
                                    o.Outputs[1].OnTypeChanged -= CallNode_OnTypeChanged;
                                }
                                else if(o.Outputs.Count > 0)
                                {
                                    o.Outputs[0].OnTypeChanged -= CallNode_OnTypeChanged;
                                }
                            }
                        }

                        selectedFunction = topGraph.CustomFunctions[selectedIndex];
                        selectedFunction.OnGraphUpdated += SelectedFunction_OnGraphUpdated;

                        var ou = selectedFunction.OutputNode;

                        if(ou != null)
                        {
                            if(ou.Outputs.Count > 1)
                            {
                                ou.Outputs[1].OnTypeChanged += CallNode_OnTypeChanged;
                            }
                            else if(ou.Outputs.Count > 0)
                            {
                                ou.Outputs[0].OnTypeChanged += CallNode_OnTypeChanged;
                            }
                        }

                        selectedName = selectedFunction.Name;
                        OnDescription(selectedName);
                        UpdateInputs();
                    }
                }
            }
        }

        private void CallNode_OnTypeChanged(NodeOutput inp)
        {
            UpdateOutputType();
        }

        private void SelectedFunction_OnGraphUpdated(Graph g)
        {
            UpdateInputs();
        }

        protected string[] function;

        [Dropdown("SelectedIndex")]
        [Editable(ParameterInputType.Dropdown, "Function")]
        public string[] Function
        {
            get
            {
                return function;
            }
        }

        protected string selectedName;
        public FunctionGraph selectedFunction { get; protected set; }

        NodeOutput output;
        Graph topGraph;

        public CallNode(int w, int h, GraphPixelType p = GraphPixelType.RGBA) : base()
        {
            //we ignore w,h,p

            CanPreview = false;

            Name = "Call";
            Id = Guid.NewGuid().ToString();
            shaderId = "S" + Id.Split('-')[0];

            output = new NodeOutput(NodeType.Float | NodeType.Float2 | NodeType.Float3 | NodeType.Float4 | NodeType.Bool, this);
            Outputs.Add(output);
        }

        public override string GetDescription()
        {
            return selectedName;
        }

        protected void OnParentSet()
        {
            Graph g =  topGraph = TopGraph();

            if(g != null)
            {
                function = new string[g.CustomFunctions.Count];

                int i = 0;
                for (i = 0; i < g.CustomFunctions.Count; i++)
                {
                    var f = g.CustomFunctions[i];

                    if (f.Name.Equals(selectedName) || string.IsNullOrEmpty(selectedName))
                    {
                        if (selectedFunction != null)
                        {
                            var o = selectedFunction.OutputNode;
                            selectedFunction.OnGraphUpdated -= SelectedFunction_OnGraphUpdated;

                            if (o != null)
                            {
                                if (o.Outputs.Count > 1)
                                {
                                    o.Outputs[1].OnTypeChanged -= CallNode_OnTypeChanged;
                                }
                                else if (o.Outputs.Count > 0)
                                {
                                    o.Outputs[0].OnTypeChanged -= CallNode_OnTypeChanged;
                                }
                            }
                        }
                        selectedName = f.Name;
                        selectedIndex = i;
                        selectedFunction = f;
                        selectedFunction.OnGraphUpdated += SelectedFunction_OnGraphUpdated;

                        var ou = selectedFunction.OutputNode;

                        if (ou != null)
                        {
                            if (ou.Outputs.Count > 1)
                            {
                                ou.Outputs[1].OnTypeChanged += CallNode_OnTypeChanged;
                            }
                            else if (ou.Outputs.Count > 0)
                            {
                                ou.Outputs[0].OnTypeChanged += CallNode_OnTypeChanged;
                            }
                        }
                    }

                    function[i] = f.Name;
                    OnDescription(f.Name);
                }

                UpdateInputs();
            }
        }

        protected void UpdateInputs()
        {
            List<NodeOutput> previousOutputs = new List<NodeOutput>();
            List<int> indices = new List<int>();
            List<NodeInput> previous = new List<NodeInput>();

            foreach (var i in Inputs)
            {
                //isntead of directly
                //removing from UI at this point
                //we simply store them
                //for later when
                //we either replace them
                //or completely remove them
                previous.Add(i);

                int index = -1;

                previousOutputs.Add(i.Input);

                if (i.HasInput)
                {
                    index = i.Input.To.IndexOf(i);
                    i.Input.Remove(i);
                }

                indices.Add(index);
            }

            Inputs.Clear();

            if (previousOutputs.Count > 0)
            {
                var prevEx = previousOutputs[0];
                var idx = indices[0];

                if(prevEx != null)
                {
                    if (idx == -1)
                    {
                        prevEx.Add(executeInput, false);
                    }
                    else
                    {
                        prevEx.InsertAt(idx, executeInput, false);
                    }
                }
            }

            Inputs.Add(executeInput);

            if (previous.Count > 0)
            {
                AddedInput(executeInput, previous[0]);
            }
            else
            {
                AddedInput(executeInput);
            }

            if(selectedFunction != null)
            {
                var nodes = selectedFunction.Nodes;
                List<Node> args = nodes.FindAll(m => m is ArgNode);

                int i = 1;
                foreach(ArgNode arg in args)
                {
                    NodeInput input = new NodeInput(arg.InputType, this, arg.InputName);

                    if (previousOutputs.Count > 1 && i < previousOutputs.Count)
                    {
                        var prev = previousOutputs[i];
                        var idx = indices[i];

                        //whoops forgot to verify that the output
                        //parent could indeed accept the new input arg type
                        if (prev != null && (prev.Type & input.Type) != 0)
                        {
                            if (idx < 0)
                            {
                                prev.Add(input, false);
                            }
                            else
                            {
                                prev.InsertAt(idx, input, false);
                            }
                        }
                    }

                    Inputs.Add(input);

                    input.OnInputAdded += Input_OnInputAdded;
                    input.OnInputChanged += Input_OnInputChanged;

                    //if we still have previous nodes
                    //then we simply want to try and
                    //replace them on add
                    //so the UI will update the graphical connections 
                    //otherwise we just add a new input node
                    if (i < previous.Count)
                    {
                        AddedInput(input, previous[i]);
                    }
                    else
                    {
                        AddedInput(input);
                    }

                    i++;
                }

                //remove any left over nodes from ui
                //if they were not replaced
                //this happens in cases where
                //the new function set has less args
                //then the previous function set
                while (i < previous.Count)
                {
                    RemovedInput(previous[i]);
                    i++;
                }

                UpdateOutputType();
            }
        }

        public override void UpdateOutputType()
        {
            if (selectedFunction != null)
            {
                var op = selectedFunction.GetOutputType();

                if (op != null)
                {
                    output.Type = op.Value;
                }
            }
        }

        public string GetFunctionShaderCode()
        {
            if (selectedFunction == null)
            {
                return "";
            }

            return selectedFunction.GetFunctionShaderCode();
        }

        public override string GetShaderPart(string currentFrag)
        {
            if (selectedFunction == null)
            {
                return "";
            }

            bool canProcess = true;

            foreach (var i in Inputs)
            {
                if (i != executeInput)
                {
                    if (!i.HasInput)
                    {
                        canProcess = false;
                        break;
                    }
                }
            }

            if (!canProcess)
            {
                return "";
            }

            var s = shaderId + "1";

            string prefix = "";

            if(output.Type == NodeType.Float)
            {
                prefix = "float ";
            }
            else if(output.Type == NodeType.Float2)
            {
                prefix = "vec2 ";
            }
            else if(output.Type == NodeType.Float3)
            {
                prefix = "vec3 ";
            }
            else if(output.Type == NodeType.Float4 || output.Type == NodeType.Color || output.Type == NodeType.Gray)
            {
                prefix = "vec4 ";
            }
            else if(output.Type == NodeType.Bool)
            {
                prefix = "bool ";
            }
            else if(output.Type == NodeType.Matrix)
            {
                prefix = "mat4 ";
            }

            string frag = prefix + s + " = " + selectedFunction.Name.Replace(" ", "").Replace("-", "_") + "(";

            foreach(var i in Inputs)
            {
                if (i != executeInput)
                {
                    var nid = (i.Input.Node as MathNode).ShaderId;
                    var index = i.Input.Node.Outputs.IndexOf(i.Input);
                    nid += index;

                    frag += nid + ",";
                }
            }

            if(Inputs.Count > 1)
            {
                frag = frag.Substring(0, frag.Length - 1);
            }

            frag += ");\r\n";

            return frag;
        }

        private void Input_OnInputChanged(NodeInput n)
        {
            TryAndProcess();
        }

        private void Input_OnInputAdded(NodeInput n)
        {
            Updated();
        }

        public override void TryAndProcess()
        {
            if (selectedFunction == null) return;

            bool canProcess = true;

            foreach(var i in Inputs)
            {
                if (i != executeInput)
                {
                    if (!i.HasInput)
                    {
                        canProcess = false;
                        break;
                    }
                }
            }

            if(canProcess)
            {
                Process();
            }
        }

        void Process()
        {
            if (selectedFunction == null) return;

            foreach(var i in Inputs)
            {
                if (i != executeInput)
                {
                    if (i.Input.Data == null) return;
                }
            }

            foreach(var i in Inputs)
            {
                if (i != executeInput)
                {
                    selectedFunction.SetVar(i.Name, i.Input.Data, i.Input.Type);
                }
            }

            selectedFunction.TryAndProcess();

            output.Data = selectedFunction.Result;

            if (output.Data != null)
            {
                result = output.Data.ToString();
            }

            if (ParentGraph != null)
            {
                FunctionGraph g = (FunctionGraph)ParentGraph;

                if (g != null && g.OutputNode == this)
                {
                    g.Result = output.Data;
                }
            }
        }

        public class CallNodeData : NodeData
        {
            public int selectedIndex;
            public string selectedName;
        }

        public override string GetJson()
        {
            CallNodeData d = new CallNodeData();
            FillBaseNodeData(d);

            d.selectedIndex = selectedIndex;
            d.selectedName = selectedName;

            return JsonConvert.SerializeObject(d);
        }

        public override void AssignParentGraph(Graph g)
        {
            if (parentGraph != null)
            {
                if (parentGraph is FunctionGraph)
                {
                    FunctionGraph fg = parentGraph as FunctionGraph;

                    fg.OnParentGraphSet -= Fg_OnParentGraphSet;
                    fg.OnParentNodeSet -= Fg_OnParentNodeSet;
                }
            }

            if (g != null)
            {
                if (g is FunctionGraph)
                {
                    FunctionGraph fg = g as FunctionGraph;

                    fg.OnParentGraphSet += Fg_OnParentGraphSet;
                    fg.OnParentNodeSet += Fg_OnParentNodeSet;
                }
            }

            base.AssignParentGraph(g);

            OnParentSet();
        }

        public override void AssignParentNode(Node n)
        {
            base.AssignParentNode(n);

            OnParentSet();
        }

        public override void FromJson(string data)
        {
            CallNodeData d = JsonConvert.DeserializeObject<CallNodeData>(data);
            SetBaseNodeDate(d);
            selectedIndex = d.selectedIndex;
            selectedName = d.selectedName;

            OnParentSet();
        }

        public override void Dispose()
        {
            if(parentGraph != null)
            {
                if(parentGraph is FunctionGraph)
                {
                    FunctionGraph fg = parentGraph as FunctionGraph;

                    fg.OnParentGraphSet -= Fg_OnParentGraphSet;
                    fg.OnParentNodeSet -= Fg_OnParentNodeSet;
                }
            }

            base.Dispose();
        }

        private void Fg_OnParentNodeSet(FunctionGraph g)
        {
            OnParentSet();   
        }

        private void Fg_OnParentGraphSet(FunctionGraph g)
        {
            OnParentSet();
        }
    }
}
