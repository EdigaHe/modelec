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
    public class Add3DPPartComponent : GH_Component
    {
        SharedData dataset;
        bool testBtnClick;
        RhinoDoc myDoc;
        TargetBody currBody;
        PrinterProfile printerProfile;
        ProcessingWindow prcWin;
        ObjectAttributes solidAttribute, orangeAttribute, redAttribute, blueAttribute;
        Point3d selPrtPosPt;

        /// <summary>
        /// Initializes a new instance of the Add3DPPartComponent class.
        /// </summary>
        public Add3DPPartComponent()
          : base("3DPrintedPartAdd", "PPA",
              "Add a 3D-printed part into the model",
              "Modelec", "3DP-Control")
        {
            dataset = SharedData.Instance;
            testBtnClick = false;
            currBody = TargetBody.Instance;
            myDoc = RhinoDoc.ActiveDoc;
            printerProfile = PrinterProfile.Instance;
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
            pManager.AddBooleanParameter("AddBtnClicked", "BC", "click the button to add the selected 3D-printed part", GH_ParamAccess.item);
            pManager.AddTextParameter("Selected3DPrintedPart", "SP", "Select one 3D-printed part from the list", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("3DPrintedPartList", "3DPList", "List of added 3D-printed parts", GH_ParamAccess.list);
            pManager.AddBooleanParameter("ListUpdated", "ListUp", "the list is updated", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string read3DPPartName = "";
            bool btnClick = false;
            bool isOn = false;
            List<string> output = new List<string>();

            #region read the button click and decide what 3D-printed part is selected

            if (!DA.GetData(0, ref btnClick))
                return;

            if (!DA.GetData(1, ref read3DPPartName))
                return;

            //if (!DA.GetData(1, ref dataset))
            //    return;

            if (!btnClick && testBtnClick)
            {
                isOn = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }

            #endregion

            if (isOn)
            {
               
                if (myDoc.Objects.Find(currBody.objID) == null)
                {
                    // the model body has not been selected or the model body has been transformed 

                    ObjRef objSel_ref1;
                    Guid selObjId = Guid.Empty;
                    var rc = RhinoGet.GetOneObject("Select a model (brep)", false, ObjectType.AnyObject, out objSel_ref1);
                    if (rc == Rhino.Commands.Result.Success)
                    {
                        selObjId = objSel_ref1.ObjectId;
                        currBody.objID = selObjId;
                        ObjRef currObj = new ObjRef(selObjId);
                        currBody.bodyBrep = currObj.Brep();
                    }
                }
                

                #region Step 1: create the 3D-printed part's name

                string temp_name = "";
                int temp_id = 0;

                switch (read3DPPartName)
                {
                    case "Connection Node": {
                            temp_id = 0;
                            foreach(Part3DP p in dataset.part3DPList)
                            {
                                if (p.PartName.Substring(0, 1).Equals("N"))
                                {
                                    temp_id++;
                                }
                            }

                            temp_id++;
                            temp_name = "Node" + temp_id.ToString();
                            dataset.currPart3DP.Type = 1;

                        } break;
                    case "Capacitive Touch Area":
                        {
                            temp_id = 0;
                            foreach (Part3DP p in dataset.part3DPList)
                            {
                                if (p.PartName.Substring(0, 1).Equals("A"))
                                {
                                    temp_id++;
                                }
                            }

                            temp_id++;
                            temp_name = "Area" + temp_id.ToString();
                            dataset.currPart3DP.Type = 2;
                        }
                        break;
                    case "Resistor":
                        {
                            temp_id = 0;
                            foreach (Part3DP p in dataset.part3DPList)
                            {
                                if (p.PartName.Substring(0, 1).Equals("R"))
                                {
                                    temp_id++;
                                }
                            }

                            temp_id++;
                            temp_name = "R" + temp_id.ToString();
                            dataset.currPart3DP.Type = 3;
                        }
                        break;
                    default: break;
                }

                dataset.currPart3DP.PartName = temp_name;

                #endregion

                #region Step 2: add the part to the Rhino scene

                #region create a new layer if not exist to store all 3D-printed parts

                if (myDoc.Layers.FindName("3DParts") == null)
                {
                    // create a new layer named "Parts"
                    string layer_name = "3DParts";
                    Layer p_ly = myDoc.Layers.CurrentLayer;

                    Layer new_ly = new Layer();
                    new_ly.Name = layer_name;
                    new_ly.ParentLayerId = p_ly.ParentLayerId;
                    new_ly.Color = Color.FromArgb(61,61,61);

                    int index = myDoc.Layers.Add(new_ly);

                    myDoc.Layers.SetCurrentLayerIndex(index, true);

                    if (index < 0) return;
                }
                else
                {
                    int index = myDoc.Layers.FindName("3DParts").Index;
                    myDoc.Layers.SetCurrentLayerIndex(index, true);
                }

                #endregion

                switch (dataset.currPart3DP.Type)
                {
                    case 1:
                        {
                            // add the dot (sphere) to the Rhino scene
                            // initially the sphere is at the center of the object
                            Point3d cen = currBody.bodyBrep.GetBoundingBox(true).Center;
                            double iniRadius = 2;
                            Guid dotID = myDoc.Objects.AddSphere(new Sphere(cen, iniRadius));
                            myDoc.Views.Redraw();

                            dataset.currPart3DP.Depth = iniRadius;
                            dataset.currPart3DP.Height = iniRadius;
                            dataset.currPart3DP.Length = iniRadius;
                            dataset.currPart3DP.PrtID = dotID;
                            dataset.currPart3DP.PartPos = cen;
                            dataset.currPart3DP.IsDeployed = true;
                            dataset.currPart3DP.Pins.Add(new Pin("pin", 1, cen.X, cen.Y, cen.Z));

                        }
                        break;
                    case 2:
                        {
                            // add the area to the Rhino scene
                            double iniRadius = 1;

                            #region old version (commented)
                            //Point3d cenInitial = new Point3d(currBody.bodyBrep.GetBoundingBox(true).Center.X, currBody.bodyBrep.GetBoundingBox(true).Center.Y,
                            //                        currBody.bodyBrep.GetBoundingBox(true).Max.Z);
                            //Point3d cen = currBody.bodyBrep.ClosestPoint(cenInitial);


                            //Sphere rangeSphere = new Sphere(cen, iniRadius);
                            //Brep rangBrep = rangeSphere.ToBrep();

                            //Brep dupBody = currBody.bodyBrep.DuplicateBrep();

                            //Brep[] outBlends;
                            //Brep[] outWalls;
                            //Brep[] dupOffsetBody = Brep.CreateOffsetBrep(dupBody, -1, false, true, myDoc.ModelRelativeTolerance, out outBlends, out outWalls);

                            //Brep innerShell = dupOffsetBody[0];

                            //Brep interBrep = Brep.CreateBooleanIntersection(dupBody, rangBrep, myDoc.ModelAbsoluteTolerance)[0];
                            //Brep areaBrep = Brep.CreateBooleanDifference(interBrep, innerShell, myDoc.ModelAbsoluteTolerance)[0];

                            //Guid areaID = myDoc.Objects.AddBrep(areaBrep);

                            //myDoc.Views.Redraw();

                            #endregion

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


                                #region Fourth, create a sphere

                                //Point3d cen = ptOnSurf;
                                //Sphere rangeSphere = new Sphere(cen, iniRadius);
                                //Brep rangBrep = rangeSphere.ToBrep();

                                ////Brep dupBody = currBody.bodyBrep.DuplicateBrep();

                                ////Brep[] outBlends;
                                ////Brep[] outWalls;
                                ////Brep[] dupOffsetBody = Brep.CreateOffsetBrep(dupBody, -1, false, true, myDoc.ModelRelativeTolerance, out outBlends, out outWalls);

                                ////Brep innerShell = dupOffsetBody[0];

                                ////Brep interBrep = Brep.CreateBooleanIntersection(dupBody, rangBrep, myDoc.ModelAbsoluteTolerance)[0];
                                ////Brep areaBrep = Brep.CreateBooleanDifference(interBrep, innerShell, myDoc.ModelAbsoluteTolerance)[0];

                                //Guid areaID = myDoc.Objects.AddBrep(rangBrep);

                                #endregion

                                Point3d cen = ptOnSurf;
                                Sphere rangeSphere = new Sphere(cen, iniRadius *1.5);
                                Brep rangBrep = rangeSphere.ToBrep();

                                Brep dupBody = currBody.bodyBrep.DuplicateBrep();

                                Brep[] outBlends;
                                Brep[] outWalls;
                                Brep[] dupOffsetBody = Brep.CreateOffsetBrep(dupBody, -1, false, true, myDoc.ModelRelativeTolerance, out outBlends, out outWalls);

                                Brep innerShell = dupOffsetBody[0];

                                Brep interBrep = Brep.CreateBooleanIntersection(dupBody, rangBrep, myDoc.ModelAbsoluteTolerance)[0];
                                Brep areaBrep = Brep.CreateBooleanDifference(interBrep, innerShell, myDoc.ModelAbsoluteTolerance)[0];

                                Guid areaID = myDoc.Objects.AddBrep(areaBrep);

                                myDoc.Views.Redraw();


                                dataset.currPart3DP.Depth = iniRadius;
                                dataset.currPart3DP.Height = iniRadius;
                                dataset.currPart3DP.Length = iniRadius;
                                dataset.currPart3DP.PrtID = areaID;
                                dataset.currPart3DP.PartPos = cen;
                                dataset.currPart3DP.IsDeployed = true;
                                dataset.currPart3DP.Pins.Add(new Pin("pin", 1, cen.X, cen.Y, cen.Z));

                                myDoc.Objects.Delete(dupObjID, true);
                                myDoc.Views.Redraw();
                                break;
                            }

                            #endregion

                            

                        }
                        break;
                    case 3:
                        {
                            // add the resistor to the Rhino scene

                            Point3d cen = currBody.bodyBrep.GetBoundingBox(true).Center;

                            double length = -1;
                            double height = -1, depth = -1;   // height and depth are always equal for resistors

                            if (printerProfile.Trace_resistivity > 0)
                            {
                                length = 8;
                                height = 2;
                                depth = 2;
                            }
                            else
                            {
                                length = 2;
                                height = 2;
                                depth = 2;
                            }

                            Point3d pt0 = new Point3d(cen.X - height / 2, cen.Y - depth / 2, cen.Z - length / 2);
                            Point3d pt1 = new Point3d(cen.X + height / 2, cen.Y + depth / 2, cen.Z + length / 2);
                            BoundingBox box = new BoundingBox(pt0, pt1);
                            Brep brep = box.ToBrep();
                            Guid resistorID = myDoc.Objects.AddBrep(brep);
                            myDoc.Views.Redraw();

                            dataset.currPart3DP.Depth = depth;
                            dataset.currPart3DP.Height = height;
                            dataset.currPart3DP.Length = length;
                            dataset.currPart3DP.PrtID = resistorID;

                            dataset.currPart3DP.PartPos = cen;
                            dataset.currPart3DP.IsDeployed = true;
                            dataset.currPart3DP.Pins.Add(new Pin("pin1", 1, cen.X, cen.Y, cen.Z - length / 2));
                            dataset.currPart3DP.Pins.Add(new Pin("pin2", 2, cen.X, cen.Y, cen.Z + length / 2));

                        }
                        break;
                    default: break;
                }

                #endregion


                #region Step 3: add the current 3D-printed part to the part list
                Part3DP newPart3DP = new Part3DP();
                newPart3DP.Depth = dataset.currPart3DP.Depth;
                newPart3DP.Height = dataset.currPart3DP.Height;
                newPart3DP.IsDeployed = dataset.currPart3DP.IsDeployed;
                newPart3DP.Length = dataset.currPart3DP.Length;
                newPart3DP.PartName = dataset.currPart3DP.PartName;
                newPart3DP.PartPos = dataset.currPart3DP.PartPos;
                foreach(Pin pin in dataset.currPart3DP.Pins)
                {
                    newPart3DP.Pins.Add(pin);
                }
                newPart3DP.PrtID = dataset.currPart3DP.PrtID;
                newPart3DP.Type = dataset.currPart3DP.Type;
                dataset.part3DPList.Add(newPart3DP);

                #endregion

                DA.SetData(0, true);
            }
            else
            {
                DA.SetData(0, false);
            }

            
            //foreach(Part3DP p3DP in dataset.part3DPList)
            //{
            //    output.Add(p3DP.PartName);
            //}

            //if(output == null)
            //{
            //    output.Add(" ");
            //    output.Add(" ");
            //}

            
        }

        private void Ec_pos_DynamicDraw(object sender, Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            if (!(selPrtPosPt.X == Int32.MaxValue && selPrtPosPt.Y == Int32.MaxValue && selPrtPosPt.Z == Int32.MaxValue))
            {
                e.Display.DrawSphere(new Sphere(selPrtPosPt, currBody.rtThickness), Color.FromArgb(48, 163, 229));

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
            get { return new Guid("bd2dcc99-6dd7-49f4-9729-480fc5ad375f"); }
        }
    }
}