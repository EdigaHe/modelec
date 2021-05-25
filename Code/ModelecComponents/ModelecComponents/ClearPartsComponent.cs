using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class ClearPartsComponent : GH_Component
    {
        bool testBtnClick;
        RhinoDoc myDoc;
        SharedData dataset;
        TargetBody currBody;
        /// <summary>
        /// Initializes a new instance of the ClearPartsComponent class.
        /// </summary>
        public ClearPartsComponent()
          : base("ClearPartsComponent", "ClearParts",
              "Clear all the parts",
              "ModElec", "PartOperations")
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
            pManager.AddBooleanParameter("ClearPartsBtnClicked", "BC", "clear the part", GH_ParamAccess.item);
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
            bool toExePrtClear = false;

            #region read the button click and decide whether execute the circuit load or not

            if (!DA.GetData(0, ref btnClick))
                return;

            if (!btnClick && testBtnClick)
            {
                toExePrtClear = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }

            #endregion

            #region clear all the 3D information for those parts that are already deployed

            if (toExePrtClear)
            {
                foreach(Part3D prt3D in dataset.part3DList)
                {
                    if (prt3D.IsDeployed)
                    {
                        prt3D.IsDeployed = false;
                        prt3D.IsFlipped = false;
                        prt3D.Normal = new Vector3d();
                        prt3D.PartPos = new Point3d(0, 0, 0);

                        myDoc.Objects.Delete(prt3D.PrtID, true);
                        prt3D.PrtID = Guid.Empty;
                        
                        prt3D.RotationAngle = 0;
                        prt3D.TransformReverseSets.Clear();
                        prt3D.TransformSets.Clear();
                    }
                }
            }

            #endregion

            myDoc.Views.Redraw();
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
            get { return new Guid("6665ac6e-31b2-481f-b27d-7458da0167a7"); }
        }
    }
}