using System;
using System.Collections.Generic;
using System.Security;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ModelecComponents.Helpers;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class PrintProfileParserComponent : GH_Component
    {
        bool testBtnClick;
        int count;

        // JSON Parser related
        public JSONHelpers jsonHelper = new JSONHelpers();

        /// <summary>
        /// Initializes a new instance of the PrintProfileParserComponent class.
        /// </summary>
        public PrintProfileParserComponent()
          : base("PrintProfileParserComponent", "PPP",
              "Read and load the printer profile",
              "Modelec", "Utility")
        {
            testBtnClick = false;
            count = 0;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("LoadPrinterProfile", "LPP", "Open the selection window and select a printer profile JSON file to load", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Result", "R", "Parse and load all the parameters for the modeling process", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool btnClick = false;
            bool toExeCircuitLoad = false;
            string desp = "";
            string result = "";

            #region read the button click and decide whether execute the circuit load or not
            if (!DA.GetData(0, ref btnClick))
                return;

            if (!btnClick && testBtnClick)
            {
                toExeCircuitLoad = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }

            #endregion

            if (toExeCircuitLoad)
            {
                count++;

                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Create a new button with the information of the selected circuit
                        // Add the button to the list
                        string filename = openFileDialog.FileName;
                        string suffix = filename.Substring(filename.IndexOf('.') + 1, filename.Length - filename.IndexOf('.') - 1);
                        desp = filename.Substring(filename.LastIndexOf('\\') + 1, filename.IndexOf('.') - filename.LastIndexOf('\\') - 1);
                        
                        if(suffix.Equals("json") || suffix.Equals("JSON"))
                        {

                            jsonHelper.loadPrinterProfile(filename);
                            result = "Printer profile loaded!";
                        }
     
                    }
                    catch (SecurityException ex)
                    {
                        MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                        $"Details:\n\n{ex.StackTrace}");
                    }
                }
            }

            DA.SetData(0, result);

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
            get { return new Guid("5273706b-6a46-45af-95c5-c360c73013fb"); }
        }
    }
}