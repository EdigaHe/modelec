using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;

namespace ModelecComponents
{
    public class SetPartPosComponent : GH_Component
    {
        bool testBtnClick;
        int count;
        RhinoDoc myDoc;
        ObjectAttributes solidAttribute, orangeAttribute, redAttribute, blueAttribute;
        Point3d selPrtPosPt;
        TargetBody currBody;
        SharedData dataset;
        ProcessingWindow prcWin;

        /// <summary>
        /// Initializes a new instance of the SetPartPosComponent class.
        /// </summary>
        public SetPartPosComponent()
          : base("SetPartPosComponent", "SPP",
              "Set the position of the selected part",
              "ModElec", "PartOperations")
        {
            testBtnClick = false;
            count = 0;
            myDoc = RhinoDoc.ActiveDoc;
            selPrtPosPt = new Point3d();
            currBody = TargetBody.Instance;
            dataset = SharedData.Instance;
            prcWin = new ProcessingWindow();

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
            pManager.AddBooleanParameter("SetPosBtnClicked", "BC", "Set the position for the selected part", GH_ParamAccess.item);
            pManager.AddGenericParameter("Part3D", "P3D", "The ModElec part unit", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Data", "Dataset", "The dataset shared with all the components", GH_ParamAccess.item);
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
            bool toExePrtPosSet = false;
            Part3D selPrt = new Part3D();

            #region read the button click and decide whether execute the part position set or not
            if (!DA.GetData(0, ref btnClick))
                return;

            if (!DA.GetData(1, ref selPrt))
                return;

            if (!btnClick && testBtnClick && selPrt.PartName != "")
            {
                toExePrtPosSet = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }

            #endregion

            if (toExePrtPosSet)
            {
                // deselect all selected objects
                myDoc.Objects.UnselectAll();

                #region old version
                //#region Step 0: Ask the user to select the target Brep if the body is not selected

                //if (myDoc.Objects.Find(currBody.objID) == null)
                //{
                //    // the model body has not been selected or the model body has been transformed 

                //    ObjRef objSel_ref;
                //    Guid selObjId = Guid.Empty;
                //    var rc = RhinoGet.GetOneObject("Select a model (brep)", false, ObjectType.AnyObject, out objSel_ref);
                //    if (rc == Rhino.Commands.Result.Success)
                //    {
                //        selObjId = objSel_ref.ObjectId;
                //        currBody.objID = selObjId;
                //        ObjRef currObj = new ObjRef(selObjId);
                //        currBody.bodyBrep = currObj.Brep();
                //    }
                //}

                //#endregion

                //#region Step 0.1: create a new layer if not exist to store all parts

                //if(myDoc.Layers.FindName("Parts") == null)
                //{
                //    // create a new layer named "Parts"
                //    string layer_name = "Parts";
                //    Layer p_ly = myDoc.Layers.CurrentLayer;

                //    Layer new_ly = new Layer();
                //    new_ly.Name = layer_name;
                //    new_ly.ParentLayerId = p_ly.ParentLayerId;
                //    new_ly.Color = Color.Beige;

                //    int index = myDoc.Layers.Add(new_ly);

                //    myDoc.Layers.SetCurrentLayerIndex(index, true);

                //    if (index < 0) return;
                //}
                //else
                //{
                //    int index = myDoc.Layers.FindName("Parts").Index;
                //    myDoc.Layers.SetCurrentLayerIndex(index, true);
                //}

                //#endregion

                //bool isDeployedReal = false;
                //foreach(Part3D prt in dataset.part3DList)
                //{
                //    if (prt.PartName.Equals(selPrt.PartName))
                //    {
                //        isDeployedReal = prt.IsDeployed;
                //        break;
                //    }
                //}

                //if (!isDeployedReal)
                //{

                //    #region Step 1: import the STL of the 3D model and position it 40 mm above the 3D model

                //    Point3d obj_pos = currBody.bodyBrep.GetBoundingBox(true).Center;
                //    Point3d new_ori_pos = new Point3d(obj_pos.X, obj_pos.Y, obj_pos.Z + 40);

                //    string script = string.Format("_-Import \"{0}\" _Enter", selPrt.ModelPath);
                //    Rhino.RhinoApp.RunScript(script, false);

                //    BoundingBox partBoundingBox = new BoundingBox();
                //    Guid recoverPartPosID = Guid.Empty;
                //    Guid importedObjID = Guid.Empty;

                //    int oldIdx = -1;
                //    foreach (Part3D prt3D in dataset.part3DList)
                //    {
                //        if (prt3D.PartName.Equals(selPrt.PartName))
                //        {
                //            oldIdx = dataset.part3DList.IndexOf(prt3D);
                //            break;
                //        }
                //    }

                //    #region enumerate all the meshes in the imported STL file: update the pin positions accordingly and union all the meshes

                //    if(myDoc.Objects.GetSelectedObjects(false, false).Count() > 1)
                //    {
                //        List<double> vol_list = new List<double>();
                //        List<int> vol_idx_list = new List<int>();

                //        int pos = 0;
                //        foreach(RhinoObject rObj in myDoc.Objects.GetSelectedObjects(false, false))
                //        {
                //            double vGet = rObj.Geometry.GetBoundingBox(true).Volume;
                //            vol_list.Add(vGet);

                //            vol_idx_list.Add(pos);
                //            pos++;
                //        }

                //        List<int> idx_list = new List<int>();

                //        var query = vol_list.GroupBy(x => x)
                //                  .Where(g => g.Count() > 1)
                //                  .Select(y => y.Key)
                //                  .ToList();

                //        if(query.Count != 0)
                //        {
                //            double target = query.ElementAt(0);

                //            foreach (double var in vol_list)
                //            {
                //                if (var == target)
                //                {
                //                    int idx = vol_list.IndexOf(var);
                //                    idx_list.Add(idx);
                //                }
                //            }

                //            foreach (int i in idx_list)
                //            {
                //                int obj_idx = vol_idx_list.ElementAt(i);
                //                RhinoObject robj = myDoc.Objects.GetSelectedObjects(false, false).ElementAt(obj_idx);

                //                Point3d new_pin_pos = robj.Geometry.GetBoundingBox(true).Center;

                //                Vector3d real_vect = selPrt.PartPos - new_pin_pos;

                //                int target_idx = -1;
                //                double angle = double.MaxValue;
                //                Point3d target_pos = new Point3d();

                //                foreach (Pin pin_info in selPrt.Pins)
                //                {
                //                    Point3d pin_pos = pin_info.Pos;
                //                    Vector3d test_vect = selPrt.PartPos - pin_pos;

                //                    #region find the correct pin and update the pin position
                //                    double an = Vector3d.VectorAngle(real_vect, test_vect);
                //                    if (an <= angle)
                //                    {
                //                        angle = an;
                //                        target_idx = selPrt.Pins.IndexOf(pin_info);
                //                        target_pos = pin_pos;
                //                    }

                //                    #endregion
                //                }

                //                selPrt.Pins.ElementAt(target_idx).Pos = target_pos;
                //                dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(target_idx).Pos = target_pos;
                //            }
                //        }

                //        #region boolean union all the meshes together
                //        List<Mesh> importedMeshes = new List<Mesh>();
                //        foreach(RhinoObject robj in myDoc.Objects.GetSelectedObjects(false, false))
                //        {
                //            Mesh m = (Mesh)robj.Geometry;
                //            importedMeshes.Add(m);
                //        }
                //        var final_mesh = Mesh.CreateBooleanUnion(importedMeshes);
                //        var obj = final_mesh[0];
                //        importedObjID = myDoc.Objects.AddMesh(obj);

                //        // delete the original meshes
                //        foreach (RhinoObject robj in myDoc.Objects.GetSelectedObjects(false, false))
                //        {
                //            Guid objID = robj.Attributes.ObjectId;
                //            myDoc.Objects.Delete(objID,true);
                //        }

                //        Point3d ori_pos = obj.GetBoundingBox(true).Center;
                //        Vector3d resetPos = new_ori_pos - ori_pos;

                //        var xform = Transform.Translation(resetPos);
                //        recoverPartPosID = myDoc.Objects.Transform(importedObjID, xform, true);

                //        partBoundingBox = myDoc.Objects.Find(recoverPartPosID).Geometry.GetBoundingBox(true);

                //        // update the part position and pin positions, both selPrt and dataset's part3Dlist
                //        Point3d p_pos = selPrt.PartPos;
                //        p_pos.Transform(xform);
                //        selPrt.PartPos = p_pos;
                //        dataset.part3DList.ElementAt(oldIdx).PartPos = p_pos;

                //        foreach (Pin pin_info in selPrt.Pins)
                //        {
                //            Point3d pin_pos = pin_info.Pos;

                //            pin_pos.Transform(xform);

                //            int idx = selPrt.Pins.IndexOf(pin_info);
                //            selPrt.Pins.ElementAt(idx).Pos = pin_pos;

                //            dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = pin_pos;
                //        }

                //        myDoc.Views.Redraw();
                //        #endregion
                //    }

                //    #endregion
                //    else
                //    {
                //        foreach (var obj in myDoc.Objects.GetSelectedObjects(false, false))
                //        {
                //            importedObjID = obj.Attributes.ObjectId;

                //            Point3d ori_pos = obj.Geometry.GetBoundingBox(true).Center;
                //            Vector3d resetPos = new_ori_pos - ori_pos;

                //            var xform = Transform.Translation(resetPos);
                //            recoverPartPosID = myDoc.Objects.Transform(importedObjID, xform, true);

                //            partBoundingBox = myDoc.Objects.Find(recoverPartPosID).Geometry.GetBoundingBox(true);

                //            // update the part position and pin positions, both selPrt and dataset's part3Dlist
                //            Point3d p_pos = selPrt.PartPos;
                //            p_pos.Transform(xform);
                //            selPrt.PartPos = p_pos;
                //            dataset.part3DList.ElementAt(oldIdx).PartPos = p_pos;

                //            foreach (Pin pin_info in selPrt.Pins)
                //            {
                //                Point3d pin_pos = pin_info.Pos;

                //                pin_pos.Transform(xform);

                //                int idx = selPrt.Pins.IndexOf(pin_info);
                //                selPrt.Pins.ElementAt(idx).Pos = pin_pos;

                //                dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = pin_pos;
                //            }

                //            myDoc.Views.Redraw();
                //        }
                //    }


                //    #endregion

                //    #region old version of the position selection

                //    //#region Step 2: Find all the points as candidate positions on the surface of the 3D model and ask the user the select one position

                //    //#region convert the selected brep into a mesh

                //    //var brep = currBody.bodyBrep;
                //    //if (brep == null) return;

                //    //var default_mesh_params = MeshingParameters.Default;
                //    //var minimal = MeshingParameters.Minimal;

                //    //var meshes = Mesh.CreateFromBrep(brep, default_mesh_params);
                //    //if (meshes == null || meshes.Length == 0) return;

                //    //var brep_mesh = new Mesh(); // the mesh of the currently selected body
                //    //foreach (var mesh in meshes)
                //    //    brep_mesh.Append(mesh);
                //    //brep_mesh.Faces.ConvertQuadsToTriangles();

                //    //#endregion

                //    //#region prepare the points on the model surface

                //    //BoundingBox meshBox = brep_mesh.GetBoundingBox(true);
                //    //double w = meshBox.Max.X - meshBox.Min.X;
                //    //double l = meshBox.Max.Y - meshBox.Min.Y;
                //    //double h = meshBox.Max.Z - meshBox.Min.Z;
                //    //currBody.surfPts.Clear();

                //    //// show the processing warning window
                //    //prcWin.Show();

                //    //for (int i = 0; i < w; i++)
                //    //{
                //    //    for (int j = 0; j < l; j++)
                //    //    {
                //    //        for (int t = 0; t < h; t++)
                //    //        {
                //    //            Point3d ptInSpace = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, t + meshBox.Min.Z);
                //    //            if (brep.ClosestPoint(ptInSpace).DistanceTo(ptInSpace) <= 0.5)
                //    //            {
                //    //                currBody.surfPts.Add(brep.ClosestPoint(ptInSpace));
                //    //            }
                //    //        }
                //    //    }
                //    //}
                //    //Brep solidDupBrep = currBody.bodyBrep.DuplicateBrep();
                //    //Guid dupObjID = myDoc.Objects.AddBrep(solidDupBrep, solidAttribute);
                //    //myDoc.Objects.Hide(currBody.objID, true);
                //    //myDoc.Views.Redraw();

                //    //prcWin.Hide();

                //    //#region render all the points on the surface

                //    //prcWin.Show();

                //    //List<Guid> pts_normals = new List<Guid>();
                //    //foreach (Point3d pt in currBody.surfPts)
                //    //{
                //    //    Guid ptID = myDoc.Objects.AddPoint(pt);
                //    //    pts_normals.Add(ptID);

                //    //    Vector3d outwardDir = new Vector3d();

                //    //    Point3d closestPt = new Point3d();
                //    //    double u, v;
                //    //    ComponentIndex i;
                //    //    currBody.bodyBrep.ClosestPoint(pt, out closestPt, out i, out u, out v, 2, out outwardDir);

                //    //    List<Brep> projectedBrep = new List<Brep>();
                //    //    projectedBrep.Add(currBody.bodyBrep);
                //    //    List<Point3d> projectPt = new List<Point3d>();
                //    //    projectPt.Add(closestPt);

                //    //    var intersectionPts = Intersection.ProjectPointsToBreps(projectedBrep, projectPt, outwardDir, myDoc.ModelAbsoluteTolerance);
                //    //    if (intersectionPts.Length == 1)
                //    //    {
                //    //        outwardDir = (-1) * outwardDir;
                //    //    }

                //    //    Line normal = new Line(pt, pt + outwardDir);
                //    //    Curve normalCrv = normal.ToNurbsCurve();
                //    //    Guid norID = myDoc.Objects.AddCurve(normalCrv, redAttribute);
                //    //    pts_normals.Add(norID);
                //    //}
                //    //myDoc.Views.Redraw();

                //    //prcWin.Hide();

                //    //#endregion

                //    //#endregion

                //    //#region select the points on the surface and drop the part on that spot

                //    //while (true)
                //    //{
                //    //    Rhino.Input.Custom.GetPoint ec_pos = new Rhino.Input.Custom.GetPoint();
                //    //    ec_pos.SetCommandPrompt("drop the electrical component");
                //    //    ec_pos.MouseMove += Ec_pos_MouseMove;
                //    //    ec_pos.DynamicDraw += Ec_pos_DynamicDraw;
                //    //    ec_pos.Get(true);

                //    //    foreach (Guid pnID in pts_normals)
                //    //    {
                //    //        myDoc.Objects.Delete(pnID, true);
                //    //    }
                //    //    // delete the solid body brep
                //    //    myDoc.Objects.Delete(dupObjID, true);
                //    //    myDoc.Objects.Show(currBody.objID, true);
                //    //    myDoc.Views.Redraw();

                //    //    if (ec_pos.Point().X == Int32.MaxValue) return;

                //    //    #region Step 1: find the closest point on the selected object's surface

                //    //    Point3d ptOnSurf = FindCloesetPointFromSurface(ec_pos.Point(), currBody.objID);

                //    //    #endregion

                //    //    #region Step 2: get the normal direction at this point on the surface

                //    //    ObjRef currObjRef = new ObjRef(currBody.objID);
                //    //    Vector3d outwardDir = new Vector3d();

                //    //    Point3d closestPt = new Point3d();
                //    //    double u, v;
                //    //    ComponentIndex i;
                //    //    currBody.bodyBrep.ClosestPoint(ptOnSurf, out closestPt, out i, out u, out v, 2, out outwardDir);

                //    //    List<Brep> projectedBrep = new List<Brep>();
                //    //    projectedBrep.Add(currBody.bodyBrep);
                //    //    List<Point3d> projectPt = new List<Point3d>();
                //    //    projectPt.Add(closestPt);

                //    //    var intersectionPts = Intersection.ProjectPointsToBreps(projectedBrep, projectPt, outwardDir, myDoc.ModelAbsoluteTolerance);
                //    //    if (intersectionPts.Length == 1)
                //    //    {
                //    //        outwardDir = (-1) * outwardDir;
                //    //    }

                //    //    #endregion

                //    //    #region Step 3: move the imported part to the final location (make sure the part is entirely hidden in the model)

                //    //    double hideratio = 0.5;

                //    //    double partHeight = partBoundingBox.GetCorners().ElementAt(4).Z - partBoundingBox.GetCorners().ElementAt(0).Z;
                //    //    var embedTranslation = Transform.Translation(outwardDir * (-1) * partHeight * hideratio);
                //    //    Point3d new_part_pos = ptOnSurf;
                //    //    new_part_pos.Transform(embedTranslation);

                //    //    Vector3d moveVector = new_part_pos - new_ori_pos;
                //    //    Vector3d retractMoveVector = new_ori_pos - new_part_pos;
                //    //    var partTranslation = Transform.Translation(moveVector);
                //    //    var retractPartTraslation = Transform.Translation(retractMoveVector);
                //    //    Guid transit_ID = myDoc.Objects.Transform(recoverPartPosID, partTranslation, true);

                //    //    #endregion

                //    //    #region Step 4: create the truncated pyramid socket

                //    //    // create the solid box based on the bounding box before rotation
                //    //    partBoundingBox = myDoc.Objects.Find(transit_ID).Geometry.GetBoundingBox(true);

                //    //    // make the socket a bit larger than the part (currently, ~0.5mm bigger)
                //    //    partBoundingBox.Inflate(0.1);
                //    //    Box socketBox = new Box(partBoundingBox);
                //    //    BoundingBox scktBoundingBox = socketBox.BoundingBox;
                //    //    Brep truncatedSocket = scktBoundingBox.ToBrep();

                //    //    //// create a truncated pyramid
                //    //    //double offsetTP = (scktBoundingBox.GetCorners()[4].Z - scktBoundingBox.GetCorners()[0].Z) * Math.Sqrt(2) * 0.5;
                //    //    //Point3d n4 = scktBoundingBox.GetCorners()[4] + new Vector3d(-offsetTP, -offsetTP, 0);
                //    //    //Point3d n5 = scktBoundingBox.GetCorners()[5] + new Vector3d(offsetTP, -offsetTP, 0);
                //    //    //Point3d n6 = scktBoundingBox.GetCorners()[6] + new Vector3d(offsetTP, offsetTP, 0);
                //    //    //Point3d n7 = scktBoundingBox.GetCorners()[7] + new Vector3d(-offsetTP, offsetTP, 0);
                //    //    //Point3d n8 = n4;
                //    //    //List<Point3d> topRectCorners = new List<Point3d>();
                //    //    //topRectCorners.Add(n4);
                //    //    //topRectCorners.Add(n5);
                //    //    //topRectCorners.Add(n6);
                //    //    //topRectCorners.Add(n7);
                //    //    //topRectCorners.Add(n8);

                //    //    //Point3d o0 = scktBoundingBox.GetCorners()[0];
                //    //    //Point3d o1 = scktBoundingBox.GetCorners()[1];
                //    //    //Point3d o2 = scktBoundingBox.GetCorners()[2];
                //    //    //Point3d o3 = scktBoundingBox.GetCorners()[3];
                //    //    //Point3d o4 = o0;
                //    //    //List<Point3d> bottomRectCorners = new List<Point3d>();
                //    //    //bottomRectCorners.Add(o0);
                //    //    //bottomRectCorners.Add(o1);
                //    //    //bottomRectCorners.Add(o2);
                //    //    //bottomRectCorners.Add(o3);
                //    //    //bottomRectCorners.Add(o4);

                //    //    //Polyline topRect = new Polyline(topRectCorners);
                //    //    //Polyline bottomRect = new Polyline(bottomRectCorners);
                //    //    //Curve topRectCrv = topRect.ToNurbsCurve();
                //    //    //Curve bottomRectCrv = bottomRect.ToNurbsCurve();
                //    //    //Line crossLn = new Line(o0, n4);
                //    //    //Curve crossCrv = crossLn.ToNurbsCurve();

                //    //    //var sweep = new SweepOneRail();
                //    //    //sweep.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                //    //    //sweep.ClosedSweep = false;
                //    //    //sweep.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                //    //    ////myDoc.Objects.AddCurve(topRectCrv, orangeAttribute);
                //    //    ////myDoc.Views.Redraw();
                //    //    ////myDoc.Objects.AddCurve(bottomRectCrv, orangeAttribute);
                //    //    ////myDoc.Views.Redraw();
                //    //    ////myDoc.Objects.AddCurve(crossCrv, orangeAttribute);
                //    //    ////myDoc.Views.Redraw();

                //    //    //Brep[] tpBreps = sweep.PerformSweep(bottomRectCrv, crossCrv);
                //    //    //Brep tpBrep = tpBreps[0];
                //    //    //Brep truncatedSocket = tpBrep.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);

                //    //    myDoc.Views.Redraw();

                //    //    #endregion

                //    //    #region Step 5: rotate the part to the normal direction

                //    //    Vector3d startDirVector = new Vector3d(0, 0, 1);
                //    //    Vector3d endDirVector = outwardDir;
                //    //    var partRotation = Transform.Rotation(startDirVector, endDirVector, new_part_pos);
                //    //    var retractPartRotation = Transform.Rotation(endDirVector, startDirVector, new_part_pos);

                //    //    // no intersections so that the part can be added to this location inside the 3D model
                //    //    Guid socketBoxID = myDoc.Objects.AddBrep(truncatedSocket, orangeAttribute);
                //    //    // get the final socket box after rotation
                //    //    Guid finalSocketID = myDoc.Objects.Transform(socketBoxID, partRotation, true);
                //    //    // situate the part in the final position and orientation
                //    //    Guid finalPartPosID = myDoc.Objects.Transform(transit_ID, partRotation, true);

                //    //    // Extend the box a bit more outside the selected solid
                //    //    Transform differenceMove = Transform.Translation(outwardDir * (partHeight / 2 - 0.1));
                //    //    Guid extendedSocketBoxID = myDoc.Objects.Transform(finalSocketID, differenceMove, false);

                //    //    Brep currentSelectedBrep = currBody.bodyBrep;
                //    //    Brep toRemoveBrep = (Brep)myDoc.Objects.Find(extendedSocketBoxID).Geometry;
                //    //    // Test if the added electrical component can be added and hiden in this position
                //    //    Brep dupCurrentSelectedBrep = currentSelectedBrep.DuplicateBrep();
                //    //    Brep dupToRemoveSocketBrep = toRemoveBrep.DuplicateBrep();
                //    //    var leftObj = Brep.CreateBooleanDifference(dupToRemoveSocketBrep, dupCurrentSelectedBrep, myDoc.ModelAbsoluteTolerance);
                //    //    myDoc.Objects.Delete(extendedSocketBoxID, true);

                //    //    //if (leftObj.Count() != 1)
                //    //    //{
                //    //    //    // revert the position of the imported part model
                //    //    //    Guid retractFinalPartPosID = myDoc.Objects.Transform(finalPartPosID, retractPartRotation, true);
                //    //    //    recoverPartPosID = myDoc.Objects.Transform(retractFinalPartPosID, retractPartTraslation, true);
                //    //    //    myDoc.Objects.Delete(finalSocketID, true);
                //    //    //    myDoc.Views.Redraw();
                //    //    //    continue;
                //    //    //}
                //    //    //else
                //    //    //{

                //    //        // confirm that the part is added to the model
                //    //        partSocket temp = new partSocket();
                //    //        temp.PrtName = selPrt.PartName;
                //    //        temp.SocketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;

                //    //        bool isExistSocket = false;
                //    //        int socketIdx = -1;
                //    //        foreach(partSocket prtS in currBody.partSockets)
                //    //        {
                //    //            if (prtS.PrtName.Equals(temp.PrtName))
                //    //            {
                //    //                isExistSocket = true;
                //    //                socketIdx = currBody.partSockets.IndexOf(prtS);
                //    //                break;
                //    //            }
                //    //        }

                //    //        if (!isExistSocket)
                //    //        {
                //    //            currBody.partSockets.Add(temp);
                //    //        }
                //    //        else
                //    //        {
                //    //            currBody.partSockets.ElementAt(socketIdx).SocketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;
                //    //        }


                //    //        // draw the normal for part rotation and flip manipulations
                //    //        //Line normalLine = new Line(new_part_pos, outwardDir * partHeight);
                //    //        //if (!normalRedLineID.Equals(Guid.Empty))
                //    //        //{
                //    //        //    myDoc.Objects.Delete(normalRedLineID, true);
                //    //        //}
                //    //        //normalRedLineID = myDoc.Objects.AddLine(normalLine, redAttribute);
                //    //        //myDoc.Views.Redraw();
                //    //        //currBody.deployedEmbedType.Add(embedType);

                //    //        // update the part's Guid and pin information since the part is added to the scene

                //    //        dataset.part3DList.ElementAt(oldIdx).PrtID = finalPartPosID;
                //    //        dataset.part3DList.ElementAt(oldIdx).IsDeployed = true;
                //    //        dataset.part3DList.ElementAt(oldIdx).PartPos = new_part_pos;

                //    //        // translation: partTranslation
                //    //        // rotation: partRotation
                //    //        foreach (Pin p in dataset.part3DList.ElementAt(oldIdx).Pins)
                //    //        {
                //    //            Point3d pin_pos = p.Pos;
                //    //            pin_pos.Transform(partTranslation);
                //    //            pin_pos.Transform(partRotation);

                //    //            int idx = dataset.part3DList.ElementAt(oldIdx).Pins.IndexOf(p);
                //    //            dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = pin_pos;
                //    //        }
                //    //        dataset.part3DList.ElementAt(oldIdx).Normal = outwardDir;

                //    //        dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Clear();
                //    //        dataset.part3DList.ElementAt(oldIdx).TransformSets.Clear();

                //    //        dataset.part3DList.ElementAt(oldIdx).TransformSets.Add(partTranslation);
                //    //        dataset.part3DList.ElementAt(oldIdx).TransformSets.Add(partRotation);

                //    //        dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Add(retractPartRotation);
                //    //        dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Add(retractPartTraslation);

                //    //        myDoc.Objects.Delete(finalSocketID, true);
                //    //        myDoc.Views.Redraw();
                //    //        break;
                //    //    //}

                //    //    #endregion
                //    //}

                //    //#endregion

                //    //#endregion

                //    #endregion

                //    #region Step 2: Show potential potision candidate when the user moves the mouse around the 3D model

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

                //    #region a better way to capture all position candidates -- using ray-tracing method to find all the points on the surface

                //    BoundingBox meshBox = brep.GetBoundingBox(true);
                //    double w = meshBox.Max.X - meshBox.Min.X;
                //    double l = meshBox.Max.Y - meshBox.Min.Y;
                //    double h = meshBox.Max.Z - meshBox.Min.Z;
                //    currBody.surfPts.Clear();
                //    double offset = 5; // offset the origin of the ray to include the points on the boundary of the bounding box

                //    // show the processing warning window
                //    prcWin.Show();

                //    for (int i = 0; i < w; i++)
                //    {
                //        for (int j = 0; j < l; j++)
                //        {
                //            //for (int t = 0; t < h; t++)
                //            //{
                //            //    Point3d ptInSpace = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, t + meshBox.Min.Z);
                //            //    if (brep.ClosestPoint(ptInSpace).DistanceTo(ptInSpace) <= 0.5)
                //            //    {
                //            //        currBody.surfPts.Add(brep.ClosestPoint(ptInSpace));
                //            //    }
                //            //}

                //            Point3d ptInSpaceOri = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, meshBox.Min.Z - offset);
                //            Point3d ptInSpaceEnd = new Point3d(i + meshBox.Min.X, j + meshBox.Min.Y, meshBox.Max.Z + offset);

                //            Line ray_ln = new Line(ptInSpaceOri, ptInSpaceEnd);
                //            Curve ray_crv = ray_ln.ToNurbsCurve();

                //            Curve[] overlapCrvs;
                //            Point3d[] overlapPts;

                //            Intersection.CurveBrep(ray_crv, currBody.bodyBrep, myDoc.ModelAbsoluteTolerance, out overlapCrvs, out overlapPts);

                //            if (overlapPts != null)
                //            {
                //                if (overlapPts.Count() != 0)
                //                {
                //                    foreach(Point3d p in overlapPts)
                //                    {
                //                        currBody.surfPts.Add(p);
                //                    }
                //                }
                //            }

                //        }
                //    }

                //    for (int i = 0; i < w; i++)
                //    {
                //        for (int t = 0; t < h; t++)
                //        {

                //            Point3d ptInSpaceOri = new Point3d(i + meshBox.Min.X, meshBox.Min.Y - offset, meshBox.Min.Z + t);
                //            Point3d ptInSpaceEnd = new Point3d(i + meshBox.Min.X, meshBox.Max.Y + offset, meshBox.Min.Z + t);

                //            Line ray_ln = new Line(ptInSpaceOri, ptInSpaceEnd);
                //            Curve ray_crv = ray_ln.ToNurbsCurve();

                //            Curve[] overlapCrvs;
                //            Point3d[] overlapPts;

                //            Intersection.CurveBrep(ray_crv, currBody.bodyBrep, myDoc.ModelAbsoluteTolerance, out overlapCrvs, out overlapPts);

                //            if (overlapPts != null)
                //            {
                //                if (overlapPts.Count() != 0)
                //                {
                //                    foreach (Point3d p in overlapPts)
                //                    {
                //                        if(currBody.surfPts.IndexOf(p) == -1)
                //                            currBody.surfPts.Add(p);
                //                    }
                //                }
                //            }

                //            //myDoc.Objects.AddCurve(ray_crv, redAttribute);

                //        }
                //    }

                //    for (int j = 0; j < l; j++)
                //    {
                //        for (int t = 0; t < h; t++)
                //        {

                //            Point3d ptInSpaceOri = new Point3d(meshBox.Min.X - offset, j + meshBox.Min.Y, meshBox.Min.Z + t);
                //            Point3d ptInSpaceEnd = new Point3d(meshBox.Max.X + offset, j + meshBox.Min.Y, meshBox.Min.Z + t);

                //            Line ray_ln = new Line(ptInSpaceOri, ptInSpaceEnd);
                //            Curve ray_crv = ray_ln.ToNurbsCurve();

                //            Curve[] overlapCrvs;
                //            Point3d[] overlapPts;

                //            Intersection.CurveBrep(ray_crv, currBody.bodyBrep, myDoc.ModelAbsoluteTolerance, out overlapCrvs, out overlapPts);

                //            if (overlapPts != null)
                //            {
                //                if (overlapPts.Count() != 0)
                //                {
                //                    foreach (Point3d p in overlapPts)
                //                    {
                //                        if (currBody.surfPts.IndexOf(p) == -1)
                //                            currBody.surfPts.Add(p);
                //                    }
                //                }
                //            }

                //            //myDoc.Objects.AddCurve(ray_crv, redAttribute);

                //        }
                //    }

                //    #endregion

                //    #region show all the candidate positions

                //    Brep solidDupBrep = currBody.bodyBrep.DuplicateBrep();
                //    Guid dupObjID = myDoc.Objects.AddBrep(solidDupBrep, solidAttribute);
                //    myDoc.Objects.Hide(currBody.objID, true);

                //    List<Guid> pts_normals = new List<Guid>();
                //    foreach (Point3d pt in currBody.surfPts)
                //    {
                //        Guid pID = myDoc.Objects.AddPoint(pt);
                //        pts_normals.Add(pID);
                //    }

                //    myDoc.Views.Redraw();

                //    prcWin.Hide();

                //    #endregion

                //    while (true)
                //    {
                //        #region ask the user to select one position

                //        Rhino.Input.Custom.GetPoint ec_pos = new Rhino.Input.Custom.GetPoint();
                //        ec_pos.SetCommandPrompt("selsect a place and drop the electrical part");
                //        ec_pos.MouseMove += Ec_pos_MouseMove;
                //        ec_pos.DynamicDraw += Ec_pos_DynamicDraw;
                //        ec_pos.Get(true);

                //        foreach (Guid pnID in pts_normals)
                //        {
                //            myDoc.Objects.Delete(pnID, true);
                //        }

                //        // re-show the original body and quit position selection mode
                //        myDoc.Objects.Delete(dupObjID, true);
                //        myDoc.Objects.Show(currBody.objID, true);
                //        myDoc.Views.Redraw();

                //        if (ec_pos.Point().X == Int32.MaxValue) return;

                //        #endregion

                //        #region Step 1: find the closest point on the selected object's surface

                //        Point3d ptOnSurf = FindCloesetPointFromSurface(ec_pos.Point(), currBody.objID);

                //        #endregion

                //        #region Step 2: get the normal direction at this point on the surface

                //        ObjRef currObjRef = new ObjRef(currBody.objID);
                //        Vector3d outwardDir = new Vector3d();

                //        Point3d closestPt = new Point3d();
                //        double u, v;
                //        ComponentIndex ii;
                //        currBody.bodyBrep.ClosestPoint(ptOnSurf, out closestPt, out ii, out u, out v, 2, out outwardDir);

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

                //        #region Step 3: move the imported part to the final location (initially, the part is entirely hidden in the model)

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
                //        //Transform differenceMove = Transform.Translation(outwardDir * (partHeight / 2 - 0.1));
                //        //Guid extendedSocketBoxID = myDoc.Objects.Transform(finalSocketID, differenceMove, false);

                //        Brep currentSelectedBrep = currBody.bodyBrep;

                //        // Test if the added electrical component can be added and hiden in this position
                //        //Brep dupCurrentSelectedBrep = currentSelectedBrep.DuplicateBrep();
                //        //Brep dupToRemoveSocketBrep = toRemoveBrep.DuplicateBrep();
                //        //var leftObj = Brep.CreateBooleanDifference(dupToRemoveSocketBrep, dupCurrentSelectedBrep, myDoc.ModelAbsoluteTolerance);
                //        //myDoc.Objects.Delete(extendedSocketBoxID, true);


                //        #region test if the added part collides with other parts

                //        bool isPosValid = true;

                //        Brep socketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;

                //        if (currBody.partSockets.Count > 0)
                //        {
                //            List<Brep> exitSockets = new List<Brep>();
                //            foreach(partSocket prtSocket in currBody.partSockets)
                //            {
                //                exitSockets.Add(prtSocket.SocketBrep);
                //            }
                //            var soketOverlaps = Brep.CreateBooleanIntersection(new List<Brep> { socketBrep }, exitSockets, myDoc.ModelAbsoluteTolerance);

                //            if(soketOverlaps == null)
                //            {
                //                isPosValid = true;
                //            }
                //            else
                //            {
                //                if(soketOverlaps.Count() > 0)
                //                {
                //                    isPosValid = false;
                //                }
                //                else
                //                {
                //                    isPosValid = true;
                //                }
                //            }
                //        }

                //        #endregion

                //        if (isPosValid)
                //        {
                //            // confirm that the part is added to the model
                //            partSocket temp = new partSocket();
                //            temp.PrtName = selPrt.PartName;
                //            temp.SocketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;

                //            bool isExistSocket = false;
                //            int socketIdx = -1;
                //            foreach (partSocket prtS in currBody.partSockets)
                //            {
                //                if (prtS.PrtName.Equals(temp.PrtName))
                //                {
                //                    isExistSocket = true;
                //                    socketIdx = currBody.partSockets.IndexOf(prtS);
                //                    break;
                //                }
                //            }

                //            if (!isExistSocket)
                //            {
                //                currBody.partSockets.Add(temp);
                //            }
                //            else
                //            {
                //                currBody.partSockets.ElementAt(socketIdx).SocketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;
                //            }

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

                //            dataset.part3DList.ElementAt(oldIdx).Height = partHeight;

                //            myDoc.Objects.Delete(finalSocketID, true);
                //            myDoc.Views.Redraw();
                //            break;
                //        }

                //        #endregion
                //    }
                //    //}

                //    //#endregion

                //    //#endregion

                //    #endregion
                //}
                //else
                //{

                #endregion

                #region Step 1: transform the imported part 3D model back to original position 

                Point3d obj_pos = currBody.bodyBrep.GetBoundingBox(true).Center;
                Point3d new_ori_pos = new Point3d(obj_pos.X, obj_pos.Y, obj_pos.Z + 40);

                int prt_idx = -1;
                foreach(Part3D prt3D in dataset.part3DList)
                {
                    if (prt3D.PartName.Equals(selPrt.PartName))
                    {
                        prt_idx = dataset.part3DList.IndexOf(prt3D);
                        break;
                    }
                }

                int socketIdx = -1;
                foreach (partSocket prtS in currBody.partSockets)
                {
                    if (prtS.PrtName.Equals(selPrt.PartName))
                    {
                        socketIdx = currBody.partSockets.IndexOf(prtS);
                    }
                }


                BoundingBox partBoundingBox = new BoundingBox();
                Guid recoverPartPosID = Guid.Empty;
                Guid tempPartPosID1 = Guid.Empty;
                Guid tempPartPosID1_5 = Guid.Empty;
                Guid tempPartPosID2 = Guid.Empty;
                Guid importedObjID = Guid.Empty;

                importedObjID = dataset.part3DList.ElementAt(prt_idx).PrtID;

                #region First, reverse the exposure

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
                Guid newIDTemp = myDoc.Objects.Transform(importedObjID, resetTrans, true);
                dataset.part3DList.ElementAt(prt_idx).PrtID = newIDTemp;

                old_prt_pos.Transform(resetTrans);

                #region update the pins

                foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                {
                    Point3d temp_pt = p.Pos;

                    temp_pt.Transform(resetTrans);

                    int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                    dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = temp_pt;
                }

                #endregion

                #region update the socket

                prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                prtBrep.Transform(resetTrans);
                currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                #endregion

                #endregion

                #region Second, reverse flip if possible

                Transform flipTrans = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).Normal, reverseNormalVector, old_prt_pos);
                if (dataset.part3DList.ElementAt(prt_idx).IsFlipped)
                {
                    Guid newID = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, flipTrans, true);
                    dataset.part3DList.ElementAt(prt_idx).PrtID = newID;
                    old_prt_pos.Transform(flipTrans);

                    #region update the pins

                    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                    {
                        Point3d temp_pt = p.Pos;

                        temp_pt.Transform(flipTrans);

                        int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                    dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = temp_pt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(flipTrans);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion
                }

            #endregion

                #region Third, transform back to the standard position and orientation

                foreach (Transform t in dataset.part3DList.ElementAt(prt_idx).TransformReverseSets)
                {
                    Guid transID = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, t, true);
                    dataset.part3DList.ElementAt(prt_idx).PrtID = transID;
                    old_prt_pos.Transform(t);

                    #region update the pins

                    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                    {
                        Point3d temp_pt = p.Pos;

                        temp_pt.Transform(t);

                        int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = temp_pt;
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
                //    // set the part position using the current center in case the user has manually moved the part
                //    Transform manualMove = Transform.Translation(myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center - dataset.part3DList.ElementAt(prt_idx).PartPos);
                //    dataset.part3DList.ElementAt(prt_idx).PartPos = myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center;
                //    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                //    {
                //        Point3d p_pos = p.Pos;
                //        p_pos.Transform(manualMove);
                //        int i = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                //        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(i).Pos = p_pos;
                //    }

                //    // First, rotate or flip the part back if there is a rotation or flip for the part on the surface
                //    Transform roback = Transform.Rotation((-1) * dataset.part3DList.ElementAt(prt_idx).RotationAngle / 180.0 * Math.PI,
                //                                            dataset.part3DList.ElementAt(prt_idx).Normal, dataset.part3DList.ElementAt(prt_idx).PartPos);
                //    tempPartPosID1 = myDoc.Objects.Transform(importedObjID, roback, true);

                //    dataset.part3DList.ElementAt(prt_idx).PartPos.Transform(roback);
                //    foreach(Pin pin_info in dataset.part3DList.ElementAt(prt_idx).Pins)
                //    {
                //        Point3d p_pos = pin_info.Pos;
                //        p_pos.Transform(roback);
                //        int p_idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(pin_info);
                //        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(p_idx).Pos = p_pos;
                //    }

                //    if (dataset.part3DList.ElementAt(prt_idx).IsFlipped)
                //    {
                //        Vector3d reverseNormalVector = dataset.part3DList.ElementAt(prt_idx).Normal * (-1);
                //        Transform flipTrans = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).Normal, reverseNormalVector, dataset.part3DList.ElementAt(prt_idx).PartPos);

                //        tempPartPosID1_5 = myDoc.Objects.Transform(tempPartPosID1, flipTrans, true);

                //        dataset.part3DList.ElementAt(prt_idx).PartPos.Transform(flipTrans);
                //        foreach (Pin pin_info in dataset.part3DList.ElementAt(prt_idx).Pins)
                //        {
                //            Point3d p_pos = pin_info.Pos;
                //            p_pos.Transform(flipTrans);
                //            int p_idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(pin_info);
                //            dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(p_idx).Pos = p_pos;
                //        }
                //    }
                //    else
                //    {
                //        tempPartPosID1_5 = tempPartPosID1;
                //    }

                //    // Second, transform back to the original imported position
                //    // don't use Part3D's TransformReverseSets
                //    Transform rotationBack = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).Normal, new Vector3d(0, 0, 1), dataset.part3DList.ElementAt(prt_idx).PartPos);
                //    tempPartPosID2 = myDoc.Objects.Transform(tempPartPosID1_5, rotationBack, true);

                //    Transform translateBack = Transform.Translation(new_ori_pos - dataset.part3DList.ElementAt(prt_idx).PartPos);
                //    recoverPartPosID = myDoc.Objects.Transform(tempPartPosID2, translateBack, true);

                //    partBoundingBox = myDoc.Objects.Find(recoverPartPosID).Geometry.GetBoundingBox(true);

                //    Point3d tempPt = dataset.part3DList.ElementAt(prt_idx).PartPos;
                //    tempPt.Transform(rotationBack);
                //    tempPt.Transform(translateBack);
                //    dataset.part3DList.ElementAt(prt_idx).PartPos = tempPt;

                //    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                //    {
                //        Point3d tempPinPt = p.Pos;
                //        tempPinPt.Transform(rotationBack);
                //        tempPinPt.Transform(translateBack);
                //        int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                //        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = tempPinPt;

                //        //myDoc.Objects.AddPoint(dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos, redAttribute);
                //        //myDoc.Views.Redraw();
                //    }

                //// myDoc.Views.Redraw();

                #endregion

                #endregion

                myDoc.Views.Redraw();

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

                // show the processing warning window
                prcWin.Show();

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

                #region show all the candidate positions

                Brep solidDupBrep = currBody.bodyBrep.DuplicateBrep();
                Guid dupObjID = myDoc.Objects.AddBrep(solidDupBrep, solidAttribute);
                myDoc.Objects.Hide(currBody.objID, true);

                List<Guid> pts_normals = new List<Guid>();
                foreach (Point3d pt in currBody.surfPts)
                {
                    Guid pID = myDoc.Objects.AddPoint(pt);
                    pts_normals.Add(pID);
                } 

                myDoc.Views.Redraw();

                prcWin.Hide();

                #endregion

                #region select the points on the surface and drop the part on that spot

                while (true)
                {
                    #region Step 0: ask the user to select one position

                    Rhino.Input.Custom.GetPoint ec_pos = new Rhino.Input.Custom.GetPoint();
                    ec_pos.SetCommandPrompt("selsect a place and drop the electrical part");
                    ec_pos.MouseMove += Ec_pos_MouseMove;
                    ec_pos.DynamicDraw += Ec_pos_DynamicDraw;
                    ec_pos.Get(true);

                    foreach (Guid pnID in pts_normals)
                    {
                        myDoc.Objects.Delete(pnID, true);
                    }

                    // re-show the original body and quit position selection mode
                    myDoc.Objects.Delete(dupObjID, true);
                    myDoc.Objects.Show(currBody.objID, true);
                    myDoc.Views.Redraw();

                    if (ec_pos.Point().X == Int32.MaxValue) return;

                    #endregion

                    #region Step 1: find the closest point on the selected object's surface

                    Point3d ptOnSurf = FindCloesetPointFromSurface(ec_pos.Point(), currBody.bodyBrep);

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

                    
                    #region Fourth, transform the part to the new place

                    double hideratio = 0.5;
                    var embedTranslation = Transform.Translation(outwardDir * (-1) * prt_height * hideratio);
                    Point3d new_part_pos = ptOnSurf;
                    new_part_pos.Transform(embedTranslation);

                    //Point3d new_ori_pos = myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center;
                    new_ori_pos = old_prt_pos;
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

                    #endregion

                    #region Fifth, flip back if possible

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
                            Point3d temp_pt = p.Pos;

                            temp_pt.Transform(flipTrans1);

                            int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                            dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = temp_pt;
                        }

                        #endregion

                        #region update the socket

                        prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                        prtBrep.Transform(flipTrans1);
                        currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                        #endregion
                    }

                    #endregion

                    #region Sixth, set the exposure level

                    Transform setExpTrans = Transform.Translation(dataset.part3DList.ElementAt(prt_idx).Normal * prt_exp * prt_height);
                    Guid newID1 = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, setExpTrans, true);
                    dataset.part3DList.ElementAt(prt_idx).PrtID = newID1;
                    old_prt_pos.Transform(setExpTrans);

