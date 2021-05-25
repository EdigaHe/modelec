using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class StackedButtonsComponent : GH_Component
    {
        IGH_Component Component;
        GH_Document GrasshopperDocument;
        public List<GH_ActiveObject> components = new List<GH_ActiveObject>();
        public List<GH_ActiveObject> reserveComponents = new List<GH_ActiveObject>();

        public void RemoveAllComponents(GH_Document doc)
        {
            reserveComponents.Clear();
            foreach(var component in components)
            {
                reserveComponents.Add(component);
            }
            doc.ScheduleSolution(0, (val) => {
                foreach(var comp in reserveComponents)
                {
                    doc.RemoveObject(comp.Attributes, false);
                }
                reserveComponents.Clear();
            });
        }
        /// <summary>
        /// Initializes a new instance of the StackedButtonsComponent class.
        /// </summary>
        public StackedButtonsComponent()
          : base("StackedButtonsComponent", "SB",
              "Dynamically generate the part buttons for a circuit",
              "Modelec", "Utility")
        {

        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // pManager.AddGenericParameter("Dataset", "Dataset", "The dataset shared with all the components", GH_ParamAccess.item);
            pManager.AddBooleanParameter("isActive", "isActive", "To display the components", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("PartNameList", "PtNameList", "The list of all part in the schematic", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            //List<GH_ObjectWrapper> inputPart2DList = new List<GH_ObjectWrapper>();
            SharedData dataset = SharedData.Instance;
            bool isActive = false;
            List<string> ptNameList = new List<string>();
            bool toCreateButtonList = false;

            #region read the part info

            if (!DA.GetData(0, ref isActive))
                return;

            if(dataset.part2DList.Count > 0 && isActive)
            {
                toCreateButtonList = true;
            }
            #endregion

            if (toCreateButtonList)
            {

                RemoveAllComponents(GrasshopperDocument);

                // Convert the ObjectWrapper to Part2D
                //foreach(GH_ObjectWrapper objWrap in inputPart2DList)
                //{
                //    part2DList.Add(objWrap.Value as Part2D);
                //}

                int count = dataset.part2DList.Count;
                int idx = 0;

                // ToDo: change the stack of buttons to a dropdown list

                foreach (Part2D p in dataset.part2DList)
                {
                    #region old code for adding buttons at runtime
                    //var panel = new Grasshopper.Kernel.Special.GH_Panel();
                    //panel.CreateAttributes();
                    //panel.Attributes.Pivot = new System.Drawing.PointF(100, 50 + 80 * idx);
                    //panel.SetUserText(p.PartName);
                    //components.Add(panel);
                    //GrasshopperDocument.AddObject(panel, false);

                    //var partBtn = new HumanUI.Components.UI_Elements.CreateButton_Component();
                    //partBtn.CreateAttributes();
                    //partBtn.Attributes.Pivot = new System.Drawing.PointF(200, 50 + 80 * idx);
                    //partBtn.Name = p.PartName;
                    //partBtn.Description = p.PosX.ToString() + ";" + p.PosY.ToString();
                    //partBtn.Params.Input[0].AddSource(panel);
                    //components.Add(partBtn);
                    //GrasshopperDocument.AddObject(partBtn, false);

                    //idx++;

                    //mergeComp.Params.Input.Add(partBtn.Params.Output[0]);
                    #endregion

                    // dynamically add items to the pulldown menu and list all the parts from the schematic
                    ptNameList.Add(p.PartName);
                }

                //ptNameList.Add("--- ModElec 3D Printable Parts ---");
                //ptNameList.Add("Ground bus");
                //ptNameList.Add("Power bus");
                //ptNameList.Add("Resistor");
                //ptNameList.Add("Capacitor");
                //ptNameList.Add("Touch pad");
            }

            DA.SetDataList(0, ptNameList);
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
            get { return new Guid("858792a4-fa91-4df1-a702-acdec1891492"); }
        }
    }
}