using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents.Helpers
{
    class PartValidationHelper
    {
        public PartValidationHelper() { }

        public Point3d FindCloesetPointFromSurface(Point3d target, Guid objID)
        {
            Point3d result = new Point3d(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue); // dumb point

            ObjRef dupObjRef = new ObjRef(objID);
            result = dupObjRef.Brep().ClosestPoint(target);

            return result;
        }

        public void validateParts()
        {
            SharedData dataset = SharedData.Instance;
            TargetBody currBody = TargetBody.Instance;
            RhinoDoc myDoc = RhinoDoc.ActiveDoc;

            bool isPartSelected = false;
            Part3D targetPart = new Part3D();

            bool isBodySelected = false;
            Guid targetBodyID = Guid.Empty;

           
            #region obtain the selected objects in the Rhino scene

            foreach (var obj in myDoc.Objects.GetSelectedObjects(false, false))
            {
                Guid selectedObjID = obj.Attributes.ObjectId;

                if (selectedObjID == currBody.objID)
                {
                    isBodySelected = true;
                    targetBodyID = selectedObjID;
                    break;
                }

                foreach (Part3D p3D in dataset.part3DList)
                {
                    if (p3D.PrtID == selectedObjID)
                    {
                        isPartSelected = true;
                        targetPart.ExposureLevel = p3D.ExposureLevel;
                        targetPart.Height = p3D.Height;
                        targetPart.IsDeployed = p3D.IsDeployed;
                        targetPart.IsFlipped = p3D.IsFlipped;
                        targetPart.ModelPath = p3D.ModelPath;
                        targetPart.Normal = p3D.Normal;
                        targetPart.PartName = p3D.PartName;
                        targetPart.PartPos = p3D.PartPos;
                        targetPart.Pins = new List<Pin>();
                        foreach (Pin p in p3D.Pins)
                        {
                            targetPart.Pins.Add(p);
                        }
                        targetPart.PrtID = p3D.PrtID;
                        targetPart.RotationAngle = p3D.RotationAngle;
                        targetPart.TransformReverseSets = new List<Transform>();
                        foreach (Transform tr in p3D.TransformReverseSets)
                        {
                            targetPart.TransformReverseSets.Add(tr);
                        }
                        targetPart.TransformSets = new List<Transform>();
                        foreach (Transform tr in p3D.TransformSets)
                        {
                            targetPart.TransformSets.Add(tr);
                        }

                        break;
                    }
                }


            }

            #endregion

            #region when the selected part is the electrical part and the body is not changed

            // Only check the selected part 
            if (isPartSelected)
            {
                if (myDoc.Layers.FindName("OriginalBody") == null)
                {
                    // not converted yet
                }
                else
                {
                    int index = myDoc.Layers.FindName("OriginalBody").Index;
                    myDoc.Objects.Find(currBody.objID).Attributes.LayerIndex = index;
                    myDoc.Layers.ElementAt(index).IsVisible = true;
                    myDoc.Objects.Show(currBody.objID, true);
                }

                dataset.isTraceGenerated = false;

                if (myDoc.Layers.FindName("ConvertedBody") == null)
                {
                    // the body has not ever been converted
                }
                else
                {
                    #region delete the old converted body

                    Guid id = currBody.convertedObjID;
                    myDoc.Objects.Delete(id, true);

                    #endregion

                    #region show the original body and delete all the automatically generated traces but keep manual traces

                    int index = myDoc.Layers.FindName("OriginalBody").Index;
                    myDoc.Layers.ElementAt(index).IsVisible = true;

                    List<int> pos = new List<int>();
                    foreach(Trace trc in dataset.deployedTraces)
                    {
                        if(trc.Type == 1)
                        {
                            myDoc.Objects.Delete(trc.TrID, true);
                            int idx = dataset.deployedTraces.IndexOf(trc);
                            pos.Add(idx);
                        }
                    }

                    pos.Sort((a, b) => b.CompareTo(a));

                    foreach(int i in pos)
                    {
                        dataset.deployedTraces.RemoveAt(i);
                    }
                    
                    // delete all the generated pads
                    foreach(Guid pad_id in dataset.pads)
                    {
                        myDoc.Objects.Delete(pad_id, true);
                    }
                    dataset.pads.Clear();

                    #endregion

                }

                #region Step 1: get the new position, socket, and the normal direction at the new position

                int socketIdx = -1;
            Brep socketOrigBrep = new Brep();

            foreach (partSocket prtS in currBody.partSockets)
            {
                if (prtS.PrtName.Equals(targetPart.PartName))
                {
                    socketIdx = currBody.partSockets.IndexOf(prtS);
                    socketOrigBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                }
            }

            Transform dynPrtTrans = new Transform();
            myDoc.Objects.Find(targetPart.PrtID).GetDynamicTransform(out dynPrtTrans);
            Point3d newCen = myDoc.Objects.Find(targetPart.PrtID).Geometry.GetBoundingBox(true).Center;
            newCen.Transform(dynPrtTrans);

            Point3d newPos = FindCloesetPointFromSurface(newCen, currBody.objID);

            Vector3d outwardDir = new Vector3d();

            Point3d closestPt = new Point3d();
            double u, v;
            ComponentIndex ii;
            currBody.bodyBrep.ClosestPoint(newPos, out closestPt, out ii, out u, out v, 2, out outwardDir);

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

                #region Step 2: reverse the self-rotation, exposure, and flip

                Point3d old_prt_pos = targetPart.PartPos;

                Vector3d prt_normal = targetPart.Normal;
                double prt_height = targetPart.Height;
                double prt_exp = targetPart.ExposureLevel;


                Transform selfrotTrans = Transform.Rotation((-1) * targetPart.RotationAngle / 180.0 * Math.PI, targetPart.Normal, old_prt_pos);
                Guid tempID = myDoc.Objects.Transform(targetPart.PrtID, selfrotTrans, true);
                targetPart.PrtID = tempID;
                old_prt_pos.Transform(selfrotTrans);

                #region update the pins

                foreach (Pin p in targetPart.Pins)
                {
                    Point3d temp_Pt = p.Pos;

                    temp_Pt.Transform(selfrotTrans);

                    int idx = targetPart.Pins.IndexOf(p);
                    targetPart.Pins.ElementAt(idx).Pos = temp_Pt;
                }

                #endregion

                #region update the socket

                Brep prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                prtBrep.Transform(selfrotTrans);
                currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                #endregion


                Vector3d reverseNormalVector = targetPart.Normal * (-1);

                Transform resetTrans = Transform.Translation(prt_normal * (-1) * prt_exp * prt_height);
                Guid newIDTemp = myDoc.Objects.Transform(targetPart.PrtID, resetTrans, true);
                targetPart.PrtID = newIDTemp;

                old_prt_pos.Transform(resetTrans);

                #region update the pins

                foreach (Pin p in targetPart.Pins)
                {
                    Point3d temp_Pt = p.Pos;

                    temp_Pt.Transform(resetTrans);

                    int idx = targetPart.Pins.IndexOf(p);
                    targetPart.Pins.ElementAt(idx).Pos = temp_Pt;
                }

                #endregion

                #region update the socket

                prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                prtBrep.Transform(resetTrans);
                currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                #endregion


                Transform flipTrans = Transform.Rotation(targetPart.Normal, reverseNormalVector, old_prt_pos);
                if (targetPart.IsFlipped)
                {
                    Guid newID = myDoc.Objects.Transform(targetPart.PrtID, flipTrans, true);
                    targetPart.PrtID = newID;
                    old_prt_pos.Transform(flipTrans);

                    #region update the pins

                    foreach (Pin p in targetPart.Pins)
                    {
                        Point3d temp_Pt = p.Pos;

                        temp_Pt.Transform(flipTrans);

                        int idx = targetPart.Pins.IndexOf(p);
                        targetPart.Pins.ElementAt(idx).Pos = temp_Pt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(flipTrans);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion
                }


                #endregion

                #region Step 3: transform back to the initial position and orientation when the part is imported

                foreach (Transform t in targetPart.TransformReverseSets)
                {
                    Guid transID = myDoc.Objects.Transform(targetPart.PrtID, t, true);
                    targetPart.PrtID = transID;
                    old_prt_pos.Transform(t);

                    #region update the pins

                    foreach (Pin p in targetPart.Pins)
                    {
                        Point3d temp_Pt = p.Pos;

                        temp_Pt.Transform(t);

                        int idx = targetPart.Pins.IndexOf(p);
                        targetPart.Pins.ElementAt(idx).Pos = temp_Pt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(t);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion
                }

                #endregion

                #region Step 4: transform to the new position and orientation

                double hideratio = 0.5;

                var embedTranslation = Transform.Translation(outwardDir * (-1) * prt_height * hideratio);
                Point3d new_part_pos = newPos;
                new_part_pos.Transform(embedTranslation);

                //Point3d new_ori_pos = myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center;
                Point3d new_ori_pos = old_prt_pos;
                Vector3d moveVector = new_part_pos - new_ori_pos;
                Vector3d retractMoveVector = new_ori_pos - new_part_pos;
                var partTranslation = Transform.Translation(moveVector);
                var retractPartTraslation = Transform.Translation(retractMoveVector);
                Guid transit_ID = myDoc.Objects.Transform(targetPart.PrtID, partTranslation, true);
                old_prt_pos.Transform(partTranslation);

                Vector3d startDirVector = new Vector3d(0, 0, 1);
                Vector3d endDirVector = outwardDir;
                var partRotation = Transform.Rotation(startDirVector, endDirVector, new_part_pos);
                var retractPartRotation = Transform.Rotation(endDirVector, startDirVector, new_part_pos);
                Guid rotate_ID = myDoc.Objects.Transform(transit_ID, partRotation, true);
                old_prt_pos.Transform(partRotation);

                targetPart.PrtID = rotate_ID;
                targetPart.Normal = outwardDir;

                #region update the pins

                foreach (Pin p in targetPart.Pins)
                {
                    Point3d temp_Pt = p.Pos;

                    temp_Pt.Transform(partTranslation);
                    temp_Pt.Transform(partRotation);

                    int idx = targetPart.Pins.IndexOf(p);
                    targetPart.Pins.ElementAt(idx).Pos = temp_Pt;
                }

                #endregion

                #region update the socket

                prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                prtBrep.Transform(partTranslation);
                prtBrep.Transform(partRotation);
                currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                #endregion

                #endregion

                #region Step 5: flip back, set the exposure, and set the self-rotation

                Vector3d reverseNormalVector1 = targetPart.Normal * (-1);
                Transform flipTrans1 = Transform.Rotation(targetPart.Normal, reverseNormalVector1, old_prt_pos);
                if (targetPart.IsFlipped)
                {
                    Guid newID = myDoc.Objects.Transform(targetPart.PrtID, flipTrans1, true);
                    targetPart.PrtID = newID;
                    old_prt_pos.Transform(flipTrans1);

                    #region update the pins

                    foreach (Pin p in targetPart.Pins)
                    {
                        Point3d temp_pt = p.Pos;

                        temp_pt.Transform(flipTrans1);

                        int idx = targetPart.Pins.IndexOf(p);
                        targetPart.Pins.ElementAt(idx).Pos = temp_pt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(flipTrans1);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion
                }


                Transform setExpTrans = Transform.Translation(targetPart.Normal * prt_exp * prt_height);
                Guid newID1 = myDoc.Objects.Transform(targetPart.PrtID, setExpTrans, true);
                targetPart.PrtID = newID1;
                old_prt_pos.Transform(setExpTrans);

                #region update the pins

                foreach (Pin p in targetPart.Pins)
                {
                    Point3d temp_pt = p.Pos;

                    temp_pt.Transform(setExpTrans);

                    int idx = targetPart.Pins.IndexOf(p);
                    targetPart.Pins.ElementAt(idx).Pos = temp_pt;
                }

                #endregion

                #region update the socket

                prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                prtBrep.Transform(setExpTrans);
                currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                #endregion


                Transform selfrotRecoverTrans = Transform.Rotation(targetPart.RotationAngle / 180.0 * Math.PI, targetPart.Normal, old_prt_pos);
                Guid tempRotID = myDoc.Objects.Transform(targetPart.PrtID, selfrotRecoverTrans, true);
                targetPart.PrtID = tempRotID;
                old_prt_pos.Transform(selfrotRecoverTrans);

                #region update the pins

                foreach (Pin p in targetPart.Pins)
                {
                    Point3d temp_Pt = p.Pos;

                    temp_Pt.Transform(selfrotRecoverTrans);

                    int idx = targetPart.Pins.IndexOf(p);
                    targetPart.Pins.ElementAt(idx).Pos = temp_Pt;
                }

                #endregion

                #region update the socket

                prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                prtBrep.Transform(selfrotRecoverTrans);
                currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                #endregion



                #endregion

                #region check if sockets overlap

                bool isPosValid = true;
                Brep socketBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;

                if (currBody.partSockets.Count > 0)
                {
                    List<Brep> exitSockets = new List<Brep>();
                    foreach (partSocket prtSocket in currBody.partSockets)
                    {
                        if (currBody.partSockets.IndexOf(prtSocket) != socketIdx)
                            exitSockets.Add(prtSocket.SocketBrep);
                    }
                    var soketOverlaps = Brep.CreateBooleanIntersection(new List<Brep> { socketBrep }, exitSockets, myDoc.ModelAbsoluteTolerance);

                    if (soketOverlaps == null)
                    {
                        isPosValid = true;
                    }
                    else
                    {
                        if (soketOverlaps.Count() > 0)
                        {
                            isPosValid = false;
                        }
                        else
                        {
                            isPosValid = true;
                        }
                    }
                }

                if (!isPosValid)
                {
                    //ValidationWarningWin v_warning_win = new ValidationWarningWin();
                    //v_warning_win.Show();

                    #region undo the movement of the part

                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = socketOrigBrep;

                    #endregion
                }
                else
                {
                    #region Step 6: update the part information

                    targetPart.PartPos = old_prt_pos;

                    targetPart.TransformReverseSets.Clear();
                    targetPart.TransformSets.Clear();

                    targetPart.TransformSets.Add(partTranslation);
                    targetPart.TransformSets.Add(partRotation);
                    targetPart.TransformReverseSets.Add(retractPartRotation);
                    targetPart.TransformReverseSets.Add(retractPartTraslation);

                    int prtIndex = -1;
                    foreach (Part3D p in dataset.part3DList)
                    {
                        if (p.PartName.Equals(targetPart.PartName))
                        {
                            prtIndex = dataset.part3DList.IndexOf(p);
                        }
                    }

                    // overwrite the part information in the part list
                    dataset.part3DList.ElementAt(prtIndex).ExposureLevel = targetPart.ExposureLevel;
                    dataset.part3DList.ElementAt(prtIndex).Height = targetPart.Height;
                    dataset.part3DList.ElementAt(prtIndex).IsDeployed = targetPart.IsDeployed;
                    dataset.part3DList.ElementAt(prtIndex).IsFlipped = targetPart.IsFlipped;
                    dataset.part3DList.ElementAt(prtIndex).ModelPath = targetPart.ModelPath;
                    dataset.part3DList.ElementAt(prtIndex).Normal = targetPart.Normal;
                    dataset.part3DList.ElementAt(prtIndex).PartName = targetPart.PartName;
                    dataset.part3DList.ElementAt(prtIndex).PartPos = targetPart.PartPos;
                    dataset.part3DList.ElementAt(prtIndex).Pins.Clear();
                    foreach (Pin p in targetPart.Pins)
                    {
                        dataset.part3DList.ElementAt(prtIndex).Pins.Add(p);
                    }
                    dataset.part3DList.ElementAt(prtIndex).PrtID = targetPart.PrtID;
                    dataset.part3DList.ElementAt(prtIndex).RotationAngle = targetPart.RotationAngle;
                    dataset.part3DList.ElementAt(prtIndex).TransformReverseSets.Clear();
                    foreach (Transform tr in targetPart.TransformReverseSets)
                    {
                        dataset.part3DList.ElementAt(prtIndex).TransformReverseSets.Add(tr);
                    }
                    dataset.part3DList.ElementAt(prtIndex).TransformSets.Clear();
                    foreach (Transform tr in targetPart.TransformSets)
                    {
                        dataset.part3DList.ElementAt(prtIndex).TransformSets.Add(tr);
                    }

                    dataset.currPart3D = dataset.part3DList.ElementAt(prtIndex);
                    #endregion
                }

                myDoc.Views.Redraw();

                #endregion

                
            }

            #endregion

            #region when the selected part is the body and it has been changed

            // check and update all the parts
            if (isBodySelected)
            {
                if (myDoc.Layers.FindName("OriginalBody") == null)
                {
                    // not converted yet
                }
                else
                {
                    int index = myDoc.Layers.FindName("OriginalBody").Index;
                    myDoc.Objects.Find(currBody.objID).Attributes.LayerIndex = index;
                    myDoc.Layers.ElementAt(index).IsVisible = true;
                    myDoc.Objects.Show(currBody.objID, true);
                }

                dataset.isTraceGenerated = false;

                if (myDoc.Layers.FindName("ConvertedBody") == null)
                {
                    // the body has not ever been converted
                }
                else
                {
                    #region delete the old converted body

                    Guid id = currBody.convertedObjID;
                    myDoc.Objects.Delete(id, true);

                    #endregion

                    #region show the original body and delete all the automatically generated traces but keep manual traces

                    int index = myDoc.Layers.FindName("OriginalBody").Index;
                    myDoc.Layers.ElementAt(index).IsVisible = true;

                    List<int> pos = new List<int>();
                    foreach (Trace trc in dataset.deployedTraces)
                    {
                        if (trc.Type == 1)
                        {
                            myDoc.Objects.Delete(trc.TrID, true);
                            int idx = dataset.deployedTraces.IndexOf(trc);
                            pos.Add(idx);
                        }
                    }

                    pos.Sort((a, b) => b.CompareTo(a));

                    foreach (int i in pos)
                    {
                        dataset.deployedTraces.RemoveAt(i);
                    }

                    // delete all the generated pads
                    foreach (Guid pad_id in dataset.pads)
                    {
                        myDoc.Objects.Delete(pad_id, true);
                    }
                    dataset.pads.Clear();

                    #endregion

                }

                Transform dynPrtTrans = new Transform();
                myDoc.Objects.Find(targetBodyID).GetDynamicTransform(out dynPrtTrans);
                dynPrtTrans.Affineize();

                foreach (Part3D p3D in dataset.part3DList)
                {
                    targetPart = p3D;

                    Part3D temp = new Part3D();

                    temp.ExposureLevel = p3D.ExposureLevel;
                    temp.Height = p3D.Height;
                    temp.IsDeployed = p3D.IsDeployed;
                    temp.IsFlipped = p3D.IsFlipped;
                    temp.ModelPath = p3D.ModelPath;
                    temp.Normal = p3D.Normal;
                    temp.PartName = p3D.PartName;
                    temp.PartPos = p3D.PartPos;
                    temp.Pins = new List<Pin>();
                    foreach (Pin p in p3D.Pins)
                    {
                        temp.Pins.Add(p);
                    }
                    temp.PrtID = p3D.PrtID;
                    temp.RotationAngle = p3D.RotationAngle;
                    temp.TransformReverseSets = new List<Transform>();
                    foreach (Transform tr in p3D.TransformReverseSets)
                    {
                        temp.TransformReverseSets.Add(tr);
                    }
                    temp.TransformSets = new List<Transform>();
                    foreach (Transform tr in p3D.TransformSets)
                    {
                        temp.TransformSets.Add(tr);
                    }

                    #region Step 1: get the new position, socket, and the normal direction at the new position

                    int socketIdx = -1;
                    Brep socketOrigBrep = new Brep();

                    foreach (partSocket prtS in currBody.partSockets)
                    {
                        if (prtS.PrtName.Equals(targetPart.PartName))
                        {
                            socketIdx = currBody.partSockets.IndexOf(prtS);
                            socketOrigBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        }
                    }

                    Point3d newCen = myDoc.Objects.Find(temp.PrtID).Geometry.GetBoundingBox(true).Center;
                    newCen.Transform(dynPrtTrans);

                    Point3d newPos = FindCloesetPointFromSurface(newCen, targetBodyID);

                    Vector3d outwardDir = new Vector3d();

                    Point3d closestPt = new Point3d();
                    double u, v;
                    ComponentIndex ii;
                    currBody.bodyBrep.ClosestPoint(newPos, out closestPt, out ii, out u, out v, 2, out outwardDir);

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

                    #region Step 2: reverse self-rotation, exposure, and flip

                    Point3d old_prt_pos = temp.PartPos;

                    Vector3d prt_normal = temp.Normal;
                    double prt_height = temp.Height;
                    double prt_exp = temp.ExposureLevel;
                    
                    Transform selfrotTrans = Transform.Rotation((-1) * temp.RotationAngle / 180.0 * Math.PI, temp.Normal, old_prt_pos);
                    Guid tempID = myDoc.Objects.Transform(temp.PrtID, selfrotTrans, true);
                    temp.PrtID = tempID;
                    old_prt_pos.Transform(selfrotTrans);

                    #region update the pins

                    foreach (Pin p in temp.Pins)
                    {
                        Point3d temp_Pt = p.Pos;

                        temp_Pt.Transform(selfrotTrans);

                        int idx = temp.Pins.IndexOf(p);
                        temp.Pins.ElementAt(idx).Pos = temp_Pt;
                    }

                    #endregion

                    #region update the socket

                    Brep prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(selfrotTrans);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion



                    Vector3d reverseNormalVector = temp.Normal * (-1);

                    Transform resetTrans = Transform.Translation(prt_normal * (-1) * prt_exp * prt_height);
                    Guid newIDTemp = myDoc.Objects.Transform(temp.PrtID, resetTrans, true);
                    temp.PrtID = newIDTemp;

                    old_prt_pos.Transform(resetTrans);

                    #region update the pins

                    foreach (Pin p in temp.Pins)
                    {
                        Point3d temp_Pt = p.Pos;

                        temp_Pt.Transform(resetTrans);

                        int idx = temp.Pins.IndexOf(p);
                        temp.Pins.ElementAt(idx).Pos = temp_Pt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(resetTrans);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion


                    Transform flipTrans = Transform.Rotation(temp.Normal, reverseNormalVector, old_prt_pos);
                    if (temp.IsFlipped)
                    {
                        Guid newID = myDoc.Objects.Transform(temp.PrtID, flipTrans, true);
                        temp.PrtID = newID;
                        old_prt_pos.Transform(flipTrans);

                        #region update the pins

                        foreach (Pin p in temp.Pins)
                        {
                            Point3d temp_Pt = p.Pos;

                            temp_Pt.Transform(flipTrans);

                            int idx = temp.Pins.IndexOf(p);
                            temp.Pins.ElementAt(idx).Pos = temp_Pt;
                        }

                        #endregion

                        #region update the socket

                        prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        prtBrep.Transform(flipTrans);
                        currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                        #endregion
                    }

                    #endregion

                    #region Step 3: transform back to the initial position and orientation when the part is imported

                    foreach (Transform t in temp.TransformReverseSets)
                    {
                        Guid transID = myDoc.Objects.Transform(temp.PrtID, t, true);
                        temp.PrtID = transID;
                        old_prt_pos.Transform(t);

                        #region update the pins

                        foreach (Pin p in temp.Pins)
                        {
                            Point3d temp_Pt = p.Pos;

                            temp_Pt.Transform(t);

                            int idx = temp.Pins.IndexOf(p);
                            temp.Pins.ElementAt(idx).Pos = temp_Pt;
                        }

                        #endregion

                        #region update the socket

                        prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        prtBrep.Transform(t);
                        currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                        #endregion
                    }

                    #endregion

                    #region Step 4: transform to the new position and orientation

                    double hideratio = 0.5;

                    var embedTranslation = Transform.Translation(outwardDir * (-1) * prt_height * hideratio);
                    Point3d new_part_pos = newPos;
                    new_part_pos.Transform(embedTranslation);

                    //Point3d new_ori_pos = myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center;
                    Point3d new_ori_pos = old_prt_pos;
                    Vector3d moveVector = new_part_pos - new_ori_pos;
                    Vector3d retractMoveVector = new_ori_pos - new_part_pos;
                    var partTranslation = Transform.Translation(moveVector);
                    var retractPartTraslation = Transform.Translation(retractMoveVector);
                    Guid transit_ID = myDoc.Objects.Transform(temp.PrtID, partTranslation, true);
                    old_prt_pos.Transform(partTranslation);

                    Vector3d startDirVector = new Vector3d(0, 0, 1);
                    Vector3d endDirVector = outwardDir;
                    var partRotation = Transform.Rotation(startDirVector, endDirVector, new_part_pos);
                    var retractPartRotation = Transform.Rotation(endDirVector, startDirVector, new_part_pos);
                    Guid rotate_ID = myDoc.Objects.Transform(transit_ID, partRotation, true);
                    old_prt_pos.Transform(partRotation);

                    temp.PrtID = rotate_ID;
                    temp.Normal = outwardDir;

                    #region update the pins

                    foreach (Pin p in temp.Pins)
                    {
                        Point3d temp_Pt = p.Pos;

                        temp_Pt.Transform(partTranslation);
                        temp_Pt.Transform(partRotation);

                        int idx = temp.Pins.IndexOf(p);
                        temp.Pins.ElementAt(idx).Pos = temp_Pt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(partTranslation);
                    prtBrep.Transform(partRotation);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion

                    #endregion

                    #region Step 5: flip back, set the exposure, and set the self-rotation

                    Vector3d reverseNormalVector1 = temp.Normal * (-1);
                    Transform flipTrans1 = Transform.Rotation(temp.Normal, reverseNormalVector1, old_prt_pos);
                    if (temp.IsFlipped)
                    {
                        Guid newID = myDoc.Objects.Transform(temp.PrtID, flipTrans1, true);
                        temp.PrtID = newID;
                        old_prt_pos.Transform(flipTrans1);

                        #region update the pins

                        foreach (Pin p in temp.Pins)
                        {
                            Point3d temp_pt = p.Pos;

                            temp_pt.Transform(flipTrans1);

                            int idx = temp.Pins.IndexOf(p);
                            temp.Pins.ElementAt(idx).Pos = temp_pt;
                        }

                        #endregion

                        #region update the socket

                        prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        prtBrep.Transform(flipTrans1);
                        currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                        #endregion
                    }


                    Transform setExpTrans = Transform.Translation(temp.Normal * prt_exp * prt_height);
                    Guid newID1 = myDoc.Objects.Transform(temp.PrtID, setExpTrans, true);
                    temp.PrtID = newID1;
                    old_prt_pos.Transform(setExpTrans);

                    #region update the pins

                    foreach (Pin p in temp.Pins)
                    {
                        Point3d temp_pt = p.Pos;

                        temp_pt.Transform(setExpTrans);

                        int idx = temp.Pins.IndexOf(p);
                        temp.Pins.ElementAt(idx).Pos = temp_pt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(setExpTrans);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion


                    Transform selfrotRecoverTrans = Transform.Rotation(temp.RotationAngle / 180.0 * Math.PI, temp.Normal, old_prt_pos);
                    Guid tempRotID = myDoc.Objects.Transform(temp.PrtID, selfrotRecoverTrans, true);
                    temp.PrtID = tempRotID;
                    old_prt_pos.Transform(selfrotRecoverTrans);

                    #region update the pins

                    foreach (Pin p in temp.Pins)
                    {
                        Point3d temp_Pt = p.Pos;

                        temp_Pt.Transform(selfrotRecoverTrans);

                        int idx = temp.Pins.IndexOf(p);
                        temp.Pins.ElementAt(idx).Pos = temp_Pt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(selfrotRecoverTrans);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion



                    #endregion

                    #region check if sockets can be generated in the new model

                    bool isSocketSizeValid = true;
                    Brep socketBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;

                    bool isPosValid = true;


                    #region test if the socket size is valid in the new model

                    var soketLeftovers = Brep.CreateBooleanDifference(currBody.bodyBrep, socketBrep, myDoc.ModelAbsoluteTolerance);

                    if (soketLeftovers == null)
                    {
                        isSocketSizeValid = false;
                    }
                    else
                    {
                        if (soketLeftovers.Count() > 0)
                        {
                            if (soketLeftovers.Count() == 1)
                                isSocketSizeValid = true;
                            else
                                isSocketSizeValid = false;
                        }
                        else
                        {
                            isSocketSizeValid = false;
                        }
                    }

                    #endregion

                    #region test if all the sockets overlap

                    List<Brep> exitSockets = new List<Brep>();
                    foreach (partSocket prtSocket in currBody.partSockets)
                    {
                        if (currBody.partSockets.IndexOf(prtSocket) != socketIdx)
                            exitSockets.Add(prtSocket.SocketBrep);
                    }
                    var soketOverlaps = Brep.CreateBooleanIntersection(new List<Brep> { socketBrep }, exitSockets, myDoc.ModelAbsoluteTolerance);

                    if (soketOverlaps == null)
                    {
                        isPosValid = true;
                    }
                    else
                    {
                        if (soketOverlaps.Count() > 0)
                        {
                            isPosValid = false;
                        }
                        else
                        {
                            isPosValid = true;
                        }
                    }

                    #endregion


                    if (!isSocketSizeValid || !isPosValid)
                    {
                        //ValidationWarningWin v_warning_win = new ValidationWarningWin();
                        //v_warning_win.Show();

                        #region undo the movement of the part

                        currBody.partSockets.ElementAt(socketIdx).SocketBrep = socketOrigBrep;

                        #endregion
                    }
                    else
                    {
                        #region Step 6: update the part information

                        temp.PartPos = old_prt_pos;

                        temp.TransformReverseSets.Clear();
                        temp.TransformSets.Clear();

                        temp.TransformSets.Add(partTranslation);
                        temp.TransformSets.Add(partRotation);
                        temp.TransformReverseSets.Add(retractPartRotation);
                        temp.TransformReverseSets.Add(retractPartTraslation);

                        int prtIndex = -1;
                        foreach (Part3D p in dataset.part3DList)
                        {
                            if (p.PartName.Equals(targetPart.PartName))
                            {
                                prtIndex = dataset.part3DList.IndexOf(p);
                            }
                        }

                        // overwrite the part information in the part list
                        dataset.part3DList.ElementAt(prtIndex).ExposureLevel = temp.ExposureLevel;
                        dataset.part3DList.ElementAt(prtIndex).Height = temp.Height;
                        dataset.part3DList.ElementAt(prtIndex).IsDeployed = temp.IsDeployed;
                        dataset.part3DList.ElementAt(prtIndex).IsFlipped = temp.IsFlipped;
                        dataset.part3DList.ElementAt(prtIndex).ModelPath = temp.ModelPath;
                        dataset.part3DList.ElementAt(prtIndex).Normal = temp.Normal;
                        dataset.part3DList.ElementAt(prtIndex).PartName = temp.PartName;
                        dataset.part3DList.ElementAt(prtIndex).PartPos = temp.PartPos;
                        dataset.part3DList.ElementAt(prtIndex).Pins.Clear();
                        foreach (Pin p in temp.Pins)
                        {
                            dataset.part3DList.ElementAt(prtIndex).Pins.Add(p);
                        }
                        dataset.part3DList.ElementAt(prtIndex).PrtID = temp.PrtID;
                        dataset.part3DList.ElementAt(prtIndex).RotationAngle = temp.RotationAngle;
                        dataset.part3DList.ElementAt(prtIndex).TransformReverseSets.Clear();
                        foreach (Transform tr in temp.TransformReverseSets)
                        {
                            dataset.part3DList.ElementAt(prtIndex).TransformReverseSets.Add(tr);
                        }
                        dataset.part3DList.ElementAt(prtIndex).TransformSets.Clear();
                        foreach (Transform tr in temp.TransformSets)
                        {
                            dataset.part3DList.ElementAt(prtIndex).TransformSets.Add(tr);
                        }

                        dataset.currPart3D = dataset.part3DList.ElementAt(prtIndex);
                        #endregion
                    }

                    myDoc.Views.Redraw();

                    #endregion
                }

            }
            #endregion
        }
    }

}