                    #region update the pins

                    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                    {
                        Point3d temp_pt = p.Pos;

                        temp_pt.Transform(setExpTrans);

                        int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = temp_pt;
                    }

                    #endregion

                    #region update the socket

                    prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(setExpTrans);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    #endregion

                    #endregion

                    #region Seventh, set self-rotation

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

                    #endregion

                    #region Finally, update the part information

                    dataset.part3DList.ElementAt(prt_idx).PartPos = old_prt_pos;

                    dataset.part3DList.ElementAt(prt_idx).TransformReverseSets.Clear();
                    dataset.part3DList.ElementAt(prt_idx).TransformSets.Clear();

                    dataset.part3DList.ElementAt(prt_idx).TransformSets.Add(partTranslation);
                    dataset.part3DList.ElementAt(prt_idx).TransformSets.Add(partRotation);
                    dataset.part3DList.ElementAt(prt_idx).TransformReverseSets.Add(retractPartRotation);
                    dataset.part3DList.ElementAt(prt_idx).TransformReverseSets.Add(retractPartTraslation);

                    #endregion

                    myDoc.Views.Redraw();
                    break;

                    #region old verstion: check if the position is valid by checking if sockets are overlapping

                    //#region Step 5: rotate the part to the normal direction

                    //Vector3d startDirVector = new Vector3d(0, 0, 1);
                    //Vector3d endDirVector = outwardDir;
                    //var partRotation = Transform.Rotation(startDirVector, endDirVector, new_part_pos);
                    //var retractPartRotation = Transform.Rotation(endDirVector, startDirVector, new_part_pos);

