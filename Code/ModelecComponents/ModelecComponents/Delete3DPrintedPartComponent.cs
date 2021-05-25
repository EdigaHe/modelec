using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class Delete3DPrintedPartComponent : GH_Component
    {
        SharedData dataset;
        bool testBtnClick;
        RhinoDoc myDoc;
        TargetBody currBody;

        /// <summary>
        /// Initializes a new instance of the Delete3DPrintedPartComponent class.
        /// </summary>
        public Delete3DPrintedPartComponent()
          : base("Delete3DPrintedPartComponent", "Del3DP",
              "Delete the currently selected 3D-printed part",
              "Modelec", "3DP-Control")
        {
            dataset = SharedData.Instance;
            testBtnClick = false;
            currBody = TargetBody.Instance;
            myDoc = RhinoDoc.ActiveDoc;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Delete3DPrintedPartBtnClicked", "BC", "delete the 3D-printed part", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("ListUpdated", "ListUp", "the list is updated", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool btnClick = false;
            bool isOn = false;

            if (!DA.GetData(0, ref btnClick))
                return;

            #region read the button click 
            if (!btnClick && testBtnClick)
            {
                isOn = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }
            #endregion

            if (isOn && !dataset.currPart3DP.PartName.Equals("null"))
            {
                
                int idx = -1;
                foreach(Part3DP p3d in dataset.part3DPList)
                {
                    if (p3d.PartName.Equals(dataset.currPart3DP.PartName))
                    {
                        idx = dataset.part3DPList.IndexOf(p3d);
                        break;
                    }
                }

                // remove it from the Rhino scene

                myDoc.Objects.Delete(dataset.part3DPList.ElementAt(idx).PrtID, true);
                myDoc.Views.Redraw();


                // remove the element from the list
                if (idx != -1)
                    dataset.part3DPList.RemoveAt(idx);

                // set the currently selected 3D-printed part name to "null" and reset all the parameters
                dataset.currPart3DP.PartName = "null";
                dataset.currPart3DP.PartPos = new Point3d();
                dataset.currPart3DP.Depth = -1;
                dataset.currPart3DP.Height = -1;
                dataset.currPart3DP.IsDeployed = false;
                dataset.currPart3DP.Length = -1;
                dataset.currPart3DP.Level = 1;
                dataset.currPart3DP.Pins = new List<Pin>();
                dataset.currPart3DP.PrtID = Guid.Empty;
                dataset.currPart3DP.Type = -1;

                DA.SetData(0, true);
            }
            else
            {
                DA.SetData(0, false);
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
            get { return new Guid("582a97e1-5d18-49bc-8164-de3884b09f16"); }
        }
    }
}