using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class Select3DPrintedPartFromListComponent : GH_Component
    {
        SharedData dataset;
        bool testBtnClick;
        RhinoDoc myDoc;
        TargetBody currBody;
        /// <summary>
        /// Initializes a new instance of the Select3DPrintedPartFromListComponent class.
        /// </summary>
        public Select3DPrintedPartFromListComponent()
          : base("Select3DPrintedPartFromListComponent", "Sel3DPP",
              "Select the 3D-printed part from the list",
              "Modelec", "3DP-Control")
        {
            dataset = SharedData.Instance;
            myDoc = RhinoDoc.ActiveDoc;
            currBody = TargetBody.Instance;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("PartName", "PN", "select a part form the list", GH_ParamAccess.item);
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
            string read3DPPartName = "";

            if (!DA.GetData(0, ref read3DPPartName))
                return;

            if (!read3DPPartName.Equals(""))
            {
                // update the 3D-printed part that is currently selected
                foreach(Part3DP p3dp in dataset.part3DPList)
                {
                    if (p3dp.PartName.Equals(read3DPPartName))
                    {
                        dataset.currPart3DP = new Part3DP();
                        dataset.currPart3DP.PartName = p3dp.PartName;
                        dataset.currPart3DP.PartPos = p3dp.PartPos;
                        dataset.currPart3DP.Depth = p3dp.Depth;
                        dataset.currPart3DP.Height = p3dp.Height;
                        dataset.currPart3DP.IsDeployed = p3dp.IsDeployed;
                        dataset.currPart3DP.Length = p3dp.Length;
                        dataset.currPart3DP.Level = p3dp.Level;
                        dataset.currPart3DP.Pins.Clear();
                        foreach(Pin p in p3dp.Pins)
                        {
                            dataset.currPart3DP.Pins.Add(p);
                        }
                        dataset.currPart3DP.PrtID = p3dp.PrtID;
                        dataset.currPart3DP.Type = p3dp.Type;

                        // highlight the object in the Rhino Scene
                        myDoc.Objects.UnselectAll();
                        myDoc.Objects.Select(dataset.currPart3DP.PrtID);
                        myDoc.Views.Redraw();
                    }
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
            get { return new Guid("74fe9e56-e720-4cb0-bbe5-c32a02e2a771"); }
        }
    }
}