                    //// no intersections so that the part can be added to this location inside the 3D model
                    //Guid socketBoxID = myDoc.Objects.AddBrep(truncatedSocket, orangeAttribute);
                    //// get the final socket box after rotation
                    //Guid finalSocketID = myDoc.Objects.Transform(socketBoxID, partRotation, true);
                    //// situate the part in the final position and orientation
                    //Guid finalPartPosID = myDoc.Objects.Transform(transit_ID, partRotation, true);

                    //// Extend the box a bit more outside the selected solid
                    ////Transform differenceMove = Transform.Translation(outwardDir * (partHeight / 2 - 0.1));
                    ////Guid extendedSocketBoxID = myDoc.Objects.Transform(finalSocketID, differenceMove, false);

                    //Brep currentSelectedBrep = currBody.bodyBrep;

                    //// Test if the added electrical component can be added and hiden in this position
                    ////Brep dupCurrentSelectedBrep = currentSelectedBrep.DuplicateBrep();
                    ////Brep dupToRemoveSocketBrep = toRemoveBrep.DuplicateBrep();
                    ////var leftObj = Brep.CreateBooleanDifference(dupToRemoveSocketBrep, dupCurrentSelectedBrep, myDoc.ModelAbsoluteTolerance);
                    ////myDoc.Objects.Delete(extendedSocketBoxID, true);


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

