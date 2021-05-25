﻿using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Component to update a browser object
    /// </summary>
    /// <seealso cref="Grasshopper.Kernel.GH_Component" />
    public class SetBrowser_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SetBrowser_Component class.
        /// </summary>
        public SetBrowser_Component()
            : base("Set Browser", "SetBrowser",
                "Control the Browser element - with back/forward buttons, and control over the displayed site etc.",
                "Human UI", "UI Output")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Browser", "B", "The Browser UI Element to modify", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Back", "Bk", "Set to true in order to send the browser back a page", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Forward", "Fwd", "Set to true to send the browser forward a page", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Refresh", "Rf", "Set to true to refresh the current window", GH_ParamAccess.item);
            pManager.AddTextParameter("URL", "U", "The URL to set the current browser to access.", GH_ParamAccess.item);
            for(int i=1;i<5;i++){
                pManager[i].Optional = true;
            }
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
            object obj = null;
            bool forward = false;
            bool back = false;
            bool refresh = false;
            string URL = "";
            if (!DA.GetData<object>("Browser", ref obj)) return;
            //get the web browser object
            WebBrowser wb = HUI_Util.GetUIElement<WebBrowser>(obj);

            //Back
            if (DA.GetData<bool>("Back", ref back))
            {
                if (wb.CanGoBack && back)
                {
                    wb.GoBack();
                }
            }
            //Forward
            if (DA.GetData<bool>("Forward", ref forward))
            {
                if (wb.CanGoForward && forward)
                {
                    wb.GoForward();
                }
            }
            //Refresh
            if (DA.GetData<bool>("Refresh", ref refresh))
            {
                if (refresh)
                {
                    wb.Refresh();
                }
            }
            //URL
            if (DA.GetData<string>("URL", ref URL))
            {
                wb.Source = new Uri(URL);
            }




        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.setBrowser;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{a880cc82-b5df-45bc-a730-afa8352c4679}");
    }
}