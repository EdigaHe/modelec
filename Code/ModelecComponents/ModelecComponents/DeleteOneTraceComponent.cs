using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class DeleteOneTraceComponent : GH_Component
    {
        SharedData dataset;
        bool testBtnClick;
        RhinoDoc myDoc;
        TargetBody currBody;

        /// <summary>
        /// Initializes a new instance of the DeleteOneTraceComponent class.
        /// </summary>
        public DeleteOneTraceComponent()
          : base("DeleteOneTraceComponent", "DelTrace",
              "Allows the user to select one trace from the list and delete it",
              "ModElec", "TraceOperations")
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
            pManager.AddBooleanParameter("DeleteTraceBtnClicked", "BC", "delete the selected trace", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("ListUpdated", "ListUpfromDel", "the list is updated", GH_ParamAccess.item);
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

            if (isOn && dataset.currTrace.Count !=0)
            {
                // remove the element from the list

                List<int> toRemovePos = new List<int>();

                foreach(Trace currTrc in dataset.currTrace)
                {
                    myDoc.Objects.Delete(currTrc.TrID, true);
                    
                    int i = dataset.deployedTraces.IndexOf(dataset.deployedTraces.Find(x => x.TrID.Equals(currTrc.TrID)));
                    toRemovePos.Add(i);
                }

                myDoc.Views.Redraw();

                toRemovePos.Sort((a, b) => b.CompareTo(a));

                foreach(int i in toRemovePos)
                {
                    dataset.deployedTraces.RemoveAt(i);
                }

                // set the currently selected 3D-printed part name to "null" and reset all the parameters
                dataset.currTrace.Clear();
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
            get { return new Guid("acb55cce-4231-4dd0-8965-2f0d15112a53"); }
        }
    }
}