                    //if (isPosValid)
                    //{
                    //    // confirm that the part is added to the model
                    //    partSocket temp = new partSocket();
                    //    temp.PrtName = selPrt.PartName;
                    //    temp.SocketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;

                    //    bool isExistSocket = false;
                    //    int socketIdx = -1;
                    //    foreach (partSocket prtS in currBody.partSockets)
                    //    {
                    //        if (prtS.PrtName.Equals(temp.PrtName))
                    //        {
                    //            isExistSocket = true;
                    //            socketIdx = currBody.partSockets.IndexOf(prtS);
                    //            break;
                    //        }
                    //    }

                    //    if (!isExistSocket)
                    //    {
                    //        currBody.partSockets.Add(temp);
                    //    }
                    //    else
                    //    {
                    //        currBody.partSockets.ElementAt(socketIdx).SocketBrep = (Brep)myDoc.Objects.Find(finalSocketID).Geometry;
                    //    }

                    //    // update the part's Guid and pin information since the part is added to the scene
                    //    int oldIdx = -1;
                    //    foreach (Part3D prt3D in dataset.part3DList)
                    //    {
                    //        if (prt3D.PartName.Equals(selPrt.PartName))
                    //        {
                    //            oldIdx = dataset.part3DList.IndexOf(prt3D);
                    //            break;
                    //        }
                    //    }

