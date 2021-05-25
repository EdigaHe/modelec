using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class ParseSpotlightComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ParseSpotlightComponent class.
        /// </summary>
        public ParseSpotlightComponent()
          : base("ParseSpotlightComponent", "PS",
              "Parse what spotlight should be generated",
              "Modelec", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Part3D", "P3D", "The ModElec part unit", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Data", "Dataset", "The dataset shared with all the components", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Width", "SchematicWidth", "The width of the imported schematic", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Height", "SchematicHeight", "The height of the imported schematic", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Visibility", "V", "Show or hide the spotlight", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("X Position", "XPos", "The x position of the spotlight", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("Y Position", "YPos", "The y position of the spotlight", GH_ParamAccess.item);
            pManager.AddTextParameter("Margins", "M", "The margins of the spotlight", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string readPartName = "";
            bool isShow = false;
            int xOffset = 0, yOffset = 0;
            int w = 0, h = 0;
            string margins = "";

            bool isOn = false;
            SharedData dataset = SharedData.Instance;
            Part3D selPart = new Part3D();

            #region read the button click and decide whether execute the circuit load or not
            if (!DA.GetData(0, ref selPart))
                return;

            //if (!DA.GetData(1, ref dataset))
            //    return;

            if (!DA.GetData(1, ref w))
                return;

            if (!DA.GetData(2, ref h))
                return;

            if (selPart.PartName != ""  && dataset.part2DList.Count > 0)
            {
                isOn = true;
            }
            else
            {
                isOn = false;
            }

            #endregion

            if (isOn)
            {
                readPartName = selPart.PartName;
                foreach(Part2D prt in dataset.part2DList)
                {
                    if (prt.PartName.Equals(readPartName))
                    {
                        isShow = true;
                        xOffset = (int)prt.PosX;
                        yOffset = (int)prt.PosY;

                        margins = (xOffset).ToString() + "," + ((-1) * h + yOffset).ToString() + ",0,0";
                    }
                }
            }

            DA.SetData(0, isShow);
            DA.SetData(1, margins);
            
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
            get { return new Guid("a3069de3-a90d-4a0e-a38d-50900d4b730d"); }
        }
    }
}