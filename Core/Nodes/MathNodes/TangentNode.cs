﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Materia.MathHelpers;

namespace Materia.Nodes.MathNodes
{
    public class TangentNode : MathNode
    {
        NodeInput input;
        NodeOutput output;

        public TangentNode(int w, int h, GraphPixelType p = GraphPixelType.RGBA) : base()
        {
            //we ignore w,h,p

            CanPreview = false;

            Name = "Tangent";
            Id = Guid.NewGuid().ToString();
            shaderId = "S" + Id.Split('-')[0];

            input = new NodeInput(NodeType.Float, this, "Float Input");
            output = new NodeOutput(NodeType.Float, this);

            Inputs.Add(input);

            input.OnInputAdded += Input_OnInputAdded;
            input.OnInputChanged += Input_OnInputChanged;

            Outputs.Add(output);
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
            if (input.HasInput)
            {
                Process();
            }
        }

        public override string GetShaderPart(string currentFrag)
        {
            if (!input.HasInput) return "";
            var s = shaderId + "1";
            var n1id = (input.Input.Node as MathNode).ShaderId;

            var index = input.Input.Node.Outputs.IndexOf(input.Input);

            n1id += index;

            return "float " + s + " = tan(" + n1id + ");\r\n"; 
        }

        void Process()
        {
            object o = input.Input.Data;

            if (o is float || o is int || o is double || o is long)
            {
                float v = Convert.ToSingle(o);
                output.Data = (float)Math.Tan(v);
            }
            else
            {
                output.Data = 0;
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
    }
}