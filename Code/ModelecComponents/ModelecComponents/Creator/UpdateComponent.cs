using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace ModelecComponents.Creator
{
    public class UpdateComponent : GH_Component
    {
        CreatorData creatorData;
        /// <summary>
        /// Initializes a new instance of the UpdateComponent class.
        /// </summary>
        public UpdateComponent()
          : base("UpdateComponent", "Up",
              "Update the list",
              "ModElec", "Model Creator")
        {
            creatorData = CreatorData.Instance;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("PadListUpdate", "PLU", "the list should update", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("PadList", "PL", "the list", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool readFlag = false;
            List<string> result = new List<string>();

            if (!DA.GetData(0, ref readFlag))
                return;

            if (true)
            {
                // add the current pad to the list
                foreach (PadInfo pi in creatorData.pads)
                {
                    string padinfotext = String.Format("{0} ({1:N1},{2:N1},{3:N1})", pi.PadName, pi.X, pi.Y, pi.Z);
                    result.Add(padinfotext);
                }
            }

            DA.SetDataList(0, result);
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
            get { return new Guid("61cb947a-0e6b-4f14-80fb-7814cfa4f269"); }
        }
    }
}