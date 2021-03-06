using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class ClearTracesComponent : GH_Component
    {
        bool testBtnClick;
        RhinoDoc myDoc;
        SharedData dataset;
        TargetBody currBody;
        /// <summary>
        /// Initializes a new instance of the ClearTracesComponent class.
        /// </summary>
        public ClearTracesComponent()
          : base("ClearTracesComponent", "ClearTra",
              "Clear all the deployed traces",
              "ModElec", "TraceOperations")
        {
            testBtnClick = false;
            myDoc = RhinoDoc.ActiveDoc;
            dataset = SharedData.Instance;
            currBody = TargetBody.Instance;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("ClearTracesBtnClicked", "BC", "clear the trace", GH_ParamAccess.item);
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
            bool toExeTraceClear = false;

            #region read the button click and decide whether execute the circuit load or not

            if (!DA.GetData(0, ref btnClick))
                return;

            if (!btnClick && testBtnClick)
            {
                toExeTraceClear = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }

            #endregion

            #region clear all traces that are already deployed

            if (toExeTraceClear)
            {
                myDoc.Objects.Delete(currBody.convertedObjID, true);
                myDoc.Objects.Show(currBody.objID, true);

                foreach (Trace trace in dataset.deployedTraces)
                {
                    myDoc.Objects.Delete(trace.TrID, true);
                }

                dataset.deployedTraces.Clear();
            }

            #endregion

            myDoc.Views.Redraw();
            dataset.isTraceGenerated = false;
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
            get { return new Guid("e1f4bd57-acec-4228-b5bb-cd0de433d327"); }
        }
    }
}