using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class HideTracesComponent : GH_Component
    {
        RhinoDoc myDoc;
        SharedData dataset;
        TargetBody currBody;
        bool testBtnClick;

        /// <summary>
        /// Initializes a new instance of the HideTracesComponent class.
        /// </summary>
        public HideTracesComponent()
          : base("HideTracesComponent", "HideTraces",
              "Hide or show all the traces",
              "ModElec", "TraceOperations")
        {
            myDoc = RhinoDoc.ActiveDoc;
            dataset = SharedData.Instance;
            currBody = TargetBody.Instance;
            testBtnClick = false;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("HideAllTracesBtnClicked", "HideTraceC", "hide all the traces", GH_ParamAccess.item);
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
            bool btnClick = false;
            bool toExeTraceHideSet = false;

            #region read the button click and decide whether or not to hide all the traces
            if (!DA.GetData(0, ref btnClick))
                return;

            #endregion

            if (!btnClick)
            {
                toExeTraceHideSet = false;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                toExeTraceHideSet = true;
                testBtnClick = true;
            }


            if (toExeTraceHideSet)
            {
                if (myDoc.Layers.FindName("Traces") != null) {
                    myDoc.Layers.SetCurrentLayerIndex(0, true);
                    int index = myDoc.Layers.FindName("Traces").Index;
                    myDoc.Layers.ElementAt(index).IsVisible = false;
                }
            }
            else
            {
                if (myDoc.Layers.FindName("Traces") != null)
                {
                    myDoc.Layers.SetCurrentLayerIndex(0, true);
                    int index = myDoc.Layers.FindName("Traces").Index;
                    myDoc.Layers.ElementAt(index).IsVisible = true;
                }
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("81a70bd4-34db-4efc-9c99-ebe08018733b"); }
        }
    }
}