using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class LoadCustomComponentModelComponent : GH_Component
    {
        bool testBtnClick;
        int count;
        SharedData dataset;

        RhinoDoc myDoc;
        TargetBody currBody;
        ProcessingWindow prcWin;

        /// <summary>
        /// Initializes a new instance of the LoadCustomComponentModelComponent class.
        /// </summary>
        public LoadCustomComponentModelComponent()
          : base("LoadCustomComponentModelComponent", "LoadCusComp",
              "Load custom 3D model for a specific electrical component",
              "ModElec", "Utility")
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
            pManager.AddBooleanParameter("LoadCustomModel", "LBtnClick", "click the button to load a custom 3D model", GH_ParamAccess.item);
            //pManager.AddTextParameter("CurrentDirectory", "CDir", "The directory the Grassshopper components is at", GH_ParamAccess.item);
            pManager.AddGenericParameter("Part3D", "P3D", "The ModElec part unit", GH_ParamAccess.item);
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
            bool toExeCustomModelLoad = false;
            //string curr_dir = "";
            Part3D selPart = new Part3D();

            #region read the button click and decide whether execute the circuit load or not
            if (!DA.GetData(0, ref btnClick))
                return;

            //if (!DA.GetData(1, ref curr_dir))
            //    return;

            if (!DA.GetData(1, ref selPart))
                return;

            if (!btnClick && testBtnClick && selPart.PartName != "")
            {
                toExeCustomModelLoad = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }

            #endregion

            if (toExeCustomModelLoad)
            {
                #region switch to the layer "Parts"

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

                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string modelPath = openFileDialog.FileName;
                    string modelXML = modelPath.Substring(0, modelPath.IndexOf('.')) + ".xml";

                    #region Step 1: update the corresponding part's model path

                    int idx = dataset.part3DList.IndexOf(dataset.part3DList.Find(x => x.PartName.Equals(selPart.PartName)));
                    dataset.part3DList.ElementAt(idx).Iscustom = true;
                    dataset.part3DList.ElementAt(idx).ModelPath = modelPath;

                    if(!dataset.part3DList.ElementAt(idx).PrtID.Equals(Guid.Empty))
                        myDoc.Objects.Delete(dataset.part3DList.ElementAt(idx).PrtID, true);
                    myDoc.Views.Redraw();

                    #endregion

                    #region Step 1.5: get the intermediate position 

                    Point3d old_prt_pos = new Point3d();
                    Vector3d prt_normal = dataset.part3DList.ElementAt(idx).Normal;
                    double prt_height = dataset.part3DList.ElementAt(idx).Height;
                    double prt_exp = dataset.part3DList.ElementAt(idx).ExposureLevel; ;


                    if (!dataset.part3DList.ElementAt(idx).PrtID.Equals(Guid.Empty))
                    {
                        old_prt_pos = dataset.part3DList.ElementAt(idx).PartPos;

                        // self-rotation
                        Transform selfrotTrans = Transform.Rotation((-1) * dataset.part3DList.ElementAt(idx).RotationAngle / 180.0 * Math.PI,
                                                               dataset.part3DList.ElementAt(idx).Normal, old_prt_pos);
                        old_prt_pos.Transform(selfrotTrans);

                        // exposure
                        Vector3d reverseNormalVector = dataset.part3DList.ElementAt(idx).Normal * (-1);
                        Transform resetTrans = Transform.Translation(prt_normal * (-1) * prt_exp * prt_height);
                        old_prt_pos.Transform(resetTrans);

                        // flip
                        Transform flipTrans = Transform.Rotation(dataset.part3DList.ElementAt(idx).Normal, reverseNormalVector, old_prt_pos);
                        if (dataset.part3DList.ElementAt(idx).IsFlipped)
                        {
                            old_prt_pos.Transform(flipTrans);
                        }

                        var offset = Transform.Translation(dataset.part3DList.ElementAt(idx).Normal * dataset.part3DList.ElementAt(idx).Height / 2);
                        old_prt_pos.Transform(offset);
                        // old_prt_pos is the new intermediate position
                    }
                    else
                    {
                        #region initialize the final position of the model

                        myDoc.Objects.UnselectAll();
                        List<string> connectedPartNames = new List<string>();
                        string ppName = dataset.part3DList.ElementAt(idx).PartName.Substring(dataset.part3DList.ElementAt(idx).PartName.IndexOf('-') + 1,
                                                dataset.part3DList.ElementAt(idx).PartName.Length - dataset.part3DList.ElementAt(idx).PartName.IndexOf('-') - 1);

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
                        else if (connectDeoplyedPartNum == 0)
                        {
                            // no one connected
                            unknownPos = currBody.bodyBrep.GetBoundingBox(true).Center + new Point3d(0, 0, 30);
                        }
                        else
                        {
                            // more than one connected parts are deployed
                            unknownPos = unknownPos / connectedPartNames.Count;
                        }
                        dataset.part3DList.ElementAt(idx).PartPos = unknownPos;
                        old_prt_pos = currBody.bodyBrep.ClosestPoint(unknownPos);

                        #endregion
                    }

                    #endregion

                    #region Step 2: import the new custom 3D model

                    Point3d obj_pos = currBody.bodyBrep.GetBoundingBox(true).Center;
                    Point3d new_ori_pos = new Point3d(obj_pos.X, obj_pos.Y, obj_pos.Z + 40);

                    string script = string.Format("_-Import \"{0}\" _Enter", dataset.part3DList.ElementAt(idx).ModelPath);
                    Rhino.RhinoApp.RunScript(script, false);

                    //update all the pins
                    string filedir = modelXML;
                    XElement netList = XElement.Load($"{filedir}");
                    IEnumerable<XElement> pinElements = from item in netList.Descendants("pin")
                                                               select item;
                    foreach (XElement e in pinElements)
                    {
                        string p_name = e.Descendants("name").ElementAt(0).Value;
                        Pin pin = dataset.part3DList.ElementAt(idx).Pins.Find(x => x.PinName.Equals(p_name));
                        int p_idx = dataset.part3DList.ElementAt(idx).Pins.IndexOf(pin);
                        double p_x = Double.Parse(e.Descendants("x").ElementAt(0).Value);
                        double p_y = Double.Parse(e.Descendants("y").ElementAt(0).Value);
                        double p_z = Double.Parse(e.Descendants("z").ElementAt(0).Value);
                        Point3d p_new_pos = new Point3d(p_x, p_y, p_z);
                        dataset.part3DList.ElementAt(idx).Pins.ElementAt(p_idx).Pos = p_new_pos;
                    }


                    BoundingBox partBoundingBox = new BoundingBox();
                    Guid recoverPartPosID = Guid.Empty;
                    Guid importedObjID = Guid.Empty;

                    foreach (var obj in myDoc.Objects.GetSelectedObjects(false, false))
                    {
                        importedObjID = obj.Attributes.ObjectId;

                        Point3d ori_pos = obj.Geometry.GetBoundingBox(true).Center;
                        Vector3d resetPos = new_ori_pos - ori_pos;

                        var xform = Transform.Translation(resetPos);
                        recoverPartPosID = myDoc.Objects.Transform(importedObjID, xform, true);

                        foreach(Pin p in dataset.part3DList.ElementAt(idx).Pins)
                        {
                            Point3d pos = p.Pos;
                            int p_idx = dataset.part3DList.ElementAt(idx).Pins.IndexOf(p);
                            pos.Transform(xform);
                            dataset.part3DList.ElementAt(idx).Pins.ElementAt(p_idx).Pos = pos;
                        }

                        partBoundingBox = myDoc.Objects.Find(recoverPartPosID).Geometry.GetBoundingBox(true);
                        double newHeight = partBoundingBox.Max.Z - partBoundingBox.Min.Z;
                        Brep newSocketBrep = partBoundingBox.ToBrep();
                        dataset.part3DList.ElementAt(idx).Height = newHeight;
                        prt_height = newHeight;

                        var offset1 = Transform.Translation(dataset.part3DList.ElementAt(idx).Normal * (-1) * dataset.part3DList.ElementAt(idx).Height / 2);
                        old_prt_pos.Transform(offset1);

                        // update the part position and pin positions, both selPrt and dataset's part3Dlist
                        Point3d p_pos = old_prt_pos;
                        Vector3d setPos = p_pos - new_ori_pos;

                        var cenform = Transform.Translation(setPos);
                        Guid newPartPosID = myDoc.Objects.Transform(recoverPartPosID, cenform, true);
                        newSocketBrep.Transform(cenform);

                        dataset.part3DList.ElementAt(idx).TransformSets.Clear();
                        dataset.part3DList.ElementAt(idx).TransformSets.Add(cenform);

                        Vector3d newNormal = dataset.part3DList.ElementAt(idx).Normal;
                        var rotForm = Transform.Rotation(new Vector3d(0, 0, 1), newNormal, old_prt_pos);
                        Guid interPartPosID = myDoc.Objects.Transform(newPartPosID, rotForm, true);
                        dataset.part3DList.ElementAt(idx).TransformSets.Add(rotForm);
                        newSocketBrep.Transform(rotForm);

                        foreach (Pin p in dataset.part3DList.ElementAt(idx).Pins)
                        {
                            Point3d pos = p.Pos;
                            int p_idx = dataset.part3DList.ElementAt(idx).Pins.IndexOf(p);
                            pos.Transform(cenform);
                            pos.Transform(rotForm);
                            dataset.part3DList.ElementAt(idx).Pins.ElementAt(p_idx).Pos = pos;
                        }

                        var rotFormReverse = Transform.Rotation(newNormal, new Vector3d(0, 0, 1), old_prt_pos);
                        var cenFormReverse = Transform.Translation(setPos * (-1));

                        dataset.part3DList.ElementAt(idx).TransformReverseSets.Clear();
                        dataset.part3DList.ElementAt(idx).TransformReverseSets.Add(rotFormReverse);
                        dataset.part3DList.ElementAt(idx).TransformReverseSets.Add(cenFormReverse);


                        // apply flip back
                        Vector3d reverseNormalVector1 = dataset.part3DList.ElementAt(idx).Normal * (-1);
                        Transform flipTrans1 = Transform.Rotation(dataset.part3DList.ElementAt(idx).Normal, reverseNormalVector1, old_prt_pos);
                        dataset.part3DList.ElementAt(idx).PrtID = interPartPosID;
                        if (dataset.part3DList.ElementAt(idx).IsFlipped)
                        {
                            Guid newID = myDoc.Objects.Transform(dataset.part3DList.ElementAt(idx).PrtID, flipTrans1, true);
                            dataset.part3DList.ElementAt(idx).PrtID = newID;
                            old_prt_pos.Transform(flipTrans1);
                            newSocketBrep.Transform(flipTrans1);

                            foreach (Pin p in dataset.part3DList.ElementAt(idx).Pins)
                            {
                                Point3d pos = p.Pos;
                                int p_idx = dataset.part3DList.ElementAt(idx).Pins.IndexOf(p);
                                pos.Transform(flipTrans1);
                                dataset.part3DList.ElementAt(idx).Pins.ElementAt(p_idx).Pos = pos;
                            }
                        }


                        // apply exposure back
                        Transform setExpTrans = Transform.Translation(dataset.part3DList.ElementAt(idx).Normal * prt_exp * prt_height);
                        Guid newID1 = myDoc.Objects.Transform(dataset.part3DList.ElementAt(idx).PrtID, setExpTrans, true);
                        dataset.part3DList.ElementAt(idx).PrtID = newID1;
                        old_prt_pos.Transform(setExpTrans);
                        newSocketBrep.Transform(setExpTrans);

                        foreach (Pin p in dataset.part3DList.ElementAt(idx).Pins)
                        {
                            Point3d pos = p.Pos;
                            int p_idx = dataset.part3DList.ElementAt(idx).Pins.IndexOf(p);
                            pos.Transform(setExpTrans);
                            dataset.part3DList.ElementAt(idx).Pins.ElementAt(p_idx).Pos = pos;
                        }

                        // apply self-rotation back
                        Transform selfrotRecoverTrans = Transform.Rotation(dataset.part3DList.ElementAt(idx).RotationAngle / 180.0 * Math.PI,
                                                               dataset.part3DList.ElementAt(idx).Normal, old_prt_pos);
                        Guid tempRotID = myDoc.Objects.Transform(dataset.part3DList.ElementAt(idx).PrtID, selfrotRecoverTrans, true);
                        dataset.part3DList.ElementAt(idx).PrtID = tempRotID;
                        old_prt_pos.Transform(selfrotRecoverTrans);
                        dataset.part3DList.ElementAt(idx).PartPos = old_prt_pos;
                        newSocketBrep.Transform(selfrotRecoverTrans);

                        foreach (Pin p in dataset.part3DList.ElementAt(idx).Pins)
                        {
                            Point3d pos = p.Pos;
                            int p_idx = dataset.part3DList.ElementAt(idx).Pins.IndexOf(p);
                            pos.Transform(selfrotRecoverTrans);
                            dataset.part3DList.ElementAt(idx).Pins.ElementAt(p_idx).Pos = pos;
                        }

                        // update the socket 
                        int socketIdx = -1;
                        foreach (partSocket prtS in currBody.partSockets)
                        {
                            if (prtS.PrtName.Equals(selPart.PartName))
                            {
                                socketIdx = currBody.partSockets.IndexOf(prtS);
                            }
                        }
                        
                        currBody.partSockets.ElementAt(socketIdx).SocketBrep = newSocketBrep;

                        myDoc.Views.Redraw();
                        break;
                    }
                    
                    #endregion

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
            get { return new Guid("b6443045-bc50-488c-bf1a-340f0e1f9d6d"); }
        }
    }
}