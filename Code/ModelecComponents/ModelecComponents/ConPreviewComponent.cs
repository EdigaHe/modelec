using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class ConPreviewComponent : GH_Component
    {
        RhinoDoc myDoc;
        SharedData dataset;
        TargetBody currBody;
        ObjectAttributes redAttribute;

        /// <summary>
        /// Initializes a new instance of the PartFlipComponent class.
        /// </summary>
        public ConPreviewComponent()
          : base("ConPreviewComponent", "ConPre",
              "Preview all the connections based on the imported circuit schematic",
              "ModElec", "TraceOperations")
        {
            myDoc = RhinoDoc.ActiveDoc;
            dataset = SharedData.Instance;
            currBody = TargetBody.Instance;

            int redIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material redMat = myDoc.Materials[redIndex];
            redMat.DiffuseColor = System.Drawing.Color.Red;
            redMat.Transparency = 0.3;
            redMat.SpecularColor = System.Drawing.Color.Red;
            redMat.CommitChanges();
            redAttribute = new ObjectAttributes();
            
            redAttribute.MaterialIndex = redIndex;
            redAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            redAttribute.ObjectColor = Color.Red;
            redAttribute.ColorSource = ObjectColorSource.ColorFromObject;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("ConnectionPreviewBtnClicked", "ConPreC", "preview all the connections", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// Get the pin position based on the part name and the pin name
        /// </summary>
        /// <param name="partName">the part name is something like a letter followed by a digit, no dash included</param>
        /// <param name="pinName">the pin name</param>
        /// <returns></returns>
        public Point3d GetPinPointByNames(string partName, string pinName)
        {
            Point3d result = new Point3d();

            switch (dataset.schematicType)
            {
                case 1:
                    {

                    }
                    break;

                case 2:
                    {
                        foreach (Part3D prt3D in dataset.part3DList)
                        {
                            string prt3DName = prt3D.PartName;

                            if (prt3DName.Equals(partName))
                            {
                                foreach (Pin p in prt3D.Pins)
                                {
                                    string p_id = p.PinName.Substring(0, p.PinName.IndexOf(' '));
                                    if (p_id.Equals(pinName))
                                    {
                                        result = p.Pos;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    break;
                case 3:
                    {
                        foreach (Part3D prt3D in dataset.part3DList)
                        {
                            string prt3DName = prt3D.PartName.Substring(prt3D.PartName.IndexOf('-') + 1, prt3D.PartName.Length - prt3D.PartName.IndexOf('-') - 1);

                            if (prt3DName.Equals(partName))
                            {
                                foreach (Pin p in prt3D.Pins)
                                {
                                    if (p.PinName.Equals(pinName))
                                    {
                                        result = p.Pos;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool btnClick = false;

            #region read the button click and decide whether execute the circuit load or not
            if (!DA.GetData(0, ref btnClick))
                return;

            #endregion


            if (btnClick)
            {
                //show all the connections based on the schematic

                #region Step 1: switch to the connection layer (creat one if not exist)

                if (myDoc.Layers.FindName("JumpWires") == null)
                {
                    // create a new layer named "JumpWires"
                    string layer_name = "JumpWires";
                    Layer p_ly = myDoc.Layers.CurrentLayer;

                    Layer new_ly = new Layer();
                    new_ly.Name = layer_name;
                    new_ly.ParentLayerId = p_ly.ParentLayerId;
                    new_ly.Color = Color.Red;

                    int index = myDoc.Layers.Add(new_ly);
                    myDoc.Layers.ElementAt(index).IsVisible = true;
                    myDoc.Layers.SetCurrentLayerIndex(index, true);
                    redAttribute.LayerIndex = index;

                    if (index < 0) return;

                    #region Step 2: generate all the connection curves based on the circuit schematic and update the dataset.traceOracle
                    foreach (ConnectionNet conNet in dataset.connectionNetList)
                    {
                        TraceCluster temp = new TraceCluster();

                        Point3d cc = new Point3d(0, 0, 0);

                        foreach (PartPinPair ppp in conNet.PartPinPairs)
                        {
                            // Obtain the pin's position in 3D
                            Point3d pinPt = GetPinPointByNames(ppp.PartName, ppp.PinName);

                            TraceClusterMember tcTemp = new TraceClusterMember();
                            tcTemp.PartName = ppp.PartName;
                            tcTemp.PinName = ppp.PinName;
                            tcTemp.Pos = pinPt;

                            cc += pinPt;
                            temp.ClusterMembers.Add(tcTemp);
                        }

                        cc = cc / conNet.PartPinPairs.Count;
                        temp.ClusterCenter = cc;

                        for(int i = 0; i < temp.ClusterMembers.Count; i++)
                        {
                            for(int j = i+1; j < temp.ClusterMembers.Count; j++)
                            {
                                JumpWire jw = new JumpWire();
                                jw.StartPrtName = temp.ClusterMembers.ElementAt(i).PartName;
                                jw.StartPinName = temp.ClusterMembers.ElementAt(i).PinName;
                                jw.EndPrtName = temp.ClusterMembers.ElementAt(j).PartName;
                                jw.EndPinName = temp.ClusterMembers.ElementAt(j).PinName;
                                Line l = new Line(temp.ClusterMembers.ElementAt(i).Pos, temp.ClusterMembers.ElementAt(j).Pos);
                                Curve c = l.ToNurbsCurve();
                                Guid c_id = myDoc.Objects.AddCurve(c,redAttribute);
                                jw.Crv = c;
                                jw.CrvID = c_id;

                                myDoc.Views.Redraw();

                                temp.Jumpwires.Add(jw);
                            }
                        }

                        dataset.traceOracle.Add(temp);
                    }
                    #endregion
                }
                else
                {
                    int index = myDoc.Layers.FindName("JumpWires").Index;
                    redAttribute.LayerIndex = index;
                    myDoc.Layers.ElementAt(index).IsVisible = true;
                    myDoc.Layers.SetCurrentLayerIndex(index, true);
                    //myDoc.Views.Redraw();

                    foreach (TraceCluster trC in dataset.traceOracle)
                    {
                        foreach (JumpWire jwOld in trC.Jumpwires)
                        {

                            myDoc.Objects.Delete(jwOld.CrvID, true);

                        }
                    }
                    myDoc.Views.Redraw();

                    // connection information has been added to the data set
                    dataset.traceOracle.Clear();

                    // create the oracle of the traces
                    foreach (ConnectionNet conNet in dataset.connectionNetList)
                    {
                        TraceCluster temp = new TraceCluster();

                        Point3d cc = new Point3d(0, 0, 0);

                        foreach (PartPinPair ppp in conNet.PartPinPairs)
                        {
                            // Obtain the pin's position in 3D
                            Point3d pinPt = GetPinPointByNames(ppp.PartName, ppp.PinName);

                            TraceClusterMember tcTemp = new TraceClusterMember();
                            tcTemp.PartName = ppp.PartName;
                            tcTemp.PinName = ppp.PinName;
                            tcTemp.Pos = pinPt;

                            cc += pinPt;
                            temp.ClusterMembers.Add(tcTemp);
                        }

                        cc = cc / conNet.PartPinPairs.Count;
                        temp.ClusterCenter = cc;

                        for (int i = 0; i < temp.ClusterMembers.Count; i++)
                        {
                            for (int j = i + 1; j < temp.ClusterMembers.Count; j++)
                            {
                                JumpWire jw = new JumpWire();
                                jw.StartPrtName = temp.ClusterMembers.ElementAt(i).PartName;
                                jw.StartPinName = temp.ClusterMembers.ElementAt(i).PinName;
                                jw.EndPrtName = temp.ClusterMembers.ElementAt(j).PartName;
                                jw.EndPinName = temp.ClusterMembers.ElementAt(j).PinName;
                                Line l = new Line(temp.ClusterMembers.ElementAt(i).Pos, temp.ClusterMembers.ElementAt(j).Pos);
                                Curve c = l.ToNurbsCurve();
                                Guid c_id = myDoc.Objects.AddCurve(c, redAttribute);
                                jw.Crv = c;
                                jw.CrvID = c_id;

                                myDoc.Views.Redraw();

                                temp.Jumpwires.Add(jw);
                            }
                        }

                        dataset.traceOracle.Add(temp);
                    }


                    #region update the jump wires

                    foreach (TraceCluster trC in dataset.traceOracle)
                    {
                        foreach(JumpWire jwOld in trC.Jumpwires)
                        {                          

                            //Part3D p3dStart = dataset.part3DList.Find(x => x.PartName.Substring(x.PartName.IndexOf('-') + 1, x.PartName.Length - x.PartName.IndexOf('-') - 1).Equals(jwOld.StartPrtName));
                            //Point3d newStartPos = p3dStart.Pins.Find(x => x.PinName.Equals(jwOld.StartPinName)).Pos;

                            Point3d newStartPos = GetPinPointByNames(jwOld.StartPrtName, jwOld.StartPinName);

                            //Part3D p3dEnd = dataset.part3DList.Find(x => x.PartName.Substring(x.PartName.IndexOf('-') + 1, x.PartName.Length - x.PartName.IndexOf('-') - 1).Equals(jwOld.EndPrtName));
                            //Point3d newEndPos = p3dEnd.Pins.Find(x => x.PinName.Equals(jwOld.EndPinName)).Pos;

                            Point3d newEndPos = GetPinPointByNames(jwOld.EndPrtName, jwOld.EndPinName);

                            Line l = new Line(newStartPos, newEndPos);
                            Curve c = l.ToNurbsCurve();
                            Guid c_id = myDoc.Objects.AddCurve(c, redAttribute);
                            myDoc.Objects.Delete(jwOld.CrvID, true);

                            // update the original jump wire

                            int idx = trC.Jumpwires.IndexOf(jwOld);
                            trC.Jumpwires.ElementAt(idx).Crv = c;
                            trC.Jumpwires.ElementAt(idx).CrvID = c_id;

                            if (trC.Jumpwires.ElementAt(idx).IsDeployed)
                            {
                                // remove the red jump wires
                                myDoc.Objects.Hide(trC.Jumpwires.ElementAt(idx).CrvID, true);
                            }
                        }
                    }
                    myDoc.Views.Redraw();

                    #endregion
                }

                #endregion
            }
            else
            {
                // hide all the connections based on the schematic
                if (myDoc.Layers.FindName("JumpWires") != null)
                {
                    myDoc.Layers.SetCurrentLayerIndex(0, true);
                    int index = myDoc.Layers.FindName("JumpWires").Index;
                    myDoc.Layers.ElementAt(index).IsVisible = false;
                    // switch to the first layer (the default layer)
                    //myDoc.Views.Redraw();
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
            get { return new Guid("e97092bb-fdaa-4e09-a5b7-2003911eeb45"); }
        }
    }
}