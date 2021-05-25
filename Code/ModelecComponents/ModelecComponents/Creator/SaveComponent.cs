using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting;
using System.Xml;
using Grasshopper.Kernel;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;

namespace ModelecComponents.Creator
{
    public class SaveComponent : GH_Component
    {
        bool testBtnClick;
        CreatorData creatorData;

        /// <summary>
        /// Initializes a new instance of the SaveComponent class.
        /// </summary>
        public SaveComponent()
          : base("SaveComponent", "Save",
              "Save the 3D model and related pin information",
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
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool btnClick = false;
            bool toExeSave = false;
            string filename = "";

            #region read the button click and decide whether execute the circuit load or not
            if (!DA.GetData(0, ref btnClick))
                return;

            if (!DA.GetData(1, ref filename))
                return;

            if (!btnClick && testBtnClick && filename!="")
            {
                toExeSave = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }
            #endregion

            if (toExeSave)
            {
                string currDir = Directory.GetCurrentDirectory() + "\\";
                string modelSTL = currDir + filename + ".stl";
                string partXML = currDir + filename + ".xml";

                if (File.Exists(modelSTL)) File.Delete(modelSTL);
                if (File.Exists(partXML)) File.Delete(partXML);

                #region Step 1: save the model as a stl

                Rhino.DocObjects.ObjRef objSel_ref;
                Guid selObjId = Guid.Empty;
                var rc = RhinoGet.GetOneObject("Select surface or polysurface to mesh", false, ObjectType.AnyObject, out objSel_ref);
                if (rc == Rhino.Commands.Result.Success)
                {

                    String str1 = "_ExportFileAs=_Binary ";
                    String str2 = "_ExportUnfinishedObjects=_Yes ";
                    String str3 = "_UseSimpleDialog=_No ";
                    String str4 = "_UseSimpleParameters=_Yes ";
                    String str5 = "_Enter _Enter";

                    String str = str1 + str2 + str3 + str4 + str5;

                    var stlScript = string.Format("_-Save \"{0}\" {1}", modelSTL, str);
                    Rhino.RhinoApp.RunScript(stlScript, false);

                }

                #endregion

                #region Step 2: save the information into a XML file

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.IndentChars = ("    ");
                settings.CloseOutput = true;
                settings.OmitXmlDeclaration = true;

                using (XmlWriter writer = XmlWriter.Create(partXML, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("part");

                    foreach (PadInfo pi in creatorData.pads)
                    {
                        writer.WriteStartElement("pin");
                        writer.WriteElementString("name", pi.PadName);
                        writer.WriteElementString("x", pi.X.ToString());
                        writer.WriteElementString("y", pi.Y.ToString());
                        writer.WriteElementString("z", pi.Z.ToString());
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }

                #endregion
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
            get { return new Guid("71c8b9ca-4480-4b9d-8429-981993320c95"); }
        }
    }
}