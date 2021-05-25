﻿using System;
using System.Collections.Generic;
using System.Windows.Controls;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Component to set the contents of an existing TextBox object
    /// </summary>
    /// <seealso cref="Grasshopper.Kernel.GH_Component" />
    public class SetTextBox_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SetLabel_Component class.
        /// </summary>
        public SetTextBox_Component()
            : base("Set TextBox Contents", "SetTextBox",
                "Modify the contents of an existing Text Box object.",
                "Human UI", "UI Output")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Text Box to modify", "TB", "The text box object to modify", GH_ParamAccess.item);
            pManager.AddTextParameter("New Text Box contents", "C", "The new text to display in the text box", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object TextBlockObject = null;
            string newLabelContents = "";
            if (!DA.GetData<string>("New Text Box contents", ref newLabelContents)) return;
            if (!DA.GetData<object>("Text Box to modify", ref TextBlockObject)) return;
            // Since HUI textboxes are actually stackpanels with textboxes inside (since they may or may not also contain a button)
            // we have to grab the stackpanel first and then find the textbox inside it. 
            Panel sp = HUI_Util.GetUIElement<Panel>(TextBlockObject);
            TextBox tb = HUI_Util.findTextBox(sp);
            
            if (tb != null)
            {
                //set the text of the textbox. 
               tb.Text = newLabelContents;
            }


        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetTextBox;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{59b523a4-d32c-4a20-883c-a9cb828bf880}");
    }
}