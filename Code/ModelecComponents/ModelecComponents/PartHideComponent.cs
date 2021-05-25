using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class PartHideComponent : GH_Component
    {
        bool testBtnClick;
        RhinoDoc myDoc;
        SharedData dataset;
        TargetBody currBody;

        /// <summary>
        /// Initializes a new instance of the PartHideComponent class.
        /// </summary>
        public PartHideComponent()
          : base("PartHideComponent", "PHide",
              "Hide the part",
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
            pManager.AddBooleanParameter("HidePartBtnClicked", "BC", "flip the part", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Part3D", "P3D", "The ModElec part unit", GH_ParamAccess.item);
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
            bool toExePrtHide = false;
            //Part3D selPrt = new Part3D();
            Part3D selPrt = dataset.currPart3D;

            #region read the button click and decide whether execute the circuit load or not
            if (!DA.GetData(0, ref btnClick))
                return;

            //if (!DA.GetData(1, ref selPrt))
            //    return;

            if (!btnClick && testBtnClick && selPrt.PartName != "")
            {
                toExePrtHide = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                toExePrtHide = true;
                testBtnClick = true;
            }

            #endregion

            if (toExePrtHide)
            {
                bool isDeployedReal = false;
                foreach (Part3D prt in dataset.part3DList)
                {
                    if (prt.PartName.Equals(selPrt.PartName))
                    {
                        isDeployedReal = prt.IsDeployed;
                        break;
                    }
                }

                if (!isDeployedReal)
                {
                    // the part has not been deployed
                }
                else
                {
                    int prt_idx = -1;
                    foreach (Part3D prt3D in dataset.part3DList)
                    {
                        if (prt3D.PartName.Equals(selPrt.PartName))
                        {
                            prt_idx = dataset.part3DList.IndexOf(prt3D);
                        }
                    }

                    #region hide or show the part in place

                    Guid partID = dataset.part3DList.ElementAt(prt_idx).PrtID;

                    if (!testBtnClick)
                    {
                        // show the part
                        myDoc.Objects.Show(partID, true);
                    }
                    else
                    {
                        // hide the part
                        myDoc.Objects.Hide(partID, true);
                    }
                    #endregion

                    dataset.currPart3D = dataset.part3DList.ElementAt(prt_idx);
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
            get { return new Guid("148d4ea6-25ea-490b-aef2-a5403efafd22"); }
        }
    }
}