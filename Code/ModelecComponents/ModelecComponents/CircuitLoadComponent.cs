using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ModelecComponents.Helpers;
using System.Security;
using System.Drawing;
using System.IO;
using Grasshopper.Kernel.Types;
using GemBox.Pdf;
using GemBox.Pdf.Content;
using Rhino;
using Rhino.DocObjects;
using Rhino.Input;
using System.Linq;
using Rhino.Geometry.Intersect;
using Rhino.UI;


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace ModelecComponents
{
    

    public class CircuitLoadComponent : GH_Component
    {
        bool testBtnClick;
        int count;
        SharedData dataset;

        RhinoDoc myDoc;
        TargetBody currBody;
        ProcessingWindow prcWin;

        

        // XML Parser related
        public XmlHelpers xmlHelper = new XmlHelpers();

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CircuitLoadComponent()
          : base("LoadCircuit", "LC",
              "Load the external circuit schematics from Fritzing or Autodesk Eagle",
              "Modelec", "Utility")
        {
            testBtnClick = false;
            count = 0;
            dataset = SharedData.Instance;
            myDoc = RhinoDoc.ActiveDoc;
            currBody = TargetBody.Instance;
            prcWin = new ProcessingWindow();

        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("LoadCircuitStart", "LC", "Open the selection window and select a circuit to load", GH_ParamAccess.item);
            pManager.AddTextParameter("CurrentDirectory", "CDir", "The directory the Grassshopper components is at", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("ClickNum", "cNum", "The number of clicks", GH_ParamAccess.item);
            pManager.AddTextParameter("CircuitInfo", "CirInfo", "The information of the loaded circuit", GH_ParamAccess.item);
            pManager.AddTextParameter("SchematicPath", "CirPath", "The path of the loaded circuit", GH_ParamAccess.item);
            pManager.AddGenericParameter("SharedData", "dataset", "The data shared with all the components", GH_ParamAccess.item);
            //pManager.AddGenericParameter("ConnectionNetList", "ConnectionNetList", "The list of all connnection nets included in the schematic", GH_ParamAccess.list);
            pManager.AddNumberParameter("ImageScalar", "Sc", "The scalar of the schematic image", GH_ParamAccess.item);
            pManager.AddNumberParameter("ImageWidth", "IW", "The width of the schematic", GH_ParamAccess.item);
            pManager.AddNumberParameter("ImageHeight", "IH", "The height of the schematic", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IsActive", "IA", "Display the components", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool btnClick = false;
            bool toExeCircuitLoad = false;
            string desp = "";
            string circuitOverview = "Load a circuit...";
            string schematicImagePath = "";
            double newWidth = 300.0;
            double scalar = 1;
            double newHeight = 214.0;
            string curr_dir = "";

            bool isActive = false;

            var currentDirectory = Directory.GetCurrentDirectory();
            schematicImagePath = currentDirectory + @"\Sources\schematic_placeholder.png";

            #region read the button click and decide whether execute the circuit load or not
            if (!DA.GetData(0, ref btnClick))
                return;

            if (!DA.GetData(1, ref curr_dir))
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
                isActive = true;

                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Create a new button with the information of the selected circuit
                        // Add the button to the list
                        string filename = openFileDialog.FileName;
                        string suffix = filename.Substring(filename.IndexOf('.') + 1, filename.Length - filename.IndexOf('.') - 1);
                        desp = filename.Substring(filename.LastIndexOf('\\') + 1, filename.IndexOf('.') - filename.LastIndexOf('\\') - 1);
                        schematicImagePath = filename.Substring(0, filename.IndexOf('.')) + ".png";

                        dataset.circuitPath = filename.Substring(0, filename.LastIndexOf('\\')+1);
                        dataset.circuitFileName = desp;

                        // Test if the selected circuit schematic is created with Fritzing or Eagle, more format will be supported
                        int type = -1;   // 1: Eagle file; 2: Fritzing file; 3: Cadence
                        switch (suffix)
                        {
                            case "sch":
                            case "netlist":
                                {
                                    // the selected schematic is created with Eagle
                                    // read *.sch, *.netlist, *.png

                                    Image img = Image.FromFile(schematicImagePath);
                                    int oriWidth = img.Width;
                                    int oriHeight = img.Height;
                                    scalar = newWidth / oriWidth;
                                    newHeight = oriHeight * scalar;
                                    type = 1;
                                }
                                break;
                            case "fz":
                            case "xml":
                                {
                                    // the selected schematic is created with Fritzing
                                    // read *.fz, *.xml, and *.png

                                    Image img = Image.FromFile(schematicImagePath);
                                    int oriWidth = img.Width;
                                    int oriHeight = img.Height;
                                    scalar = newWidth / oriWidth;
                                    newHeight = oriHeight * scalar;
                                    type = 2;
                                }
                                break;
                            case "csv":
                            case "xcon":
                                {
                                    // the selected schematic is created with Cadence
                                    // read *.csv, *.xcon, and *.pdf
                                    type = 3;
                                    // convert the xcon file into an xml file so that the code can extract the information from the xcon file
                                    // delete the attributes of the first tag
                                    string sourceFile = filename;
                                    string destFile = filename.Substring(0, filename.IndexOf('.')) + ".xml";
                                    System.IO.File.Copy(sourceFile, destFile, true);

                                    string text = File.ReadAllText(destFile);
                                    text = text.Remove(7, text.IndexOf('>') - 7);
                                    File.WriteAllText(destFile, text);

                                    filename = destFile;

                                    // Convert the pdf into an jpg using a third-party dll--GemBox.Pdf
                                    // Set license key to use GemBox.Pdf in Free mode.
                                    ComponentInfo.SetLicense("FREE-LIMITED-KEY");

                                    string pdfFile = filename.Substring(0, filename.IndexOf('.')) + ".pdf";
                                    string pngFile = filename.Substring(0, filename.IndexOf('.')) + ".png";
                                    using (PdfDocument document = PdfDocument.Load(pdfFile))
                                        // In order to achieve the conversion of a loaded PDF file to image,
                                        // we just need to save a PdfDocument object to desired image format.
                                        document.Save(pngFile);

                                    Image img = Image.FromFile(pngFile);
                                    int oriWidth = img.Width;
                                    int oriHeight = img.Height;
                                    
                                    // Crop the converted PDF to show the correct image portion
                                    int origX = 0, origY = 0, origW = 0, origH = 0;
                                    xmlHelper.CropImage(filename, pngFile, out origX, out origY, out origW, out origH, oriWidth, oriHeight);

                                    Bitmap target = new Bitmap((int)origW, (int)origH);
                                    string pngFileNew = filename.Substring(0, filename.IndexOf('.')) + "_new.png";
                                    scalar = newWidth / origW;
                                    newHeight = origH * scalar;

                                    if (!File.Exists(pngFileNew))
                                    {
                                        //Load image from file
                                        using (Image image = Image.FromFile(pngFile))
                                        {
                                            // Create a Graphics object to do the drawing, *with the new bitmap as the target*
                                            using (Graphics g = Graphics.FromImage(target))
                                            {
                                                // Draw the desired area of the original into the graphics object
                                                g.DrawImage(image, new Rectangle(0, 0, origW, origH),
                                                    new Rectangle(origX, origY, origW, origH), GraphicsUnit.Pixel);
                                                // Save the result
                                                target.Save(pngFileNew);
                                            }
                                        }
                                    }

                                    schematicImagePath = pngFileNew;
                                }
                                break;
                            default: break;
                        }

                        int num_part = xmlHelper.GetNumberOfParts(filename, type);
                        int num_trace = xmlHelper.GetNumberOfTraces(filename, type);

                        circuitOverview = desp + " (" + num_part.ToString() + " parts, " + num_trace.ToString() + " traces)";

                        dataset.part2DList = xmlHelper.GetPartsFromSchematic(filename, type, scalar);
                        dataset.part3DList = xmlHelper.GetParts3DFromSchematic(filename, type, scalar, dataset, curr_dir);
                        
                        dataset.connectionNetList = xmlHelper.GetConnectionNetFromSchematic(filename, type);
                        dataset.schematicType = type;
                        //string netlistXML = desp + ".xml";

                        //int num_components = xmlHelper.GetNumberOfComponents(netlistXML);
                        //int num_traces = xmlHelper.GetNumberOfTraces(netlistXML);

                        //#region Add the imported circuit to the user intent list as a button
                        //Button userIntentBtn = new Button();
                        //userIntentBtn.Name = desp;      // ID starts with "1", aligned with the UserIntentID in the XML file
                        //userIntentBtn.Text = desp + "    (" + num_components.ToString() + " parts; " + num_traces.ToString() + " connections)";
                        //userIntentBtn.Width = 430;
                        //userIntentBtn.Height = 40;
                        //userIntentBtn.ForeColor = Color.FromArgb(61, 61, 61);
                        //userIntentBtn.BackColor = Color.FromArgb(245, 245, 245);
                        //userIntentBtn.FlatStyle = FlatStyle.Flat;
                        //userIntentBtn.FlatAppearance.BorderSize = 0;
                        //userIntentBtn.Click += UserIntentBtn_Click;

                        //this.circuitSelectionPanel.Controls.Add(userIntentBtn);
                        //#endregion

                        //#region Generate schematic.xml based on the selected fritzing file
                        //List<string> allCircuitNames = new List<string>();
                        //foreach (Button cirBtn in this.circuitSelectionPanel.Controls)
                        //{
                        //    allCircuitNames.Add(cirBtn.Name);
                        //}
                        //xmlHelper.CreateSchematicFromFritzingFiles(allCircuitNames);
                        //#endregion

                        //controller.readCircuitFiles(filename);


                        #region automatically place all the electrical parts in the model

                        // deselect all selected objects
                        myDoc.Objects.UnselectAll();

                        #region Step 0: Ask the user to select the target Brep if the body is not selected

                        if (myDoc.Objects.Find(currBody.objID) == null)
                        {
                            // the model body has not been selected or the model body has been transformed 

                            ObjRef objSel_ref;
                            Guid selObjId = Guid.Empty;
                            var rc = RhinoGet.GetOneObject("Select a model (brep)", false, ObjectType.AnyObject, out objSel_ref);
                            if (rc == Rhino.Commands.Result.Success)
                            {
                                selObjId = objSel_ref.ObjectId;
                                currBody.objID = selObjId;
                                ObjRef currObj = new ObjRef(selObjId);
                                currBody.bodyBrep = currObj.Brep();
                            }
                        }

                        #endregion

                        #region Step 0.1: create a new layer if not exist to store all parts

                        if (myDoc.Layers.FindName("Parts") == null)
                        {
                            // create a new layer named "Parts"
                            string layer_name = "Parts";
                            Layer p_ly = myDoc.Layers.CurrentLayer;

                            Layer new_ly = new Layer();
                            new_ly.Name = layer_name;
                            new_ly.ParentLayerId = p_ly.ParentLayerId;
                            new_ly.Color = Color.Beige;

                            int index = myDoc.Layers.Add(new_ly);

                            myDoc.Layers.SetCurrentLayerIndex(index, true);

                            if (index < 0) return;
                        }
                        else
                        {
                            int index = myDoc.Layers.FindName("Parts").Index;
                            myDoc.Layers.SetCurrentLayerIndex(index, true);
                        }

                        #endregion

                        // show the processing warning window
                        prcWin.Show();

                        foreach (Part3D temp in dataset.part3DList)
                        {
                            myDoc.Objects.UnselectAll();
                            List<string> connectedPartNames = new List<string>();
                            string ppName = temp.PartName.Substring(temp.PartName.IndexOf('-') + 1, temp.PartName.Length - temp.PartName.IndexOf('-') - 1);

                            foreach (ConnectionNet cnet in dataset.connectionNetList)
                            {
                                bool isConnected = false;

                                foreach (PartPinPair ppp in cnet.PartPinPairs)
                                {
                                    if (ppp.PartName.Equals(ppName))
                                    {
                                        // all the other parts in this connection net should be added to connectedParts list
                                        isConnected = true;
                                        break;
                                    }
                                }

                                if (isConnected)
                                {
                                    foreach (PartPinPair ppp in cnet.PartPinPairs)
                                    {
                                        if (!ppp.PartName.Equals(ppName))
                                        {
                                            connectedPartNames.Add(ppp.PartName);
                                        }
                                    }
                                }
                            }

                            Point3d unknownPos = new Point3d(0, 0, 0);

                            int connectDeoplyedPartNum = 0;

                            foreach (string cn in connectedPartNames)
                            {
                                foreach (Part3D p in dataset.part3DList)
                                {
                                    string pName = p.PartName.Substring(p.PartName.IndexOf('-') + 1, p.PartName.Length - p.PartName.IndexOf('-') - 1);

                                    if (cn.Equals(pName) && p.IsDeployed)
                                    {
                                        connectDeoplyedPartNum++;
                                        unknownPos += p.PartPos;
                                    }
                                }
                            }

                            if (connectDeoplyedPartNum == 1)
                            {
                                // only one connected part is deployed
                                Transform moveOnePart = Transform.Rotation(30.0 / 180 * Math.PI, new Vector3d(0, 0, 1), myDoc.Objects.Find(currBody.objID).Geometry.GetBoundingBox(true).Center);
                                unknownPos.Transform(moveOnePart);
                            }
                            else if(connectDeoplyedPartNum == 0)
                            {
                                // no one connected
                                unknownPos = currBody.bodyBrep.GetBoundingBox(true).Center + new Point3d(0, 0, 30);
                            }
                            else
                            {
                                // more than one connected parts are deployed
                                unknownPos = unknownPos / connectedPartNames.Count;
                            }
                            temp.PartPos = unknownPos;
                            Point3d knownPos = currBody.bodyBrep.ClosestPoint(unknownPos);



                            #region deploy the undeployed part

                            if (temp.ModelPath == "")
                            {
                                // the electrical component does not have related 3D model
                                // only add socket breps

                                partSocket tempSocket = new partSocket();
                                tempSocket.PrtName = temp.PartName;
                                tempSocket.SocketBrep = new Brep();
                                currBody.partSockets.Add(tempSocket);
                               

                            }
                            else
                            {
                                #region Step 1: import the STL of the 3D model and position it 40 mm above the 3D model

                                Point3d obj_pos = currBody.bodyBrep.GetBoundingBox(true).Center;
                                Point3d new_ori_pos = new Point3d(obj_pos.X, obj_pos.Y, obj_pos.Z + 40);

                                string script = string.Format("_-Import \"{0}\" _Enter", temp.ModelPath);
                                Rhino.RhinoApp.RunScript(script, false);

                                BoundingBox partBoundingBox = new BoundingBox();
                                Guid recoverPartPosID = Guid.Empty;
                                Guid importedObjID = Guid.Empty;

                                int oldIdx = -1;
                                foreach (Part3D prt3D in dataset.part3DList)
                                {
                                    if (prt3D.PartName.Equals(temp.PartName))
                                    {
                                        oldIdx = dataset.part3DList.IndexOf(prt3D);
                                        break;
                                    }
                                }

                                dataset.part3DList.ElementAt(oldIdx).IsDeployed = true;

                                #region enumerate all the meshes in the imported STL file: update the pin positions accordingly and union all the meshes

                                if (myDoc.Objects.GetSelectedObjects(false, false).Count() > 1)
                                {
                                    List<double> vol_list = new List<double>();
                                    List<int> vol_idx_list = new List<int>();

                                    int pos = 0;
                                    foreach (RhinoObject rObj in myDoc.Objects.GetSelectedObjects(false, false))
                                    {
                                        double vGet = rObj.Geometry.GetBoundingBox(true).Volume;
                                        vol_list.Add(vGet);

                                        vol_idx_list.Add(pos);
                                        pos++;
                                    }

                                    List<int> idx_list = new List<int>();

                                    var query = vol_list.GroupBy(x => x)
                                              .Where(g => g.Count() > 1)
                                              .Select(y => y.Key)
                                              .ToList();

                                    if (query.Count != 0)
                                    {
                                        double target = query.ElementAt(0);

                                        foreach (double var in vol_list)
                                        {
                                            if (var == target)
                                            {
                                                int idx = vol_list.IndexOf(var);
                                                idx_list.Add(idx);
                                            }
                                        }

                                        foreach (int i in idx_list)
                                        {
                                            int obj_idx = vol_idx_list.ElementAt(i);
                                            RhinoObject robj = myDoc.Objects.GetSelectedObjects(false, false).ElementAt(obj_idx);

                                            Point3d new_pin_pos = robj.Geometry.GetBoundingBox(true).Center;

                                            Vector3d real_vect = temp.PartPos - new_pin_pos;

                                            int target_idx = -1;
                                            double angle = double.MaxValue;
                                            Point3d target_pos = new Point3d();

                                            foreach (Pin pin_info in temp.Pins)
                                            {
                                                Point3d pin_pos = pin_info.Pos;
                                                Vector3d test_vect = temp.PartPos - pin_pos;

                                                #region find the correct pin and update the pin position
                                                double an = Vector3d.VectorAngle(real_vect, test_vect);
                                                if (an <= angle)
                                                {
                                                    angle = an;
                                                    target_idx = temp.Pins.IndexOf(pin_info);
                                                    target_pos = pin_pos;
                                                }

                                                #endregion
                                            }

                                            temp.Pins.ElementAt(target_idx).Pos = target_pos;
                                            dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(target_idx).Pos = target_pos;
                                        }
                                    }

                                    #region boolean union all the meshes together
                                    List<Mesh> importedMeshes = new List<Mesh>();
                                    foreach (RhinoObject robj in myDoc.Objects.GetSelectedObjects(false, false))
                                    {
                                        Mesh m = (Mesh)robj.Geometry;
                                        importedMeshes.Add(m);
                                    }
                                    var final_mesh = Mesh.CreateBooleanUnion(importedMeshes);
                                    var obj = final_mesh[0];
                                    importedObjID = myDoc.Objects.AddMesh(obj);

                                    // delete the original meshes
                                    foreach (RhinoObject robj in myDoc.Objects.GetSelectedObjects(false, false))
                                    {
                                        Guid objID = robj.Attributes.ObjectId;
                                        myDoc.Objects.Delete(objID, true);
                                    }

                                    Point3d ori_pos = obj.GetBoundingBox(true).Center;
                                    Vector3d resetPos = new_ori_pos - ori_pos;

                                    var xform = Transform.Translation(resetPos);
                                    recoverPartPosID = myDoc.Objects.Transform(importedObjID, xform, true);

                                    partBoundingBox = myDoc.Objects.Find(recoverPartPosID).Geometry.GetBoundingBox(true);

                                    // update the part position and pin positions, both selPrt and dataset's part3Dlist
                                    Point3d p_pos = temp.PartPos;
                                    p_pos.Transform(xform);
                                    temp.PartPos = p_pos;
                                    dataset.part3DList.ElementAt(oldIdx).PartPos = p_pos;

                                    foreach (Pin pin_info in temp.Pins)
                                    {
                                        Point3d pin_pos = pin_info.Pos;

                                        pin_pos.Transform(xform);

                                        int idx = temp.Pins.IndexOf(pin_info);
                                        temp.Pins.ElementAt(idx).Pos = pin_pos;

                                        dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = pin_pos;
                                    }

                                    myDoc.Views.Redraw();
                                    #endregion
                                }

                                #endregion
                                else
                                {
                                    foreach (var obj in myDoc.Objects.GetSelectedObjects(false, false))
                                    {
                                        importedObjID = obj.Attributes.ObjectId;

                                        Point3d ori_pos = obj.Geometry.GetBoundingBox(true).Center;
                                        Vector3d resetPos = new_ori_pos - ori_pos;

                                        var xform = Transform.Translation(resetPos);
                                        recoverPartPosID = myDoc.Objects.Transform(importedObjID, xform, true);

                                        partBoundingBox = myDoc.Objects.Find(recoverPartPosID).Geometry.GetBoundingBox(true);

                                        // update the part position and pin positions, both selPrt and dataset's part3Dlist
                                        Point3d p_pos = temp.PartPos;
                                        p_pos.Transform(xform);
                                        temp.PartPos = p_pos;
                                        dataset.part3DList.ElementAt(oldIdx).PartPos = p_pos;

                                        foreach (Pin pin_info in temp.Pins)
                                        {
                                            Point3d pin_pos = pin_info.Pos;

                                            pin_pos.Transform(xform);

                                            int idx = temp.Pins.IndexOf(pin_info);
                                            temp.Pins.ElementAt(idx).Pos = pin_pos;

                                            dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = pin_pos;
                                        }

                                        myDoc.Views.Redraw();
                                        break;
                                    }
                                }

                                #endregion

                                #region Step 2: Find all the points as candidate positions on the surface of the 3D model and ask the user the select one position

                                #region convert the selected brep into a mesh

                                var brep = currBody.bodyBrep;
                                if (brep == null) return;

                                var default_mesh_params = MeshingParameters.Default;
                                var minimal = MeshingParameters.Minimal;

                                var meshes = Mesh.CreateFromBrep(brep, default_mesh_params);
                                if (meshes == null || meshes.Length == 0) return;

                                var brep_mesh = new Mesh(); // the mesh of the currently selected body
                                foreach (var mesh in meshes)
                                    brep_mesh.Append(mesh);
                                brep_mesh.Faces.ConvertQuadsToTriangles();

                                #endregion

                                #region a better way to capture all position candidates -- using ray-tracing method to find all the points on the surface

                                BoundingBox meshBox = brep.GetBoundingBox(true);
                                double w = meshBox.Max.X - meshBox.Min.X;
                                double l = meshBox.Max.Y - meshBox.Min.Y;
                                double h = meshBox.Max.Z - meshBox.Min.Z;
                                currBody.surfPts.Clear();
                                double offset = 5; // offset the origin of the ray to include the points on the boundary of the bounding box



                                for (int i = 0; i < w; i++)
                                {
                                    for (int j = 0; j < l; j++)
                                    {
                                        //for (int t = 0; t < h; t++)
                                        //{
                                        //    Point3d ptInSpace = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, t + meshBox.Min.Z);
                                        //    if (brep.ClosestPoint(ptInSpace).DistanceTo(ptInSpace) <= 0.5)
                                        //    {
                                        //        currBody.surfPts.Add(brep.ClosestPoint(ptInSpace));
                                        //    }
                                        //}

                                        Point3d ptInSpaceOri = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, meshBox.Min.Z - offset);
                                        Point3d ptInSpaceEnd = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, meshBox.Max.Z + offset);

                                        Line ray_ln = new Line(ptInSpaceOri, ptInSpaceEnd);
                                        Curve ray_crv = ray_ln.ToNurbsCurve();

                                        Curve[] overlapCrvs;
                                        Point3d[] overlapPts;

                                        Intersection.CurveBrep(ray_crv, currBody.bodyBrep, myDoc.ModelAbsoluteTolerance, out overlapCrvs, out overlapPts);

                                        if (overlapPts != null)
                                        {
                                            if (overlapPts.Count() != 0)
                                            {
                                                foreach (Point3d p in overlapPts)
                                                {
                                                    currBody.surfPts.Add(p);
                                                }
                                            }
                                        }

                                    }
                                }

                                for (int i = 0; i < w; i++)
                                {
                                    for (int t = 0; t < h; t++)
                                    {

                                        Point3d ptInSpaceOri = new Point3d(i + meshBox.Min.X, meshBox.Min.Y - offset, meshBox.Min.Z + t);
                                        Point3d ptInSpaceEnd = new Point3d(i + meshBox.Min.X, meshBox.Max.Y + offset, meshBox.Min.Z + t);

                                        Line ray_ln = new Line(ptInSpaceOri, ptInSpaceEnd);
                                        Curve ray_crv = ray_ln.ToNurbsCurve();

                                        Curve[] overlapCrvs;
                                        Point3d[] overlapPts;

                                        Intersection.CurveBrep(ray_crv, currBody.bodyBrep, myDoc.ModelAbsoluteTolerance, out overlapCrvs, out overlapPts);

                                        if (overlapPts != null)
                                        {
                                            if (overlapPts.Count() != 0)
                                            {
                                                foreach (Point3d p in overlapPts)
                                                {
                                                    if (currBody.surfPts.IndexOf(p) == -1)
                                                        currBody.surfPts.Add(p);
                                                }
                                            }
                                        }

                                        //myDoc.Objects.AddCurve(ray_crv, redAttribute);

                                    }
                                }

                                for (int j = 0; j < l; j++)
                                {
                                    for (int t = 0; t < h; t++)
                                    {

                                        Point3d ptInSpaceOri = new Point3d(meshBox.Min.X - offset, j + meshBox.Min.Y, meshBox.Min.Z + t);
                                        Point3d ptInSpaceEnd = new Point3d(meshBox.Max.X + offset, j + meshBox.Min.Y, meshBox.Min.Z + t);

                                        Line ray_ln = new Line(ptInSpaceOri, ptInSpaceEnd);
                                        Curve ray_crv = ray_ln.ToNurbsCurve();

                                        Curve[] overlapCrvs;
                                        Point3d[] overlapPts;

                                        Intersection.CurveBrep(ray_crv, currBody.bodyBrep, myDoc.ModelAbsoluteTolerance, out overlapCrvs, out overlapPts);

                                        if (overlapPts != null)
                                        {
                                            if (overlapPts.Count() != 0)
                                            {
                                                foreach (Point3d p in overlapPts)
                                                {
                                                    if (currBody.surfPts.IndexOf(p) == -1)
                                                        currBody.surfPts.Add(p);
                                                }
                                            }
                                        }

                                        //myDoc.Objects.AddCurve(ray_crv, redAttribute);

                                    }
                                }

                                #endregion

                                #region select the points on the surface and drop the part on that spot

                                while (true)
                                {
                                    #region Step 1: find the closest point on the selected object's surface

                                    Point3d ptOnSurf = ((Brep)myDoc.Objects.Find(currBody.objID).Geometry).ClosestPoint(knownPos);

                                    #endregion

                                    #region Step 2: get the normal direction at this point on the surface

                                    ObjRef currObjRef = new ObjRef(currBody.objID);
                                    Vector3d outwardDir = new Vector3d();

                                    Point3d closestPt = new Point3d();
                                    double u, v;
                                    ComponentIndex i;
                                    currBody.bodyBrep.ClosestPoint(ptOnSurf, out closestPt, out i, out u, out v, 2, out outwardDir);

                                    List<Brep> projectedBrep = new List<Brep>();
                                    projectedBrep.Add(currBody.bodyBrep);
                                    List<Point3d> projectPt = new List<Point3d>();
                                    projectPt.Add(closestPt);

                                    var intersectionPts = Intersection.ProjectPointsToBreps(projectedBrep, projectPt, outwardDir, myDoc.ModelAbsoluteTolerance);
                                    if (intersectionPts.Length == 1)
                                    {
                                        outwardDir = (-1) * outwardDir;
                                    }

                                    #endregion

                                    #region Step 3: move the imported part to the final location (make sure the part is entirely hidden in the model)

                                    double hideratio = 0.5;

                                    double partHeight = partBoundingBox.GetCorners().ElementAt(4).Z - partBoundingBox.GetCorners().ElementAt(0).Z;
                                    var embedTranslation = Transform.Translation(outwardDir * (-1) * partHeight * hideratio);
                                    Point3d new_part_pos = ptOnSurf;
                                    new_part_pos.Transform(embedTranslation);

                                    Vector3d moveVector = new_part_pos - new_ori_pos;
                                    Vector3d retractMoveVector = new_ori_pos - new_part_pos;
                                    var partTranslation = Transform.Translation(moveVector);
                                    var retractPartTraslation = Transform.Translation(retractMoveVector);
                                    Guid transit_ID = myDoc.Objects.Transform(recoverPartPosID, partTranslation, true);

                                    #endregion

                                    #region Step 4: create the truncated pyramid socket

                                    // create the solid box based on the bounding box before rotation
                                    partBoundingBox = myDoc.Objects.Find(transit_ID).Geometry.GetBoundingBox(true);

                                    // make the socket a bit larger than the part (currently, ~0.5mm bigger)
                                    //partBoundingBox.Inflate(0.1);
                                    Box socketBox = new Box(partBoundingBox);
                                    BoundingBox scktBoundingBox = socketBox.BoundingBox;
                                    Brep truncatedSocket = scktBoundingBox.ToBrep();

                                    myDoc.Views.Redraw();

                                    #endregion

                                    #region Step 5: rotate the part to the normal direction

                                    Vector3d startDirVector = new Vector3d(0, 0, 1);
                                    Vector3d endDirVector = outwardDir;
                                    var partRotation = Transform.Rotation(startDirVector, endDirVector, new_part_pos);
                                    var retractPartRotation = Transform.Rotation(endDirVector, startDirVector, new_part_pos);

                                    // no intersections so that the part can be added to this location inside the 3D model
                                    Guid socketBoxID = myDoc.Objects.AddBrep(truncatedSocket);
                                    // get the final socket box after rotation
                                    Guid finalSocketID = myDoc.Objects.Transform(socketBoxID, partRotation, true);
                                    // situate the part in the final position and orientation
                                    Guid finalPartPosID = myDoc.Objects.Transform(transit_ID, partRotation, true);

                                    // Extend the box a bit more outside the selected solid
                                    //Transform differenceMove = Transform.Translation(outwardDir * (partHeight / 2 - 0.1));
                                    //Guid extendedSocketBoxID = myDoc.Objects.Transform(finalSocketID, differenceMove, false);

                                    Brep currentSelectedBrep = currBody.bodyBrep;

                                    //#region test if the added part collides with other parts

                                    //bool isPosValid = true;

                                    //Brep socketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;

                                    //if (currBody.partSockets.Count > 0)
                                    //{
                                    //    List<Brep> exitSockets = new List<Brep>();
                                    //    foreach (partSocket prtSocket in currBody.partSockets)
                                    //    {
                                    //        exitSockets.Add(prtSocket.SocketBrep);
                                    //    }
                                    //    var soketOverlaps = Brep.CreateBooleanIntersection(new List<Brep> { socketBrep }, exitSockets, myDoc.ModelAbsoluteTolerance);

                                    //    if (soketOverlaps == null)
                                    //    {
                                    //        isPosValid = true;
                                    //    }
                                    //    else
                                    //    {
                                    //        if (soketOverlaps.Count() > 0)
                                    //        {
                                    //            isPosValid = false;
                                    //        }
                                    //        else
                                    //        {
                                    //            isPosValid = true;
                                    //        }
                                    //    }
                                    //}

                                    //#endregion


                                    //Brep toRemoveBrep = (Brep)myDoc.Objects.Find(extendedSocketBoxID).Geometry;
                                    //// Test if the added electrical component can be added and hiden in this position
                                    //Brep dupCurrentSelectedBrep = currentSelectedBrep.DuplicateBrep();
                                    //Brep dupToRemoveSocketBrep = toRemoveBrep.DuplicateBrep();
                                    //var leftObj = Brep.CreateBooleanDifference(dupToRemoveSocketBrep, dupCurrentSelectedBrep, myDoc.ModelAbsoluteTolerance);
                                    //myDoc.Objects.Delete(extendedSocketBoxID, true);

                                    //if (leftObj.Count() != 1)
                                    //{
                                    //    // revert the position of the imported part model
                                    //    Guid retractFinalPartPosID = myDoc.Objects.Transform(finalPartPosID, retractPartRotation, true);
                                    //    recoverPartPosID = myDoc.Objects.Transform(retractFinalPartPosID, retractPartTraslation, true);
                                    //    myDoc.Objects.Delete(finalSocketID, true);
                                    //    myDoc.Views.Redraw();
                                    //    continue;
                                    //}
                                    //else
                                    //{

                                    if (true)
                                    {
                                        // confirm that the part is added to the model
                                        partSocket tempSocket = new partSocket();
                                        tempSocket.PrtName = temp.PartName;
                                        tempSocket.SocketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;

                                        bool isExistSocket = false;
                                        int socketIdx = -1;
                                        foreach (partSocket prtS in currBody.partSockets)
                                        {
                                            if (prtS.PrtName.Equals(tempSocket.PrtName))
                                            {
                                                isExistSocket = true;
                                                socketIdx = currBody.partSockets.IndexOf(prtS);
                                                break;
                                            }
                                        }

                                        if (!isExistSocket)
                                        {
                                            currBody.partSockets.Add(tempSocket);
                                        }
                                        else
                                        {
                                            currBody.partSockets.ElementAt(socketIdx).SocketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;
                                        }


                                        // draw the normal for part rotation and flip manipulations
                                        //Line normalLine = new Line(new_part_pos, outwardDir * partHeight);
                                        //if (!normalRedLineID.Equals(Guid.Empty))
                                        //{
                                        //    myDoc.Objects.Delete(normalRedLineID, true);
                                        //}
                                        //normalRedLineID = myDoc.Objects.AddLine(normalLine, redAttribute);
                                        //myDoc.Views.Redraw();
                                        //currBody.deployedEmbedType.Add(embedType);

                                        // update the part's Guid and pin information since the part is added to the scene

                                        dataset.part3DList.ElementAt(oldIdx).PrtID = finalPartPosID;
                                        dataset.part3DList.ElementAt(oldIdx).IsDeployed = true;
                                        dataset.part3DList.ElementAt(oldIdx).PartPos = new_part_pos;

                                        // translation: partTranslation
                                        // rotation: partRotation
                                        foreach (Pin p in dataset.part3DList.ElementAt(oldIdx).Pins)
                                        {
                                            Point3d pin_pos = p.Pos;
                                            pin_pos.Transform(partTranslation);
                                            pin_pos.Transform(partRotation);

                                            int idx = dataset.part3DList.ElementAt(oldIdx).Pins.IndexOf(p);
                                            dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = pin_pos;
                                        }

                                        dataset.part3DList.ElementAt(oldIdx).Normal = outwardDir;

                                        dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Clear();
                                        dataset.part3DList.ElementAt(oldIdx).TransformSets.Clear();

                                        dataset.part3DList.ElementAt(oldIdx).TransformSets.Add(partTranslation);
                                        dataset.part3DList.ElementAt(oldIdx).TransformSets.Add(partRotation);

                                        dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Add(retractPartRotation);
                                        dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Add(retractPartTraslation);

                                        dataset.part3DList.ElementAt(oldIdx).Height = partHeight;

                                        myDoc.Objects.Delete(finalSocketID, true);
                                        myDoc.Views.Redraw();
                                        break;
                                    }

                                    //}

                                    #endregion
                                }

                                #endregion

                                #endregion
                            }

                            #endregion
                        }

                        prcWin.Hide();

                        #endregion
                    }
                    catch (SecurityException ex)
                    {
                        MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                        $"Details:\n\n{ex.StackTrace}");
                    }
                }

            }




            DA.SetData(0, count);
            DA.SetData(1, circuitOverview);
            DA.SetData(2, schematicImagePath);
            DA.SetData(3, dataset);

            //List<GH_ObjectWrapper> connectionNetResultLlist = new List<GH_ObjectWrapper>();
            //foreach (ConnectionNet cNet in connectionNetList)
            //{
            //    connectionNetResultLlist.Add(new GH_ObjectWrapper(cNet));
            //}
            //DA.SetDataList(4, connectionNetResultLlist);
            DA.SetData(4, scalar);
            DA.SetData(5, newWidth);
            DA.SetData(6, newHeight);

            DA.SetData(7, isActive);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b6703749-3531-4e3f-b42d-98a815de7279"); }
        }
    }
}
