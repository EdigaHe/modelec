using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;

namespace ModelecComponents.Creator
{
    public class AddPadComponent : GH_Component
    {
        bool testBtnClick;
        CreatorData creatorData;
        /// <summary>
        /// Initializes a new instance of the AddPadComponent class.
        /// </summary>
        public AddPadComponent()
          : base("AddPadComponent", "AddPad",
              "Add the information of the pad",
              "ModElec", "Model Creator")
        {
            testBtnClick = false;
            creatorData = CreatorData.Instance;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("SetPosBtnClicked", "BC", "Set the position for the selected part", GH_ParamAccess.item);
            pManager.AddTextParameter("PadName", "PN", "Set the pad name", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("PadListUpdate", "PLU", "the list should update", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool btnClick = false;
            string padName = "";
            bool toExePad = false;
            bool isUpdate = false;

            #region read the button click and decide whether execute the circuit load or not
            if (!DA.GetData(0, ref btnClick))
                return;

            if (!DA.GetData(1, ref padName))
                return;

            if (!btnClick && testBtnClick && padName!="")
            {
                toExePad = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }
            #endregion

            if (toExePad)
            {
                ObjRef objSel_ref;
                Guid selObjId = Guid.Empty;
                var rc = RhinoGet.GetOneObject("Select a point", false, ObjectType.Point, out objSel_ref);
                if (rc == Rhino.Commands.Result.Success)
                {
                    // ask the user to select a point in the scene
                    selObjId = objSel_ref.ObjectId;
                    
                    ObjRef currObj = new ObjRef(selObjId);
                    Point selPt = currObj.Point();
                    Point3d selPtLoc = selPt.Location;

                    PadInfo temp = new PadInfo();
                    temp.PadName = padName;
                    temp.X = selPtLoc.X;
                    temp.Y = selPtLoc.Y;
                    temp.Z = selPtLoc.Z;

                    creatorData.pads.Add(temp);

                    isUpdate = true;
                      
                }
            }

            DA.SetData(0, isUpdate);
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
            get { return new Guid("5febfdb4-7bf3-443e-9dd5-43cb93c17efd"); }
        }
    }
}