                    //    dataset.part3DList.ElementAt(oldIdx).PrtID = finalPartPosID;
                    //    dataset.part3DList.ElementAt(oldIdx).IsDeployed = true;
                    //    dataset.part3DList.ElementAt(oldIdx).PartPos = new_part_pos;

                    //    // translation: partTranslation
                    //    // rotation: partRotation
                    //    foreach (Pin p in dataset.part3DList.ElementAt(oldIdx).Pins)
                    //    {
                    //        Point3d pin_pos = p.Pos;
                    //        pin_pos.Transform(partTranslation);
                    //        pin_pos.Transform(partRotation);

                    //        int idx = dataset.part3DList.ElementAt(oldIdx).Pins.IndexOf(p);
                    //        dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = pin_pos;
                    //    }
                    //    dataset.part3DList.ElementAt(oldIdx).Normal = outwardDir;

                    //    dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Clear();
                    //    dataset.part3DList.ElementAt(oldIdx).TransformSets.Clear();

                    //    dataset.part3DList.ElementAt(oldIdx).TransformSets.Add(partTranslation);
                    //    dataset.part3DList.ElementAt(oldIdx).TransformSets.Add(partRotation);

                    //    dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Add(retractPartRotation);
                    //    dataset.part3DList.ElementAt(oldIdx).TransformReverseSets.Add(retractPartTraslation);

