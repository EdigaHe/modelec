using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class SetPart3DPrintableComponent : GH_Component
    {
        bool testBtnClick;
        RhinoDoc myDoc;
        SharedData dataset;
        TargetBody currBody;

        /// <summary>
        /// Initializes a new instance of the SetPart3DPrintableComponent class.
        /// </summary>
        public SetPart3DPrintableComponent()
          : base("SetPart3DPrintableComponent", "Part3DPrintable",
              "Set the part to 3D printable",
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
            pManager.AddBooleanParameter("SetPartPrintableBtnClicked", "BC", "Set the part 3D printable", GH_ParamAccess.item);
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
            bool toExePrtSetPrintability = false;
            //Part3D selPrt = new Part3D();
            Part3D selPrt = dataset.currPart3D;

            #region read the button click and decide whether execute the circuit load or not
            if (!DA.GetData(0, ref btnClick))
                return;

            //if (!DA.GetData(1, ref selPrt))
            //    return;

            if (!btnClick && testBtnClick && selPrt.PartName != "")
            {
                toExePrtSetPrintability = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                toExePrtSetPrintability = true;
                testBtnClick = true;
            }

            #endregion

            if (toExePrtSetPrintability)
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

                    #region set the flag for 3D printable

                    if (!testBtnClick)
                    {
                        // the part is not 3D printable
                        dataset.part3DList.ElementAt(prt_idx).IsPrintable = false;
                    }
                    else
                    {
                        // the part is 3D printable
                        dataset.part3DList.ElementAt(prt_idx).IsPrintable = true;
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
            get { return new Guid("75e81436-475c-4b9f-b13e-36b58bc36f87"); }
        }
    }
}