﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents
{
    public class TornadoAgentTool
    {
        public TornadoAgent ToolAgent { get; set; }
        //Need to abstract this
        public BaseTool Tool { get; set; }

        public TornadoAgentTool(TornadoAgent agent, BaseTool tool)
        {
            ToolAgent = agent;
            Tool = tool;
        }
    }

    public class FunctionTool : BaseTool
    {
        public Delegate Function { get; set; }
        public FunctionTool(string toolName, string toolDescription, BinaryData toolParameters, Delegate function, bool strictSchema = false)
            : base(toolName, toolDescription, toolParameters, strictSchema)
        {
            Function = function;
        }
    }

    public class BaseTool
    {
        public string ToolName { get; set; }
        public string ToolDescription { get; set; }
        public BinaryData ToolParameters { get; set; }
        public bool FunctionSchemaIsStrict { get; set; }
        public BaseTool() { }

        public BaseTool(string toolName, string toolDescription, BinaryData toolParameters, bool strictSchema = false)
        {
            ToolName = toolName;
            ToolDescription = toolDescription;
            ToolParameters = toolParameters;
            FunctionSchemaIsStrict = strictSchema;
        }

        public virtual BaseTool CreateTool(string toolName, string toolDescription, BinaryData toolParameters, bool strictSchema = false)
        {
            return new BaseTool(toolName, toolDescription, toolParameters);
        }
    }
}