                    //    dataset.part3DList.ElementAt(oldIdx).Height = partHeight;

                    //    #region rotate and flip back the part, pins, and its socket

                    //    Guid newPartID = dataset.part3DList.ElementAt(oldIdx).PrtID;
                    //    Guid newPartTemp1ID = Guid.Empty;
                    //    Guid newPartTemp2ID = Guid.Empty;

                    //    Transform rotateAgain = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).RotationAngle / 180.0 * Math.PI,
                    //                                            dataset.part3DList.ElementAt(prt_idx).Normal, dataset.part3DList.ElementAt(prt_idx).PartPos);
                    //    newPartTemp1ID = myDoc.Objects.Transform(newPartID, rotateAgain, true);

                    //    foreach (Pin p in dataset.part3DList.ElementAt(oldIdx).Pins)
                    //    {
                    //        Point3d p_pos = p.Pos;
                    //        p_pos.Transform(rotateAgain);
                    //        int idx = dataset.part3DList.ElementAt(oldIdx).Pins.IndexOf(p);
                    //        dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = p_pos;
                    //    }

                    //    foreach (partSocket prtS in currBody.partSockets)
                    //    {
                    //        if (prtS.PrtName.Equals(temp.PrtName))
                    //        {
                    //            socketIdx = currBody.partSockets.IndexOf(prtS);
                    //            break;
                    //        }
                    //    }

