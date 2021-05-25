using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class Update3DPListComponent : GH_Component
    {
        SharedData dataset;
        bool listUpdatedAdd;
        bool listUpdatedDel;

        bool old_updated;

        /// <summary>
        /// Initializes a new instance of the Update3DPListComponent class.
        /// </summary>
        public Update3DPListComponent()
          : base("Update3DPListComponent", "Update3DPList",
              "Update the 3D-printed part list",
              "Modelec", "3DP-Control")
        {
            dataset = SharedData.Instance;
            listUpdatedAdd = false;
            listUpdatedDel = false;
            old_updated = false;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("AddListUpdated", "ListUp", "the list is updated by adding", GH_ParamAccess.item);
            pManager.AddBooleanParameter("DelListUpdated", "ListUp", "the list is updated by deleting", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("3DPrintedPartList", "3DPList", "List of added 3D-printed parts", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        { 
            bool isOn = true;
            List<string> output = new List<string>();

            if (!DA.GetData(0, ref listUpdatedAdd))
                return;

            if (!DA.GetData(1, ref listUpdatedDel))
                return;

            if (isOn)
            {
                foreach (Part3DP p3DP in dataset.part3DPList)
                {
                    output.Add(p3DP.PartName);
                }
            }
            else
            {
                output.Add(" ");
                output.Add(" ");
            }

            DA.SetDataList(0, output);
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
            get { return new Guid("7978e741-3939-4507-9ed7-508644533ba7"); }
        }
    }
}