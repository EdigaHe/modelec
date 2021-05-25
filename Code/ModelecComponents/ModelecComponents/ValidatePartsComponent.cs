using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace ModelecComponents
{
    public class ValidatePartsComponent : GH_Component
    {
        bool testBtnClick;
        int count;
        RhinoDoc myDoc;
        SharedData dataset;
        TargetBody currBody;
        ProcessingWindow prcWin;
        Point3d selPrtPosPt;
        ObjectAttributes solidAttribute, orangeAttribute, redAttribute, blueAttribute;

        /// <summary>
        /// Initializes a new instance of the ValidatePartsComponent class.
        /// </summary>
        public ValidatePartsComponent()
          : base("ValidatePartsComponent", "ValidateParts",
              "Validate all the parts and deploy all the parts in the model",
              "ModElec", "PartOperations")
        {
            testBtnClick = false;
            count = 0;
            myDoc = RhinoDoc.ActiveDoc;
            dataset = SharedData.Instance;
            currBody = TargetBody.Instance;
            prcWin = new ProcessingWindow();
            selPrtPosPt = new Point3d();

            int solidIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material solidMat = myDoc.Materials[solidIndex];
            solidMat.DiffuseColor = System.Drawing.Color.White;
            solidMat.SpecularColor = System.Drawing.Color.White;
            solidMat.Transparency = 0;
            solidMat.CommitChanges();
            solidAttribute = new ObjectAttributes();
            solidAttribute.LayerIndex = 2;
            solidAttribute.MaterialIndex = solidIndex;
            solidAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            solidAttribute.ObjectColor = Color.White;
            solidAttribute.ColorSource = ObjectColorSource.ColorFromObject;

            int orangeIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material orangeMat = myDoc.Materials[orangeIndex];
            orangeMat.DiffuseColor = System.Drawing.Color.Orange;
            orangeMat.Transparency = 0.3;
            orangeMat.SpecularColor = System.Drawing.Color.Orange;
            orangeMat.CommitChanges();
            orangeAttribute = new ObjectAttributes();
            orangeAttribute.LayerIndex = 3;
            orangeAttribute.MaterialIndex = orangeIndex;
            orangeAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            orangeAttribute.ObjectColor = Color.Orange;
            orangeAttribute.ColorSource = ObjectColorSource.ColorFromObject;

            int redIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material redMat = myDoc.Materials[redIndex];
            redMat.DiffuseColor = System.Drawing.Color.Red;
            redMat.Transparency = 0.3;
            redMat.SpecularColor = System.Drawing.Color.Red;
            redMat.CommitChanges();
            redAttribute = new ObjectAttributes();
            redAttribute.LayerIndex = 4;
            redAttribute.MaterialIndex = redIndex;
            redAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            redAttribute.ObjectColor = Color.Red;
            redAttribute.ColorSource = ObjectColorSource.ColorFromObject;

            int blueIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material blueMat = myDoc.Materials[blueIndex];
            blueMat.DiffuseColor = System.Drawing.Color.FromArgb(16, 150, 206);
            blueMat.SpecularColor = System.Drawing.Color.FromArgb(16, 150, 206);
            blueMat.Transparency = 0.7f;
            blueMat.TransparentColor = System.Drawing.Color.FromArgb(16, 150, 206);
            blueMat.CommitChanges();
            blueAttribute = new ObjectAttributes();
            blueAttribute.LayerIndex = 5;
            blueAttribute.MaterialIndex = blueIndex;
            blueAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            blueAttribute.ObjectColor = Color.FromArgb(16, 150, 206);
            blueAttribute.ColorSource = ObjectColorSource.ColorFromObject;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("ValidatePartsBtnClicked", "BC", "validate the part", GH_ParamAccess.item);
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
            bool toExePrtValidationSet = false;

            #region read the button click and decide whether execute the circuit load or not

            if (!DA.GetData(0, ref btnClick))
                return;

            if (!btnClick && testBtnClick)
            {
                toExePrtValidationSet = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }

            #endregion

            if (toExePrtValidationSet)
            {
                //List<Part3D> undeployedParts = new List<Part3D>();
                bool isFirstTime = true;
                foreach(Part3D prt3D in dataset.part3DList)
                {
                    if (prt3D.IsDeployed)
                    {
                        // check if the deployed part valid

                        double hideratio = 0.5;

                        //if()

                        Point3d newPos = FindCloesetPointFromSurface(myDoc.Objects.Find(prt3D.PrtID).Geometry.GetBoundingBox(true).Center, currBody.objID);

                        #region Step 1: get the normal direction at this point on the surface

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

                        int socketIdx = -1;
                        foreach (partSocket prtS in currBody.partSockets)
                        {
                            if (prtS.PrtName.Equals(prt3D.PartName))
                            {
                                socketIdx = currBody.partSockets.IndexOf(prtS);
                            }
                        }

                        #region Step 2: reverse the exposure

                        // First, reverse the exposure
                        Point3d old_prt_pos = prt3D.PartPos;

                        Vector3d prt_normal = prt3D.Normal;
                        double prt_height = prt3D.Height;
                        double prt_exp = prt3D.ExposureLevel;
                        Vector3d reverseNormalVector = prt3D.Normal * (-1);

                        Transform resetTrans = Transform.Translation(prt_normal * (-1) * prt_exp * prt_height);
                        Guid newIDTemp = myDoc.Objects.Transform(prt3D.PrtID, resetTrans, true);
                        prt3D.PrtID = newIDTemp;

                        old_prt_pos.Transform(resetTrans);

                        #region update the pins

                        foreach (Pin p in prt3D.Pins)
                        {
                            Point3d temp_Pt = p.Pos;

                            temp_Pt.Transform(resetTrans);

                            int idx = prt3D.Pins.IndexOf(p);
                            prt3D.Pins.ElementAt(idx).Pos = temp_Pt;
                        }

                        #endregion

                        #region update the socket

                        Brep prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        prtBrep.Transform(resetTrans);
                        currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                        #endregion

                        #endregion

                        #region Step 3: reverse the flip

                        Transform flipTrans = Transform.Rotation(prt3D.Normal, reverseNormalVector, old_prt_pos);
                        if (prt3D.IsFlipped)
                        {
                            Guid newID = myDoc.Objects.Transform(prt3D.PrtID, flipTrans, true);
                            prt3D.PrtID = newID;
                            old_prt_pos.Transform(flipTrans);

                            #region update the pins

                            foreach (Pin p in prt3D.Pins)
                            {
                                Point3d temp_Pt = p.Pos;

                                temp_Pt.Transform(flipTrans);

                                int idx = prt3D.Pins.IndexOf(p);
                                prt3D.Pins.ElementAt(idx).Pos = temp_Pt;
                            }

                            #endregion

                            #region update the socket

                            prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                            prtBrep.Transform(flipTrans);
                            currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                            #endregion
                        }

                        #endregion

                        #region Step 4: transform back to the standard position and orientation

                        foreach (Transform t in prt3D.TransformReverseSets)
                        {
                            Guid transID = myDoc.Objects.Transform(prt3D.PrtID, t, true);
                            prt3D.PrtID = transID;
                            old_prt_pos.Transform(t);

                            #region update the pins

                            foreach (Pin p in prt3D.Pins)
                            {
                                Point3d temp_Pt = p.Pos;

                                temp_Pt.Transform(t);

                                int idx = prt3D.Pins.IndexOf(p);
                                prt3D.Pins.ElementAt(idx).Pos = temp_Pt;
                            }

                            #endregion

                            #region update the socket

                            prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                            prtBrep.Transform(t);
                            currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                            #endregion
                        }

                        #endregion


                        #region old version (commented) 

                        //Point3d obj_pos = ((Brep)myDoc.Objects.Find(currBody.objID).Geometry).GetBoundingBox(true).Center;

                        //int prt_idx = -1;
                        //foreach (Part3D prt3D1 in dataset.part3DList)
                        //{
                        //    if (prt3D1.PartName.Equals(prt3D.PartName))
                        //    {
                        //        prt_idx = dataset.part3DList.IndexOf(prt3D1);
                        //    }
                        //}

                        //BoundingBox partBoundingBox = new BoundingBox();
                        //Guid recoverPartPosID = Guid.Empty;
                        //Guid tempPartPosID1 = Guid.Empty;
                        //Guid tempPartPosID1_5 = Guid.Empty;
                        //Guid tempPartPosID2 = Guid.Empty;
                        //Guid importedObjID = Guid.Empty;


                        //importedObjID = dataset.part3DList.ElementAt(prt_idx).PrtID;
                        ////dataset.part3DList.ElementAt(prt_idx).PartPos = myDoc.Objects.Find(importedObjID).Geometry.GetBoundingBox(true).Center;

                        //Point3d new_ori_pos = new Point3d(obj_pos.X, obj_pos.Y, obj_pos.Z + 40);

                        //// set the part position using the current center in case the user has manually moved the part
                        //Point3d currCen = myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center;
                        //Point3d prevCen = dataset.part3DList.ElementAt(prt_idx).PartPos;
                        //Transform manualMove = Transform.Translation(currCen - prevCen);

                        //dataset.part3DList.ElementAt(prt_idx).PartPos = myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center;


                        //foreach(Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                        //{
                        //    Point3d pt_pos = p.Pos;
                        //    pt_pos.Transform(manualMove);
                        //    int i = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                        //    dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(i).Pos = pt_pos;
                        //}

                        //#region test all part and pin positions

                        ////foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                        ////{
                        //    //myDoc.Objects.AddPoint(p.Pos, redAttribute);
                        //    //myDoc.Views.Redraw();
                        ////}

                        //#endregion

                        //// First, rotate or flip the part back if there is a rotation or flip for the part on the surface
                        //Transform roback = Transform.Rotation((-1)*dataset.part3DList.ElementAt(prt_idx).RotationAngle / 180.0 * Math.PI,
                        //                                        dataset.part3DList.ElementAt(prt_idx).Normal, dataset.part3DList.ElementAt(prt_idx).PartPos);
                        //tempPartPosID1 = myDoc.Objects.Transform(importedObjID, roback, true);

                        //dataset.part3DList.ElementAt(prt_idx).PartPos.Transform(roback);
                        //foreach(Pin pin_info in dataset.part3DList.ElementAt(prt_idx).Pins)
                        //{
                        //    Point3d p_pos = pin_info.Pos;
                        //    p_pos.Transform(roback);
                        //    int p_idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(pin_info);
                        //    dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(p_idx).Pos = p_pos;
                        //}

                        //if (dataset.part3DList.ElementAt(prt_idx).IsFlipped)
                        //{
                        //    Vector3d reverseNormalVector = dataset.part3DList.ElementAt(prt_idx).Normal * (-1);
                        //    Transform flipTrans = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).Normal, reverseNormalVector, dataset.part3DList.ElementAt(prt_idx).PartPos);

                        //    tempPartPosID1_5 = myDoc.Objects.Transform(tempPartPosID1, flipTrans, true);

                        //    dataset.part3DList.ElementAt(prt_idx).PartPos.Transform(flipTrans);
                        //    foreach (Pin pin_info in dataset.part3DList.ElementAt(prt_idx).Pins)
                        //    {
                        //        Point3d p_pos = pin_info.Pos;
                        //        p_pos.Transform(flipTrans);
                        //        int p_idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(pin_info);
                        //        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(p_idx).Pos = p_pos;
                        //    }
                        //}
                        //else
                        //{
                        //    tempPartPosID1_5 = tempPartPosID1;
                        //}

                        //// Second, transform back to the original imported position
                        //// don't use Part3D's TransformReverseSets
                        //Transform rotationBack = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).Normal, new Vector3d(0, 0, 1), dataset.part3DList.ElementAt(prt_idx).PartPos);
                        //tempPartPosID2 = myDoc.Objects.Transform(tempPartPosID1_5, rotationBack, true);

                        //Transform translateBack = Transform.Translation(new_ori_pos - dataset.part3DList.ElementAt(prt_idx).PartPos);
                        //recoverPartPosID = myDoc.Objects.Transform(tempPartPosID2, translateBack, true);
                        //partBoundingBox = myDoc.Objects.Find(recoverPartPosID).Geometry.GetBoundingBox(true);                

                        //Point3d tempPt = dataset.part3DList.ElementAt(prt_idx).PartPos;
                        //tempPt.Transform(rotationBack);
                        //tempPt.Transform(translateBack);
                        //dataset.part3DList.ElementAt(prt_idx).PartPos = tempPt;

                        //foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                        //{
                        //    Point3d tempPinPt = p.Pos;
                        //    tempPinPt.Transform(rotationBack);
                        //    tempPinPt.Transform(translateBack);
                        //    int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                        //    dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = tempPinPt;
                        //}

                        ////#region for test

                        ////myDoc.Objects.AddBrep(partBoundingBox.ToBrep(), orangeAttribute);
                        ////myDoc.Views.Redraw();
                        ////myDoc.Objects.AddPoint(dataset.part3DList.ElementAt(prt_idx).PartPos, orangeAttribute);

                        ////myDoc.Views.Redraw();

                        ////#endregion



                        //#region Step 3: Find all the points as candidate positions on the surface of the 3D model and ask the user the select one position

                        //#region convert the selected brep into a mesh

                        //var brep = currBody.bodyBrep;
                        //if (brep == null) return;

                        //var default_mesh_params = MeshingParameters.Default;
                        //var minimal = MeshingParameters.Minimal;

                        //var meshes = Mesh.CreateFromBrep(brep, default_mesh_params);
                        //if (meshes == null || meshes.Length == 0) return;

                        //var brep_mesh = new Mesh(); // the mesh of the currently selected body
                        //foreach (var mesh in meshes)
                        //    brep_mesh.Append(mesh);
                        //brep_mesh.Faces.ConvertQuadsToTriangles();

                        //#endregion

                        //#region prepare the points on the model surface

                        //BoundingBox meshBox = brep_mesh.GetBoundingBox(true);
                        //double w = meshBox.Max.X - meshBox.Min.X;
                        //double l = meshBox.Max.Y - meshBox.Min.Y;
                        //double h = meshBox.Max.Z - meshBox.Min.Z;
                        //if (isFirstTime)
                        //{
                        //    currBody.surfPts.Clear();

                        //    prcWin.Show();

                        //    for (int i = 0; i < w; i++)
                        //    {
                        //        for (int j = 0; j < l; j++)
                        //        {
                        //            for (int t = 0; t < h; t++)
                        //            {
                        //                Point3d ptInSpace = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, t + meshBox.Min.Z);
                        //                if (brep.ClosestPoint(ptInSpace).DistanceTo(ptInSpace) <= 0.5)
                        //                {
                        //                    currBody.surfPts.Add(brep.ClosestPoint(ptInSpace));
                        //                }
                        //            }
                        //        }
                        //    }

                        //    prcWin.Hide();
                        //    isFirstTime = false;
                        //}

                        //#endregion

                        //#region change the part to the new position

                        //while (true)
                        //{

                        //    #region Step 1: find the closest point on the selected object's surface

                        //    Point3d ptOnSurf = FindCloesetPointFromSurface(newPos, currBody.objID);

                        //    #endregion

                        //    #region Step 2: get the normal direction at this point on the surface

                        //    ObjRef currObjRef = new ObjRef(currBody.objID);
                        //    Vector3d outwardDir = new Vector3d();

                        //    Point3d closestPt = new Point3d();
                        //    double u, v;
                        //    ComponentIndex i;
                        //    currBody.bodyBrep.ClosestPoint(ptOnSurf, out closestPt, out i, out u, out v, 2, out outwardDir);

                        //    List<Brep> projectedBrep = new List<Brep>();
                        //    projectedBrep.Add(currBody.bodyBrep);
                        //    List<Point3d> projectPt = new List<Point3d>();
                        //    projectPt.Add(closestPt);

                        //    var intersectionPts = Intersection.ProjectPointsToBreps(projectedBrep, projectPt, outwardDir, myDoc.ModelAbsoluteTolerance);
                        //    if (intersectionPts.Length == 1)
                        //    {
                        //        outwardDir = (-1) * outwardDir;
                        //    }

                        //    #endregion



                        //    #region Step 3: move the imported part to the final location (make sure the part is entirely hidden in the model)

                        //    double partHeight = partBoundingBox.GetCorners().ElementAt(4).Z - partBoundingBox.GetCorners().ElementAt(0).Z;
                        //    var embedTranslation = Transform.Translation(outwardDir * (-1) * partHeight * hideratio);
                        //    Point3d new_part_pos = ptOnSurf;
                        //    new_part_pos.Transform(embedTranslation);

                        //    Vector3d moveVector = new_part_pos - new_ori_pos;
                        //    Vector3d retractMoveVector = new_ori_pos - new_part_pos;
                        //    var partTranslation = Transform.Translation(moveVector);
                        //    var retractPartTraslation = Transform.Translation(retractMoveVector);
                        //    Guid transit_ID = myDoc.Objects.Transform(recoverPartPosID, partTranslation, true);

                        //    #endregion

                        //    #region Step 4: create the truncated pyramid socket

                        //    // create the solid box based on the bounding box before rotation
                        //    partBoundingBox = myDoc.Objects.Find(transit_ID).Geometry.GetBoundingBox(true);

                        //    // make the socket a bit larger than the part (currently, ~0.5mm bigger)
                        //    partBoundingBox.Inflate(0.1);
                        //    Box socketBox = new Box(partBoundingBox);
                        //    BoundingBox scktBoundingBox = socketBox.BoundingBox;
                        //    Brep truncatedSocket = scktBoundingBox.ToBrep();
                        //    //// create a truncated pyramid
                        //    //double offsetTP = (scktBoundingBox.GetCorners()[4].Z - scktBoundingBox.GetCorners()[0].Z) * Math.Sqrt(2) * 0.5;
                        //    //Point3d n4 = scktBoundingBox.GetCorners()[4] + new Vector3d(-offsetTP, -offsetTP, 0);
                        //    //Point3d n5 = scktBoundingBox.GetCorners()[5] + new Vector3d(offsetTP, -offsetTP, 0);
                        //    //Point3d n6 = scktBoundingBox.GetCorners()[6] + new Vector3d(offsetTP, offsetTP, 0);
                        //    //Point3d n7 = scktBoundingBox.GetCorners()[7] + new Vector3d(-offsetTP, offsetTP, 0);
                        //    //Point3d n8 = n4;
                        //    //List<Point3d> topRectCorners = new List<Point3d>();
                        //    //topRectCorners.Add(n4);
                        //    //topRectCorners.Add(n5);
                        //    //topRectCorners.Add(n6);
                        //    //topRectCorners.Add(n7);
                        //    //topRectCorners.Add(n8);

                        //    //Point3d o0 = scktBoundingBox.GetCorners()[0];
                        //    //Point3d o1 = scktBoundingBox.GetCorners()[1];
                        //    //Point3d o2 = scktBoundingBox.GetCorners()[2];
                        //    //Point3d o3 = scktBoundingBox.GetCorners()[3];
                        //    //Point3d o4 = o0;
                        //    //List<Point3d> bottomRectCorners = new List<Point3d>();
                        //    //bottomRectCorners.Add(o0);
                        //    //bottomRectCorners.Add(o1);
                        //    //bottomRectCorners.Add(o2);
                        //    //bottomRectCorners.Add(o3);
                        //    //bottomRectCorners.Add(o4);

                        //    //Polyline topRect = new Polyline(topRectCorners);
                        //    //Polyline bottomRect = new Polyline(bottomRectCorners);
                        //    //Curve topRectCrv = topRect.ToNurbsCurve();
                        //    //Curve bottomRectCrv = bottomRect.ToNurbsCurve();
                        //    //Line crossLn = new Line(o0, n4);
                        //    //Curve crossCrv = crossLn.ToNurbsCurve();

                        //    //var sweep = new SweepOneRail();
                        //    //sweep.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                        //    //sweep.ClosedSweep = false;
                        //    //sweep.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                        //    ////myDoc.Objects.AddCurve(topRectCrv, orangeAttribute);
                        //    ////myDoc.Views.Redraw();
                        //    ////myDoc.Objects.AddCurve(bottomRectCrv, orangeAttribute);
                        //    ////myDoc.Views.Redraw();
                        //    ////myDoc.Objects.AddCurve(crossCrv, orangeAttribute);
                        //    ////myDoc.Views.Redraw();

                        //    //Brep[] tpBreps = sweep.PerformSweep(bottomRectCrv, crossCrv);
                        //    //Brep tpBrep = tpBreps[0];
                        //    //Brep truncatedSocket = tpBrep.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);

                        //    #endregion

                        //    #region Step 5: rotate the part to the normal direction

                        //    Vector3d startDirVector = new Vector3d(0, 0, 1);
                        //    Vector3d endDirVector = outwardDir;
                        //    var partRotation = Transform.Rotation(startDirVector, endDirVector, new_part_pos);
                        //    var retractPartRotation = Transform.Rotation(endDirVector, startDirVector, new_part_pos);

                        //    // no intersections so that the part can be added to this location inside the 3D model
                        //    Guid socketBoxID = myDoc.Objects.AddBrep(truncatedSocket, orangeAttribute);
                        //    // get the final socket box after rotation
                        //    Guid finalSocketID = myDoc.Objects.Transform(socketBoxID, partRotation, true);
                        //    // situate the part in the final position and orientation
                        //    Guid finalPartPosID = myDoc.Objects.Transform(transit_ID, partRotation, true);

                        //    // Extend the box a bit more outside the selected solid
                        //    Transform differenceMove = Transform.Translation(outwardDir * (partHeight / 2 - 0.1));
                        //    Guid extendedSocketBoxID = myDoc.Objects.Transform(finalSocketID, differenceMove, false);

                        //    Brep currentSelectedBrep = currBody.bodyBrep;
                        //    Brep toRemoveBrep = (Brep)myDoc.Objects.Find(extendedSocketBoxID).Geometry;
                        //    // Test if the added electrical component can be added and hiden in this position
                        //    Brep dupCurrentSelectedBrep = currentSelectedBrep.DuplicateBrep();
                        //    Brep dupToRemoveSocketBrep = toRemoveBrep.DuplicateBrep();

                        //    //myDoc.Objects.AddBrep(dupToRemoveSocketBrep, orangeAttribute);
                        //    //myDoc.Views.Redraw();

                        //    var leftObj = Brep.CreateBooleanDifference(dupToRemoveSocketBrep, dupCurrentSelectedBrep, myDoc.ModelAbsoluteTolerance);
                        //    myDoc.Objects.Delete(extendedSocketBoxID, true);

                        //    //if (leftObj.Count() != 1)
                        //    //{
                        //    //    // revert the position of the imported part model
                        //    //    Guid retractFinalPartPosID = myDoc.Objects.Transform(finalPartPosID, retractPartRotation, true);
                        //    //    recoverPartPosID = myDoc.Objects.Transform(retractFinalPartPosID, retractPartTraslation, true);
                        //    //    myDoc.Objects.Delete(finalSocketID, true);
                        //    //    myDoc.Views.Redraw();
                        //    //    continue;
                        //    //}
                        //    //else
                        //    //{

                        //        // confirm that the part is added to the model
                        //        partSocket tempsocket = new partSocket();
                        //        tempsocket.PrtName = prt3D.PartName;
                        //        tempsocket.SocketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;

                        //        bool isExistSocket = false;
                        //        int socketIdx = -1;
                        //        foreach (partSocket prtS in currBody.partSockets)
                        //        {
                        //            if (prtS.PrtName.Equals(tempsocket.PrtName))
                        //            {
                        //                isExistSocket = true;
                        //                socketIdx = currBody.partSockets.IndexOf(prtS);
                        //                break;
                        //            }
                        //        }

                        //        if (!isExistSocket)
                        //        {
                        //            currBody.partSockets.Add(tempsocket);
                        //        }
                        //        else
                        //        {
                        //            currBody.partSockets.ElementAt(socketIdx).SocketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;
                        //        }

                        //        // draw the normal for part rotation and flip manipulations
                        //        //Line normalLine = new Line(new_part_pos, outwardDir * partHeight);
                        //        //if (!normalRedLineID.Equals(Guid.Empty))
                        //        //{
                        //        //    myDoc.Objects.Delete(normalRedLineID, true);
                        //        //}
                        //        //normalRedLineID = myDoc.Objects.AddLine(normalLine, redAttribute);
                        //        //myDoc.Views.Redraw();
                        //        //currBody.deployedEmbedType.Add(embedType);

                        //        // update the part's Guid and pin information since the part is added to the scene
                        //        int oldIdx = -1;
                        //        foreach (Part3D prt3D1 in dataset.part3DList)
                        //        {
                        //            if (prt3D1.PartName.Equals(prt3D.PartName))
                        //            {
                        //                oldIdx = dataset.part3DList.IndexOf(prt3D1);
                        //            }
                        //        }
                        //        dataset.part3DList.ElementAt(oldIdx).PrtID = finalPartPosID;
                        //        dataset.part3DList.ElementAt(oldIdx).IsDeployed = true;
                        //        dataset.part3DList.ElementAt(oldIdx).PartPos = new_part_pos;


                        //        // translation: partTranslation
                        //        // rotation: partRotation
                        //        foreach (Pin p in dataset.part3DList.ElementAt(oldIdx).Pins)
                        //        {
                        //            Point3d pin_pos = p.Pos;
                        //            pin_pos.Transform(partTranslation);
                        //            pin_pos.Transform(partRotation);
                        //            int idx = dataset.part3DList.ElementAt(oldIdx).Pins.IndexOf(p);
                        //            dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = pin_pos;
                        //        }
                        //        dataset.part3DList.ElementAt(oldIdx).Normal = outwardDir;


                        //        dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Clear();
                        //        dataset.part3DList.ElementAt(oldIdx).TransformSets.Clear();

                        //        dataset.part3DList.ElementAt(oldIdx).TransformSets.Add(partTranslation);
                        //        dataset.part3DList.ElementAt(oldIdx).TransformSets.Add(partRotation);

                        //        dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Add(retractPartRotation);
                        //        dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Add(retractPartTraslation);

                        //        #region rotate and flip back the part, pins, and its socket

                        //        Guid newPartID = dataset.part3DList.ElementAt(oldIdx).PrtID;
                        //        Guid newPartTemp1ID = Guid.Empty;
                        //        Guid newPartTemp2ID = Guid.Empty;

                        //        Transform rotateAgain = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).RotationAngle / 180.0 * Math.PI,
                        //                                                dataset.part3DList.ElementAt(prt_idx).Normal, dataset.part3DList.ElementAt(prt_idx).PartPos);
                        //        newPartTemp1ID = myDoc.Objects.Transform(newPartID, rotateAgain, true);

                        //        foreach (Pin p in dataset.part3DList.ElementAt(oldIdx).Pins)
                        //        {
                        //            Point3d p_pos = p.Pos;
                        //            p_pos.Transform(rotateAgain);
                        //            int idx = dataset.part3DList.ElementAt(oldIdx).Pins.IndexOf(p);
                        //            dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = p_pos;
                        //        }

                        //        foreach (partSocket prtS in currBody.partSockets)
                        //        {
                        //            if (prtS.PrtName.Equals(tempsocket.PrtName))
                        //            {
                        //                socketIdx = currBody.partSockets.IndexOf(prtS);
                        //                break;
                        //            }
                        //        }

                        //        Brep tempRotBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        //        tempRotBrep.Transform(rotateAgain);
                        //        currBody.partSockets.ElementAt(socketIdx).SocketBrep = tempRotBrep;

                        //        if (dataset.part3DList.ElementAt(prt_idx).IsFlipped)
                        //        {
                        //            Vector3d reverseNormalVector = dataset.part3DList.ElementAt(prt_idx).Normal * (-1);
                        //            Transform flipTrans = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).Normal, reverseNormalVector, dataset.part3DList.ElementAt(prt_idx).PartPos);

                        //            newPartTemp2ID = myDoc.Objects.Transform(newPartTemp1ID, flipTrans, true);

                        //            Brep tempRotBrep1 = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        //            tempRotBrep1.Transform(flipTrans);
                        //            currBody.partSockets.ElementAt(socketIdx).SocketBrep = tempRotBrep1;

                        //            foreach (Pin p in dataset.part3DList.ElementAt(oldIdx).Pins)
                        //            {
                        //                Point3d p_pos = p.Pos;
                        //                p_pos.Transform(flipTrans);
                        //                int idx = dataset.part3DList.ElementAt(oldIdx).Pins.IndexOf(p);
                        //                dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = p_pos;
                        //            }
                        //            dataset.part3DList.ElementAt(oldIdx).PartPos.Transform(flipTrans);
                        //        }
                        //        else
                        //        {
                        //            newPartTemp2ID = newPartTemp1ID;
                        //        }
                        //        dataset.part3DList.ElementAt(oldIdx).PrtID = newPartTemp2ID;
                        //        #endregion

                        //        myDoc.Objects.Delete(finalSocketID, true);
                        //        myDoc.Views.Redraw();
                        //        break;
                        //    //}

                        //    #endregion
                        //}

                        //#endregion

                        //#endregion
                        #endregion

                        #region Step 5: transform the part to the new place

                        var embedTranslation = Transform.Translation(outwardDir * (-1) * prt_height * hideratio);
                        Point3d new_part_pos = newPos;
                        new_part_pos.Transform(embedTranslation);

                        //Point3d new_ori_pos = myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center;
                        Point3d new_ori_pos = old_prt_pos;
                        Vector3d moveVector = new_part_pos - new_ori_pos;
                        Vector3d retractMoveVector = new_ori_pos - new_part_pos;
                        var partTranslation = Transform.Translation(moveVector);
                        var retractPartTraslation = Transform.Translation(retractMoveVector);
                        Guid transit_ID = myDoc.Objects.Transform(prt3D.PrtID, partTranslation, true);
                        old_prt_pos.Transform(partTranslation);

                        Vector3d startDirVector = new Vector3d(0, 0, 1);
                        Vector3d endDirVector = outwardDir;
                        var partRotation = Transform.Rotation(startDirVector, endDirVector, new_part_pos);
                        var retractPartRotation = Transform.Rotation(endDirVector, startDirVector, new_part_pos);
                        Guid rotate_ID = myDoc.Objects.Transform(transit_ID, partRotation, true);
                        old_prt_pos.Transform(partRotation);

                        prt3D.PrtID = rotate_ID;
                        prt3D.Normal = outwardDir;

                        #region update the pins

                        foreach (Pin p in prt3D.Pins)
                        {
                            Point3d temp_Pt = p.Pos;

                            temp_Pt.Transform(partTranslation);
                            temp_Pt.Transform(partRotation);

                            int idx = prt3D.Pins.IndexOf(p);
                            prt3D.Pins.ElementAt(idx).Pos = temp_Pt;
                        }

                        #endregion

                        #region update the socket

                        prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        prtBrep.Transform(partTranslation);
                        prtBrep.Transform(partRotation);
                        currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                        #endregion

                        #endregion

                                                
                        #region Setp 6: flip back if possible

                        Vector3d reverseNormalVector1 = prt3D.Normal * (-1);
                        Transform flipTrans1 = Transform.Rotation(prt3D.Normal, reverseNormalVector1, old_prt_pos);
                        if (prt3D.IsFlipped)
                        {
                            Guid newID = myDoc.Objects.Transform(prt3D.PrtID, flipTrans1, true);
                            prt3D.PrtID = newID;
                            old_prt_pos.Transform(flipTrans1);

                            #region update the pins

                            foreach (Pin p in prt3D.Pins)
                            {
                                Point3d temp_pt = p.Pos;

                                temp_pt.Transform(flipTrans1);

                                int idx = prt3D.Pins.IndexOf(p);
                                prt3D.Pins.ElementAt(idx).Pos = temp_pt;
                            }

                            #endregion

                            #region update the socket

                            prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                            prtBrep.Transform(flipTrans1);
                            currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                            #endregion
                        }

                        #endregion

                        #region Step 7: set the exposure level

                        Transform setExpTrans = Transform.Translation(prt3D.Normal * prt_exp * prt_height);
                        Guid newID1 = myDoc.Objects.Transform(prt3D.PrtID, setExpTrans, true);
                        prt3D.PrtID = newID1;
                        old_prt_pos.Transform(setExpTrans);

                        #region update the pins

                        foreach (Pin p in prt3D.Pins)
                        {
                            Point3d temp_pt = p.Pos;

                            temp_pt.Transform(setExpTrans);

                            int idx = prt3D.Pins.IndexOf(p);
                            prt3D.Pins.ElementAt(idx).Pos = temp_pt;
                        }

                        #endregion

                        #region update the socket

                        prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        prtBrep.Transform(setExpTrans);
                        currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                        #endregion

                        #endregion

                        #region Finally, update the part information

                        prt3D.PartPos = old_prt_pos;

                        prt3D.TransformReverseSets.Clear();
                        prt3D.TransformSets.Clear();

                        prt3D.TransformSets.Add(partTranslation);
                        prt3D.TransformSets.Add(partRotation);
                        prt3D.TransformReverseSets.Add(retractPartRotation);
                        prt3D.TransformReverseSets.Add(retractPartTraslation);

                        #endregion

                        myDoc.Views.Redraw();

                        #region check if sockets overlap

                        bool isPosValid = true;
                        Brep socketBrep = currBody.bodyBrep;

                        if (currBody.partSockets.Count > 0)
                        {
                            List<Brep> exitSockets = new List<Brep>();
                            foreach (partSocket prtSocket in currBody.partSockets)
                            {
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
                            ValidationWarningWin v_warning_win = new ValidationWarningWin();
                            v_warning_win.Show();
                        }

                        #endregion

                    }
                    //else
                    //{
                    //    undeployedParts.Add(prt3D);
                    //}
                }

                #region old version (commented)

                //foreach (Part3D temp in undeployedParts)
                //{
                    
                //    List<string> connectedPartNames = new List<string>();
                //    string ppName = temp.PartName.Substring(temp.PartName.IndexOf('-') + 1, temp.PartName.Length - temp.PartName.IndexOf('-') - 1);

                //    foreach (ConnectionNet cnet in dataset.connectionNetList)
                //    {
                //        bool isConnected = false;

                //        foreach (PartPinPair ppp in cnet.PartPinPairs)
                //        {
                //            if (ppp.PartName.Equals(ppName))
                //            {
                //                // all the other parts in this connection net should be added to connectedParts list
                //                isConnected = true;
                //                break;
                //            }
                //        }

                //        if (isConnected)
                //        {
                //            foreach (PartPinPair ppp in cnet.PartPinPairs)
                //            {
                //                if (!ppp.PartName.Equals(ppName))
                //                {
                //                    connectedPartNames.Add(ppp.PartName);
                //                }
                //            }
                //        }
                //    }

                //    Point3d unknownPos = new Point3d(0, 0, 0);

                //    int connectDeoplyedPartNum = 0;

                //    foreach (string cn in connectedPartNames)
                //    {
                //        foreach (Part3D p in dataset.part3DList)
                //        {
                //            string pName = p.PartName.Substring(p.PartName.IndexOf('-') + 1, p.PartName.Length - p.PartName.IndexOf('-') - 1);

                //            if (cn.Equals(pName) && p.IsDeployed)
                //            {
                //                connectDeoplyedPartNum++;
                //                unknownPos += p.PartPos;
                //            }
                //        }
                //    }

                //    if (connectDeoplyedPartNum == 1)
                //    {
                //        // only one connected part is deployed
                //        Transform moveOnePart = Transform.Rotation(30.0 / 180 * Math.PI, new Vector3d(0, 0, 1), myDoc.Objects.Find(currBody.objID).Geometry.GetBoundingBox(true).Center);
                //        unknownPos.Transform(moveOnePart);
                //    }
                //    else
                //    {
                //        // more than one connected parts are deployed
                //        unknownPos = unknownPos / connectedPartNames.Count;
                //    }
                //    temp.PartPos = unknownPos;
                //    Point3d knownPos = currBody.bodyBrep.ClosestPoint(unknownPos);

                //    #region deploy the undeployed part

                //    #region Step 1: import the STL of the 3D model and position it 40 mm above the 3D model

                //    Point3d obj_pos = currBody.bodyBrep.GetBoundingBox(true).Center;
                //    Point3d new_ori_pos = new Point3d(obj_pos.X, obj_pos.Y, obj_pos.Z + 40);

                //    string script = string.Format("_-Import \"{0}\" _Enter", temp.ModelPath);
                //    Rhino.RhinoApp.RunScript(script, false);

                //    BoundingBox partBoundingBox = new BoundingBox();
                //    Guid recoverPartPosID = Guid.Empty;
                //    Guid importedObjID = Guid.Empty;

                //    int oldIdx = -1;
                //    foreach (Part3D prt3D in dataset.part3DList)
                //    {
                //        if (prt3D.PartName.Equals(temp.PartName))
                //        {
                //            oldIdx = dataset.part3DList.IndexOf(prt3D);
                //            break;
                //        }
                //    }

                //    foreach (var obj in myDoc.Objects.GetSelectedObjects(false, false))
                //    {
                //        importedObjID = obj.Attributes.ObjectId;

                //        Point3d ori_pos = obj.Geometry.GetBoundingBox(true).Center;
                //        Vector3d resetPos = new_ori_pos - ori_pos;

                //        var xform = Transform.Translation(resetPos);
                //        recoverPartPosID = myDoc.Objects.Transform(importedObjID, xform, true);

                //        partBoundingBox = myDoc.Objects.Find(recoverPartPosID).Geometry.GetBoundingBox(true);

                //        // update the part position and pin positions, both selPrt and dataset's part3Dlist
                //        Point3d p_pos = temp.PartPos;
                //        p_pos.Transform(xform);
                //        temp.PartPos = p_pos;
                //        dataset.part3DList.ElementAt(oldIdx).PartPos = p_pos;

                //        foreach (Pin pin_info in temp.Pins)
                //        {
                //            Point3d pin_pos = pin_info.Pos;

                //            pin_pos.Transform(xform);

                //            int idx = temp.Pins.IndexOf(pin_info);
                //            temp.Pins.ElementAt(idx).Pos = pin_pos;

                //            dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = pin_pos;
                //        }

                //        myDoc.Views.Redraw();
                //        break;
                //    }

                //    #endregion

                //    #region Step 2: Find all the points as candidate positions on the surface of the 3D model and ask the user the select one position

                //    #region convert the selected brep into a mesh

                //    var brep = currBody.bodyBrep;
                //    if (brep == null) return;

                //    var default_mesh_params = MeshingParameters.Default;
                //    var minimal = MeshingParameters.Minimal;

                //    var meshes = Mesh.CreateFromBrep(brep, default_mesh_params);
                //    if (meshes == null || meshes.Length == 0) return;

                //    var brep_mesh = new Mesh(); // the mesh of the currently selected body
                //    foreach (var mesh in meshes)
                //        brep_mesh.Append(mesh);
                //    brep_mesh.Faces.ConvertQuadsToTriangles();

                //    #endregion

                //    #region prepare the points on the model surface

                //    BoundingBox meshBox = brep_mesh.GetBoundingBox(true);
                //    double w = meshBox.Max.X - meshBox.Min.X;
                //    double l = meshBox.Max.Y - meshBox.Min.Y;
                //    double h = meshBox.Max.Z - meshBox.Min.Z;

                //    if (isFirstTime)
                //    {
                //        currBody.surfPts.Clear();

                //        prcWin.Show();

                //        for (int i = 0; i < w; i++)
                //        {
                //            for (int j = 0; j < l; j++)
                //            {
                //                for (int t = 0; t < h; t++)
                //                {
                //                    Point3d ptInSpace = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, t + meshBox.Min.Z);
                //                    if (brep.ClosestPoint(ptInSpace).DistanceTo(ptInSpace) <= 0.5)
                //                    {
                //                        currBody.surfPts.Add(brep.ClosestPoint(ptInSpace));
                //                    }
                //                }
                //            }
                //        }

                //        prcWin.Hide();
                //        isFirstTime = false;
                //    }

                //    #endregion

                //    #region select the points on the surface and drop the part on that spot

                //    while (true)
                //    {
                //        #region Step 1: find the closest point on the selected object's surface

                //        Point3d ptOnSurf = ((Brep)myDoc.Objects.Find(currBody.objID).Geometry).ClosestPoint(knownPos);

                //        #endregion

                //        #region Step 2: get the normal direction at this point on the surface

                //        ObjRef currObjRef = new ObjRef(currBody.objID);
                //        Vector3d outwardDir = new Vector3d();

                //        Point3d closestPt = new Point3d();
                //        double u, v;
                //        ComponentIndex i;
                //        currBody.bodyBrep.ClosestPoint(ptOnSurf, out closestPt, out i, out u, out v, 2, out outwardDir);

                //        List<Brep> projectedBrep = new List<Brep>();
                //        projectedBrep.Add(currBody.bodyBrep);
                //        List<Point3d> projectPt = new List<Point3d>();
                //        projectPt.Add(closestPt);

                //        var intersectionPts = Intersection.ProjectPointsToBreps(projectedBrep, projectPt, outwardDir, myDoc.ModelAbsoluteTolerance);
                //        if (intersectionPts.Length == 1)
                //        {
                //            outwardDir = (-1) * outwardDir;
                //        }

                //        #endregion

                //        #region Step 3: move the imported part to the final location (make sure the part is entirely hidden in the model)

                //        double hideratio = 0.5;

                //        double partHeight = partBoundingBox.GetCorners().ElementAt(4).Z - partBoundingBox.GetCorners().ElementAt(0).Z;
                //        var embedTranslation = Transform.Translation(outwardDir * (-1) * partHeight * hideratio);
                //        Point3d new_part_pos = ptOnSurf;
                //        new_part_pos.Transform(embedTranslation);

                //        Vector3d moveVector = new_part_pos - new_ori_pos;
                //        Vector3d retractMoveVector = new_ori_pos - new_part_pos;
                //        var partTranslation = Transform.Translation(moveVector);
                //        var retractPartTraslation = Transform.Translation(retractMoveVector);
                //        Guid transit_ID = myDoc.Objects.Transform(recoverPartPosID, partTranslation, true);

                //        #endregion

                //        #region Step 4: create the truncated pyramid socket

                //        // create the solid box based on the bounding box before rotation
                //        partBoundingBox = myDoc.Objects.Find(transit_ID).Geometry.GetBoundingBox(true);

                //        // make the socket a bit larger than the part (currently, ~0.5mm bigger)
                //        partBoundingBox.Inflate(0.1);
                //        Box socketBox = new Box(partBoundingBox);
                //        BoundingBox scktBoundingBox = socketBox.BoundingBox;
                //        Brep truncatedSocket = scktBoundingBox.ToBrep();
                //        //// create a truncated pyramid
                //        //double offsetTP = (scktBoundingBox.GetCorners()[4].Z - scktBoundingBox.GetCorners()[0].Z) * Math.Sqrt(2) * 0.5;
                //        //Point3d n4 = scktBoundingBox.GetCorners()[4] + new Vector3d(-offsetTP, -offsetTP, 0);
                //        //Point3d n5 = scktBoundingBox.GetCorners()[5] + new Vector3d(offsetTP, -offsetTP, 0);
                //        //Point3d n6 = scktBoundingBox.GetCorners()[6] + new Vector3d(offsetTP, offsetTP, 0);
                //        //Point3d n7 = scktBoundingBox.GetCorners()[7] + new Vector3d(-offsetTP, offsetTP, 0);
                //        //Point3d n8 = n4;
                //        //List<Point3d> topRectCorners = new List<Point3d>();
                //        //topRectCorners.Add(n4);
                //        //topRectCorners.Add(n5);
                //        //topRectCorners.Add(n6);
                //        //topRectCorners.Add(n7);
                //        //topRectCorners.Add(n8);

                //        //Point3d o0 = scktBoundingBox.GetCorners()[0];
                //        //Point3d o1 = scktBoundingBox.GetCorners()[1];
                //        //Point3d o2 = scktBoundingBox.GetCorners()[2];
                //        //Point3d o3 = scktBoundingBox.GetCorners()[3];
                //        //Point3d o4 = o0;
                //        //List<Point3d> bottomRectCorners = new List<Point3d>();
                //        //bottomRectCorners.Add(o0);
                //        //bottomRectCorners.Add(o1);
                //        //bottomRectCorners.Add(o2);
                //        //bottomRectCorners.Add(o3);
                //        //bottomRectCorners.Add(o4);

                //        //Polyline topRect = new Polyline(topRectCorners);
                //        //Polyline bottomRect = new Polyline(bottomRectCorners);
                //        //Curve topRectCrv = topRect.ToNurbsCurve();
                //        //Curve bottomRectCrv = bottomRect.ToNurbsCurve();
                //        //Line crossLn = new Line(o0, n4);
                //        //Curve crossCrv = crossLn.ToNurbsCurve();

                //        //var sweep = new SweepOneRail();
                //        //sweep.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                //        //sweep.ClosedSweep = false;
                //        //sweep.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                //        ////myDoc.Objects.AddCurve(topRectCrv, orangeAttribute);
                //        ////myDoc.Views.Redraw();
                //        ////myDoc.Objects.AddCurve(bottomRectCrv, orangeAttribute);
                //        ////myDoc.Views.Redraw();
                //        ////myDoc.Objects.AddCurve(crossCrv, orangeAttribute);
                //        ////myDoc.Views.Redraw();

                //        //Brep[] tpBreps = sweep.PerformSweep(bottomRectCrv, crossCrv);
                //        //Brep tpBrep = tpBreps[0];
                //        //Brep truncatedSocket = tpBrep.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);

                //        myDoc.Views.Redraw();

                //        #endregion

                //        #region Step 5: rotate the part to the normal direction

                //        Vector3d startDirVector = new Vector3d(0, 0, 1);
                //        Vector3d endDirVector = outwardDir;
                //        var partRotation = Transform.Rotation(startDirVector, endDirVector, new_part_pos);
                //        var retractPartRotation = Transform.Rotation(endDirVector, startDirVector, new_part_pos);

                //        // no intersections so that the part can be added to this location inside the 3D model
                //        Guid socketBoxID = myDoc.Objects.AddBrep(truncatedSocket, orangeAttribute);
                //        // get the final socket box after rotation
                //        Guid finalSocketID = myDoc.Objects.Transform(socketBoxID, partRotation, true);
                //        // situate the part in the final position and orientation
                //        Guid finalPartPosID = myDoc.Objects.Transform(transit_ID, partRotation, true);

                //        // Extend the box a bit more outside the selected solid
                //        Transform differenceMove = Transform.Translation(outwardDir * (partHeight / 2 - 0.1));
                //        Guid extendedSocketBoxID = myDoc.Objects.Transform(finalSocketID, differenceMove, false);

                //        Brep currentSelectedBrep = currBody.bodyBrep;
                //        Brep toRemoveBrep = (Brep)myDoc.Objects.Find(extendedSocketBoxID).Geometry;
                //        // Test if the added electrical component can be added and hiden in this position
                //        Brep dupCurrentSelectedBrep = currentSelectedBrep.DuplicateBrep();
                //        Brep dupToRemoveSocketBrep = toRemoveBrep.DuplicateBrep();
                //        var leftObj = Brep.CreateBooleanDifference(dupToRemoveSocketBrep, dupCurrentSelectedBrep, myDoc.ModelAbsoluteTolerance);
                //        myDoc.Objects.Delete(extendedSocketBoxID, true);

                //        //if (leftObj.Count() != 1)
                //        //{
                //        //    // revert the position of the imported part model
                //        //    Guid retractFinalPartPosID = myDoc.Objects.Transform(finalPartPosID, retractPartRotation, true);
                //        //    recoverPartPosID = myDoc.Objects.Transform(retractFinalPartPosID, retractPartTraslation, true);
                //        //    myDoc.Objects.Delete(finalSocketID, true);
                //        //    myDoc.Views.Redraw();
                //        //    continue;
                //        //}
                //        //else
                //        //{

                //            // confirm that the part is added to the model
                //            partSocket tempSocket = new partSocket();
                //            tempSocket.PrtName = temp.PartName;
                //            tempSocket.SocketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;

                //            bool isExistSocket = false;
                //            int socketIdx = -1;
                //            foreach (partSocket prtS in currBody.partSockets)
                //            {
                //                if (prtS.PrtName.Equals(tempSocket.PrtName))
                //                {
                //                    isExistSocket = true;
                //                    socketIdx = currBody.partSockets.IndexOf(prtS);
                //                    break;
                //                }
                //            }

                //            if (!isExistSocket)
                //            {
                //                currBody.partSockets.Add(tempSocket);
                //            }
                //            else
                //            {
                //                currBody.partSockets.ElementAt(socketIdx).SocketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;
                //            }


                //            // draw the normal for part rotation and flip manipulations
                //            //Line normalLine = new Line(new_part_pos, outwardDir * partHeight);
                //            //if (!normalRedLineID.Equals(Guid.Empty))
                //            //{
                //            //    myDoc.Objects.Delete(normalRedLineID, true);
                //            //}
                //            //normalRedLineID = myDoc.Objects.AddLine(normalLine, redAttribute);
                //            //myDoc.Views.Redraw();
                //            //currBody.deployedEmbedType.Add(embedType);

                //            // update the part's Guid and pin information since the part is added to the scene
                           
                //            dataset.part3DList.ElementAt(oldIdx).PrtID = finalPartPosID;
                //            dataset.part3DList.ElementAt(oldIdx).IsDeployed = true;
                //            dataset.part3DList.ElementAt(oldIdx).PartPos = new_part_pos;

                //            // translation: partTranslation
                //            // rotation: partRotation
                //            foreach (Pin p in dataset.part3DList.ElementAt(oldIdx).Pins)
                //            {
                //                Point3d pin_pos = p.Pos;
                //                pin_pos.Transform(partTranslation);
                //                pin_pos.Transform(partRotation);

                //                int idx = dataset.part3DList.ElementAt(oldIdx).Pins.IndexOf(p);
                //                dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = pin_pos;
                //            }

                //            dataset.part3DList.ElementAt(oldIdx).Normal = outwardDir;

                //            dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Clear();
                //            dataset.part3DList.ElementAt(oldIdx).TransformSets.Clear();

                //            dataset.part3DList.ElementAt(oldIdx).TransformSets.Add(partTranslation);
                //            dataset.part3DList.ElementAt(oldIdx).TransformSets.Add(partRotation);

                //            dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Add(retractPartRotation);
                //            dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Add(retractPartTraslation);

                //            myDoc.Objects.Delete(finalSocketID, true);
                //            myDoc.Views.Redraw();
                //            break;
                //        //}

                //        #endregion
                //    }

                //    #endregion

                //    #endregion

                //    #endregion

                //}

                #endregion
            }
        }

        /// <summary>
        /// Find the closest point from a surface
        /// </summary>
        /// <param name="target">the target point</param>
        /// <param name="objID">the model that has the target surface</param>
        /// <returns>The closet point on the surface</returns>
        Point3d FindCloesetPointFromSurface(Point3d target, Guid objID)
        {
            Point3d result = new Point3d(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue); // dumb point

            ObjRef dupObjRef = new ObjRef(objID);
            result = dupObjRef.Brep().ClosestPoint(target);

            return result;
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
            get { return new Guid("3b18afc9-6c0a-40ef-8555-8434de359407"); }
        }
    }
}