                    //    Brep tempRotBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    //    tempRotBrep.Transform(rotateAgain);
                    //    currBody.partSockets.ElementAt(socketIdx).SocketBrep = tempRotBrep;

                    //    if (dataset.part3DList.ElementAt(prt_idx).IsFlipped)
                    //    {
                    //        Vector3d reverseNormalVector = dataset.part3DList.ElementAt(prt_idx).Normal * (-1);
                    //        Transform flipTrans = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).Normal, reverseNormalVector, dataset.part3DList.ElementAt(prt_idx).PartPos);

                    //        newPartTemp2ID = myDoc.Objects.Transform(newPartTemp1ID, flipTrans, true);

                    //        Brep tempRotBrep1 = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    //        tempRotBrep1.Transform(flipTrans);
                    //        currBody.partSockets.ElementAt(socketIdx).SocketBrep = tempRotBrep1;

                    //        foreach (Pin p in dataset.part3DList.ElementAt(oldIdx).Pins)
                    //        {
                    //            Point3d p_pos = p.Pos;
                    //            p_pos.Transform(flipTrans);
                    //            int idx = dataset.part3DList.ElementAt(oldIdx).Pins.IndexOf(p);
                    //            dataset.part3DList.ElementAt(oldIdx).Pins.ElementAt(idx).Pos = p_pos;
                    //        }
                    //        dataset.part3DList.ElementAt(oldIdx).PartPos.Transform(flipTrans);
                    //    }
                    //    else
                    //    {
                    //        newPartTemp2ID = newPartTemp1ID;
                    //    }
                    //    dataset.part3DList.ElementAt(oldIdx).PrtID = newPartTemp2ID;
                    //    #endregion

