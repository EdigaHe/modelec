using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace ModelecComponents
{
    public class NudgeBottomComponent : GH_Component
    {
        bool testBtnClick;
        RhinoDoc myDoc;
        TargetBody currBody;
        SharedData dataset;
        ProcessingWindow prcWin;
        double step;

        /// <summary>
        /// Initializes a new instance of the NudgeBottomComponent class.
        /// </summary>
        public NudgeBottomComponent()
          : base("NudgeBottomComponent", "NBottom",
              "Down nudge the part",
              "ModElec", "PartOperations")
        {
            testBtnClick = false;
            myDoc = RhinoDoc.ActiveDoc;
            step = 0.2;

            currBody = TargetBody.Instance;
            dataset = SharedData.Instance;
            prcWin = new ProcessingWindow();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("SetPosBtnClicked", "BC", "Set the position for the selected part", GH_ParamAccess.item);
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
            bool toExePrtNudge = false;
            Part3D selPrt = new Part3D();

            #region read the button click and decide whether execute the part position set or not
            if (!DA.GetData(0, ref btnClick))
                return;

            if (!DA.GetData(1, ref selPrt))
                return;

            if (!btnClick && testBtnClick && selPrt.PartName != "")
            {
                toExePrtNudge = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }

            #endregion

            if (toExePrtNudge)
            {
                bool isDeployedReal = false;
                foreach (Part3D prt in dataset.part3DList)
                {
                    if (prt.PartName.Equals(selPrt.PartName))
                    {
                        isDeployedReal = prt.IsDeployed;
                        break;
                    }
                }

                if (!isDeployedReal)
                {
                    // the part has not been deployed
                }
                else
                {
                    int prt_idx = -1;
                    foreach (Part3D prt3D in dataset.part3DList)
                    {
                        if (prt3D.PartName.Equals(selPrt.PartName))
                        {
                            prt_idx = dataset.part3DList.IndexOf(prt3D);
                        }
                    }

                    #region Step 1: find the new position candidate on the model surface

                    Point3d currPos = myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center;
                    Point3d currProjPos = currBody.bodyBrep.ClosestPoint(currPos);

                    Vector3d outwardDir0 = new Vector3d();
                    double u0, v0;
                    ComponentIndex ii0;
                    Point3d closestPt0 = new Point3d();
                    currBody.bodyBrep.ClosestPoint(currProjPos, out closestPt0, out ii0, out u0, out v0, 2, out outwardDir0);
                    Plane plane_pt = new Plane(currProjPos, outwardDir0);


                    Point3d newPos = currPos - plane_pt.YAxis / plane_pt.YAxis.Length * step;
                    Point3d newCandPos = currBody.bodyBrep.ClosestPoint(newPos);

                    #endregion

                    #region Step 1.5: get the normal direction at this point on the surface

                    Vector3d outwardDir = new Vector3d();

                    Point3d closestPt = new Point3d();
                    double u, v;
                    ComponentIndex ii;
                    currBody.bodyBrep.ClosestPoint(newCandPos, out closestPt, out ii, out u, out v, 2, out outwardDir);

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

                    int socketIdx = -1;
                    foreach (partSocket prtS in currBody.partSockets)
                    {
                        if (prtS.PrtName.Equals(selPrt.PartName))
                        {
                            socketIdx = currBody.partSockets.IndexOf(prtS);
                        }
                    }

                    #region Step 2: move the part to the new position

                    // First, reverse self-rotation and the exposure
                    Point3d old_prt_pos = dataset.part3DList.ElementAt(prt_idx).PartPos;

                    Vector3d prt_normal = dataset.part3DList.ElementAt(prt_idx).Normal;
                    double prt_height = dataset.part3DList.ElementAt(prt_idx).Height;
                    double prt_exp = dataset.part3DList.ElementAt(prt_idx).ExposureLevel;

                    // Reserve the self-rotation

                    Transform selfrotTrans = Transform.Rotation((-1) * dataset.part3DList.ElementAt(prt_idx).RotationAngle / 180.0 * Math.PI,
                                                            dataset.part3DList.ElementAt(prt_idx).Normal, old_prt_pos);
                    Guid tempID = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, selfrotTrans, true);
                    dataset.part3DList.ElementAt(prt_idx).PrtID = tempID;
                    old_prt_pos.Transform(selfrotTrans);

                    #region update the pins

                    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                    {
                        Point3d temp_Pt = p.Pos;

                        temp_Pt.Transform(selfrotTrans);

                        int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = temp_Pt;
                    }

                    #endregion

                    #region update the socket

                    Brep prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(selfrotTrans);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion


                    Vector3d reverseNormalVector = dataset.part3DList.ElementAt(prt_idx).Normal * (-1);

                    Transform resetTrans = Transform.Translation(prt_normal * (-1) * prt_exp * prt_height);
                    Guid newIDTemp = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, resetTrans, true);
                    dataset.part3DList.ElementAt(prt_idx).PrtID = newIDTemp;

                    old_prt_pos.Transform(resetTrans);

                    #region update the pins

                    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                    {
                        Point3d tempPt = p.Pos;

                        tempPt.Transform(resetTrans);

                        int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = tempPt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(resetTrans);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion

                    // Second, reverse the flip
                    Transform flipTrans = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).Normal, reverseNormalVector, old_prt_pos);
                    if (dataset.part3DList.ElementAt(prt_idx).IsFlipped)
                    {
                        Guid newID = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, flipTrans, true);
                        dataset.part3DList.ElementAt(prt_idx).PrtID = newID;
                        old_prt_pos.Transform(flipTrans);

                        #region update the pins

                        foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                        {
                            Point3d tempPt = p.Pos;

                            tempPt.Transform(flipTrans);

                            int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                            dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = tempPt;
                        }

                        #endregion

                        #region update the socket

                        prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        prtBrep.Transform(flipTrans);
                        currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                        #endregion
                    }

                    // Third, transform back to the standard position and orientation 

                    foreach (Transform t in dataset.part3DList.ElementAt(prt_idx).TransformReverseSets)
                    {
                        Guid transID = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, t, true);
                        dataset.part3DList.ElementAt(prt_idx).PrtID = transID;
                        old_prt_pos.Transform(t);

                        #region update the pins

                        foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                        {
                            Point3d tempPt = p.Pos;

                            tempPt.Transform(t);

                            int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                            dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = tempPt;
                        }

                        #endregion

                        #region update the socket

                        prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        prtBrep.Transform(t);
                        currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                        #endregion
                    }

                    // Fourth, transform to the new position and orientation

                    double hideratio = 0.5;
                    var embedTranslation = Transform.Translation(outwardDir * (-1) * prt_height * hideratio);
                    Point3d new_part_pos = newCandPos;
                    new_part_pos.Transform(embedTranslation);

                    //Point3d new_ori_pos = myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center;
                    Point3d new_ori_pos = old_prt_pos;
                    Vector3d moveVector = new_part_pos - new_ori_pos;
                    Vector3d retractMoveVector = new_ori_pos - new_part_pos;
                    var partTranslation = Transform.Translation(moveVector);
                    var retractPartTraslation = Transform.Translation(retractMoveVector);
                    Guid transit_ID = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, partTranslation, true);
                    old_prt_pos.Transform(partTranslation);

                    Vector3d startDirVector = new Vector3d(0, 0, 1);
                    Vector3d endDirVector = outwardDir;
                    var partRotation = Transform.Rotation(startDirVector, endDirVector, new_part_pos);
                    var retractPartRotation = Transform.Rotation(endDirVector, startDirVector, new_part_pos);
                    Guid rotate_ID = myDoc.Objects.Transform(transit_ID, partRotation, true);
                    old_prt_pos.Transform(partRotation);

                    dataset.part3DList.ElementAt(prt_idx).PrtID = rotate_ID;
                    dataset.part3DList.ElementAt(prt_idx).Normal = outwardDir;

                    #region update the pins

                    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                    {
                        Point3d tempPt = p.Pos;

                        tempPt.Transform(partTranslation);
                        tempPt.Transform(partRotation);

                        int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = tempPt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(partTranslation);
                    prtBrep.Transform(partRotation);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion

                    // Fifth, flip
                    Vector3d reverseNormalVector1 = dataset.part3DList.ElementAt(prt_idx).Normal * (-1);
                    Transform flipTrans1 = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).Normal, reverseNormalVector1, old_prt_pos);
                    if (dataset.part3DList.ElementAt(prt_idx).IsFlipped)
                    {
                        Guid newID = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, flipTrans1, true);
                        dataset.part3DList.ElementAt(prt_idx).PrtID = newID;
                        old_prt_pos.Transform(flipTrans1);

                        #region update the pins

                        foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                        {
                            Point3d tempPt = p.Pos;

                            tempPt.Transform(flipTrans1);

                            int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                            dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = tempPt;
                        }

                        #endregion

                        #region update the socket

                        prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        prtBrep.Transform(flipTrans1);
                        currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                        #endregion
                    }

                    // Sixth, set exposure
                    Transform setExpTrans = Transform.Translation(dataset.part3DList.ElementAt(prt_idx).Normal * prt_exp * prt_height);
                    Guid newID1 = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, setExpTrans, true);
                    dataset.part3DList.ElementAt(prt_idx).PrtID = newID1;
                    old_prt_pos.Transform(setExpTrans);

                    #region update the pins

                    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                    {
                        Point3d tempPt = p.Pos;

                        tempPt.Transform(setExpTrans);

                        int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = tempPt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(setExpTrans);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion

                    // Seventh, set self-rotation
                    Transform selfrotRecoverTrans = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).RotationAngle / 180.0 * Math.PI,
                                                                dataset.part3DList.ElementAt(prt_idx).Normal, old_prt_pos);
                    Guid tempRotID = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, selfrotRecoverTrans, true);
                    dataset.part3DList.ElementAt(prt_idx).PrtID = tempRotID;
                    old_prt_pos.Transform(selfrotRecoverTrans);

                    #region update the pins

                    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                    {
                        Point3d temp_Pt = p.Pos;

                        temp_Pt.Transform(selfrotRecoverTrans);

                        int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = temp_Pt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(selfrotRecoverTrans);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion

                    // Finally, update all the part information

                    dataset.part3DList.ElementAt(prt_idx).PartPos = old_prt_pos;

                    dataset.part3DList.ElementAt(prt_idx).TransformReverseSets.Clear();
                    dataset.part3DList.ElementAt(prt_idx).TransformSets.Clear();

                    dataset.part3DList.ElementAt(prt_idx).TransformSets.Add(partTranslation);
                    dataset.part3DList.ElementAt(prt_idx).TransformSets.Add(partRotation);
                    dataset.part3DList.ElementAt(prt_idx).TransformReverseSets.Add(retractPartRotation);
                    dataset.part3DList.ElementAt(prt_idx).TransformReverseSets.Add(retractPartTraslation);

                    myDoc.Views.Redraw();

                    dataset.currPart3D = dataset.part3DList.ElementAt(prt_idx);

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
            get { return new Guid("f3365534-2377-430e-9b8e-10515882075c"); }
        }
    }
}