                    //    #region test the part and pin positions

                    //    //foreach (Pin p in dataset.part3DList.ElementAt(oldIdx).Pins)
                    //    //{
                    //    //    int idx = dataset.part3DList.ElementAt(oldIdx).Pins.IndexOf(p);

                    //    //    myDoc.Objects.AddPoint(dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos, redAttribute);
                    //    //    myDoc.Views.Redraw();
                    //    //}
                    //    //myDoc.Objects.AddPoint(dataset.part3DList.ElementAt(prt_idx).PartPos, redAttribute);
                    //    //myDoc.Views.Redraw();

                    //    #endregion

                    //    myDoc.Objects.Delete(finalSocketID, true);
                    //    myDoc.Views.Redraw();
                    //    break;
                    //}

                    //#endregion

                    #endregion
                }

                #endregion

                #endregion
                //}

            }
        }

        private void Ec_pos_DynamicDraw(object sender, Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            if (!(selPrtPosPt.X == Int32.MaxValue && selPrtPosPt.Y == Int32.MaxValue && selPrtPosPt.Z == Int32.MaxValue))
            {
                e.Display.DrawSphere(new Sphere(selPrtPosPt, currBody.rtThickness), Color.FromArgb(48,163,229));

                Guid pt_normal = Guid.Empty;

                Vector3d outwardDir = new Vector3d();

                Point3d closestPt = new Point3d();
                double u, v;
                ComponentIndex i;
                currBody.bodyBrep.ClosestPoint(selPrtPosPt, out closestPt, out i, out u, out v, 2, out outwardDir);

                List<Brep> projectedBrep = new List<Brep>();
                projectedBrep.Add(currBody.bodyBrep);
                List<Point3d> projectPt = new List<Point3d>();
                projectPt.Add(closestPt);

                var intersectionPts = Intersection.ProjectPointsToBreps(projectedBrep, projectPt, outwardDir, myDoc.ModelAbsoluteTolerance);

                if (intersectionPts != null)
                {
                    if (intersectionPts.Length == 1)
                    {
                        outwardDir = (-1) * outwardDir;
                    }

                    Line normal = new Line(selPrtPosPt, selPrtPosPt + outwardDir);

                    e.Display.DrawLine(normal, Color.FromArgb(204, 50, 68));
                }
            }
        }

        private void Ec_pos_MouseMove(object sender, Rhino.Input.Custom.GetPointMouseEventArgs e)
        {
            selPrtPosPt = FindCloesetPointFromSurfaceWithConstraint(e.Point, currBody.objID, 20);
        }
        /// <summary>
        /// Find the closest point from a surface with a given distance
        /// </summary>
        /// <param name="target">the target point</param>
        /// <param name="objID">the model that has the target surface</param>
        /// <param name="dis">the distance between the pointer and the surface</param>
        /// <returns>The closet point on the surface</returns>
        Point3d FindCloesetPointFromSurfaceWithConstraint(Point3d target, Guid objID, double dis)
        {
            Point3d result = new Point3d(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue); // dumb point

            ObjRef dupObjRef = new ObjRef(objID);

            if (dupObjRef.Brep().ClosestPoint(target).DistanceTo(target) <= dis)
            {
                result = dupObjRef.Brep().ClosestPoint(target);
            }

            return result;
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

        Point3d FindCloesetPointFromSurface(Point3d target, Brep objBrep)
        {
            Point3d result = new Point3d(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue); // dumb point
            result = objBrep.ClosestPoint(target);

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
            get { return new Guid("bd7a1096-9140-412e-9987-f7ca502e3d15"); }
        }
    }
}