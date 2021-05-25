using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using ModelecComponents.Routing;
using ModelecComponents.A_star;

namespace ModelecComponents
{
    public class AutoRouteCommand : GH_Component
    {
        bool testBtnClick;
        RhinoDoc myDoc;
        SharedData dataset;
        TargetBody currBody;
        ProcessingWindow prcWin;
        Point3d selPrtPosPt;
        ObjectAttributes solidAttribute, orangeAttribute, redAttribute, blueAttribute;
        PrinterProfile printMeta;
        double gridSize;
        double radius;
        double traceSpacing;

        List<Point3d> route;

        /// <summary>
        /// Initializes a new instance of the AutoRouteCommand class.
        /// </summary>
        public AutoRouteCommand()
          : base("AutoRouteCommand", "AuRou",
              "The traces are automatically generated based on the imported circuit design",
              "ModElec", "TraceOperations")
        {
            testBtnClick = false;
            myDoc = RhinoDoc.ActiveDoc;
            dataset = SharedData.Instance;
            currBody = TargetBody.Instance;
            prcWin = new ProcessingWindow();
            selPrtPosPt = new Point3d();
            printMeta = PrinterProfile.Instance;
            gridSize = 0;
            radius = 0;
            traceSpacing = 0;

            route = new List<Point3d>();

            int solidIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material solidMat = myDoc.Materials[solidIndex];
            solidMat.DiffuseColor = System.Drawing.Color.White;
            solidMat.SpecularColor = System.Drawing.Color.White;
            solidMat.Transparency = 0;
            solidMat.CommitChanges();
            solidAttribute = new ObjectAttributes();
            //solidAttribute.LayerIndex = 2;
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
            //orangeAttribute.LayerIndex = 3;
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
            //redAttribute.LayerIndex = 4;
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
            //blueAttribute.LayerIndex = 5;
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
            pManager.AddBooleanParameter("AutoRouteBtnClicked", "BC", "Auto-routing", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("AddAutoTracesListUpdated", "ListUp1", "the list is updated based on the auto-generated traces", GH_ParamAccess.item);
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

                    }break;

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
        /// Test if the curve is completely  inside a brep in Rhino
        /// </summary>
        /// <param name="crv">the target curve</param>
        /// <param name="body">the target brep</param>
        /// <returns></returns>
        public bool CurveInModel(Curve crv, Brep body, double diameter)
        {
            bool result = false;
            double radius = diameter / 2;

            var pipes = Brep.CreatePipe(crv, radius, false, PipeCapMode.Round, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians);

            //myDoc.Objects.AddBrep(pipes[0], orangeAttribute);
            //myDoc.Views.Redraw();

            var intersections = Brep.CreateBooleanDifference(pipes[0], body, myDoc.ModelAbsoluteTolerance);

            if (intersections.Length > 0)
                result = false;
            else
                result = true;

            return result;

        }

        /// <summary>
        /// Test if two straight pipes intersect with each other
        /// </summary>`
        /// <param name="trc1">the first trace for constructing the first pipe</param>
        /// <param name="tr2">the second trace for constructing the second pipe</param>
        /// <returns></returns>
        private bool PipeIsIntersecting(Trace trc1, Trace trc2, double diameter)
        {
            bool result = false;
            double radius = diameter / 2;

            Line ln1 = new Line(trc1.PrtAPos, trc1.PrtBPos);
            Curve crv1 = ln1.ToNurbsCurve();
            var pipe1 = Brep.CreatePipe(crv1, radius, false, PipeCapMode.Round, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians)[0];

            Line ln2 = new Line(trc2.PrtAPos, trc2.PrtBPos);
            Curve crv2 = ln2.ToNurbsCurve();
            var pipe2 = Brep.CreatePipe(crv2, radius, false, PipeCapMode.Round, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians)[0];


            // Test the direction of the brep
            var faces1 = pipe1.Faces;
            Point3d cen1 = pipe1.GetBoundingBox(true).Center;
            double u, v;
            faces1[0].ClosestPoint(cen1, out u, out v);
            Point3d f_closet1 = faces1[0].PointAt(u, v);

            Vector3d f_normal1 = faces1[0].NormalAt(u, v);
            Vector3d f_test1 = cen1 - f_closet1;

            double dot_product1 = f_normal1.X * f_test1.X + f_normal1.Y * f_test1.Y + f_normal1.Z * f_test1.Z;
            if (dot_product1 > 0)
            {
                pipe1.Flip();
            }

            // Test the direction of the brep
            var faces2 = pipe2.Faces;
            Point3d cen2 = pipe2.GetBoundingBox(true).Center;
            double u1, v1;
            faces2[0].ClosestPoint(cen2, out u1, out v1);
            Point3d f_closet2 = faces2[0].PointAt(u1, v1);

            Vector3d f_normal2 = faces2[0].NormalAt(u1, v1);
            Vector3d f_test2 = cen2 - f_closet2;

            double dot_product2 = f_normal2.X * f_test2.X + f_normal2.Y * f_test2.Y + f_normal2.Z * f_test2.Z;
            if (dot_product2 > 0)
            {
                pipe2.Flip();
            }

            Brep[] intersections;
            intersections = Brep.CreateBooleanIntersection( pipe1, pipe2, myDoc.ModelAbsoluteTolerance);

            if ((trc1.PrtBPos == trc2.PrtBPos) && (trc1.PrtAPos != trc2.PrtAPos))
                result = false;
            else if (intersections != null && intersections.Length != 0 && !((trc1.PrtBPos == trc2.PrtBPos) && (trc1.PrtAPos == trc2.PrtAPos)))
                result = true;
            else
                result = false;

            return result;
        }
        /// <summary>
        /// Find the intersecting point of a line and a triangle
        /// 1. Determine the point of intersection
        /// 2. Determine if the intesecting point is in or out of the triangle
        /// 
        /// </summary>
        /// <param name="p">the original point of ray</param>
        /// <param name="dir">the direction of the ray</param>
        /// <param name="f">the test triangle</param>
        /// <param name="m">the mesh</param>
        /// <returns>
        ///     -1 if the triangle is not intersecting with the ray or the intersection point is out of the triangle
        ///     otherwise, the value is the scalar on the Z-axis of the intersection point
        /// </returns>
        private double IntersectRay(Point3d p, Vector3d dir, MeshFace f, Mesh m)
        {
            double scalar = -1;

            // Find three vertices of the mesh triangle
            Point3d verA = m.Vertices.ElementAt(f.A);
            Point3d verB = m.Vertices.ElementAt(f.B);
            Point3d verC = m.Vertices.ElementAt(f.C);

            Vector3d B_A = new Vector3d(verB.X - verA.X, verB.Y - verA.Y, verB.Z - verA.Z);
            Vector3d C_A = new Vector3d(verC.X - verA.X, verC.Y - verA.Y, verC.Z - verA.Z);
            Vector3d n = new Vector3d(Vector3d.CrossProduct(B_A, C_A).X / Vector3d.CrossProduct(B_A, C_A).Length, Vector3d.CrossProduct(B_A, C_A).Y / Vector3d.CrossProduct(B_A, C_A).Length, Vector3d.CrossProduct(B_A, C_A).Z / Vector3d.CrossProduct(B_A, C_A).Length);
            double d = n.X * verA.X + n.Y * verA.Y + n.Z * verA.Z;
            double t = (d - (n.X * p.X + n.Y * p.Y + n.Z * p.Z)) / (n.X * dir.X + n.Y * dir.Y + n.Z * dir.Z);

            // Calculate the intersecting point Q
            Point3d Q = new Point3d(p.X + t * dir.X, p.Y + t * dir.Y, p.Z + t * dir.Z);

            Vector3d C_B = new Vector3d(verC.X - verB.X, verC.Y - verB.Y, verC.Z - verB.Z);
            Vector3d A_C = new Vector3d(verA.X - verC.X, verA.Y - verC.Y, verA.Z - verC.Z);

            Vector3d Q_A = new Vector3d(Q.X - verA.X, Q.Y - verA.Y, Q.Z - verA.Z);
            Vector3d Q_B = new Vector3d(Q.X - verB.X, Q.Y - verB.Y, Q.Z - verB.Z);
            Vector3d Q_C = new Vector3d(Q.X - verC.X, Q.Y - verC.Y, Q.Z - verC.Z);

            Vector3d testVec1 = Vector3d.CrossProduct(B_A, Q_A);
            Vector3d testVec2 = Vector3d.CrossProduct(C_B, Q_B);
            Vector3d testVec3 = Vector3d.CrossProduct(A_C, Q_C);

            double testAngle1 = testVec1.X * n.X + testVec1.Y * n.Y + testVec1.Z * n.Z;
            double testAngle2 = testVec2.X * n.X + testVec2.Y * n.Y + testVec2.Z * n.Z;
            double testAngle3 = testVec3.X * n.X + testVec3.Y * n.Y + testVec3.Z * n.Z;

            if (testAngle1 >= 0 && testAngle2 >= 0 && testAngle3 >= 0)
            {
                // Q (intersecting point) is inside the triangle
                scalar = t;
            }

            return scalar;
        }

        /// <summary>
        /// Test if a voxle is inside or outside a model
        /// </summary>
        /// <param name="d">input double value</param>
        /// <param name="hits">
        ///     the list of doubles that is used to test if the input double falls between two consecutive doubles, but it stores the scalar
        ///     so we should use base
        /// </param>
        /// <param name="b">the base double value used for testing</param>
        /// <returns>0 - not intersecting; 1 - intersecting on the surface; 2 - intersecting in the body</returns>
        private int TestInOutModel(double d, List<double> hits, double b)
        {
            int result = 0;

            //if(hits.Count() != 0)
            //{
            //    if(hits.Count()%2 == 0)
            //    {
            //        // assume the model dose not have a single surface (open geometry)
            //        for(int idx = 0; idx < hits.Count(); idx++)
            //        {
            //            double actualValue = hits.ElementAt(idx) + b;
            //            if(d >= actualValue)
            //            {
            //                continue;
            //            }
            //            else
            //            {
            //                if(idx%2 != 0)
            //                {
            //                    double previousVoxel = hits.ElementAt(idx - 1) + b;

            //                    if(((d - previousVoxel) <= gridSize) || ((actualValue - d) <= gridSize))
            //                    {
            //                        result = 1;
            //                    }
            //                    else
            //                    {
            //                        result = 2;
            //                    }
            //                }
            //                break;
            //            }
            //        }
            //    }
            //}

            int order = 0;
            if (hits.Count() != 0)
            {
                if (hits.Count() % 2 == 0)
                {
                    // assume the model dose not have a single surface (open geometry)
                    for (int idx = 0; idx < hits.Count(); idx++)
                    {
                        double actualValue = hits.ElementAt(idx) + b;
                        if (d > actualValue)
                        {
                            order++;
                            continue;
                        }
                        else if (d < actualValue)
                        {
                            if (order % 2 == 0)
                            {
                                result = 0;
                                break;
                            }
                            else if (order % 2 == 1)
                            {
                                double previousVoxel = hits.ElementAt(idx - 1) + b;
                                if (((d - previousVoxel) <= gridSize) || ((actualValue - d) <= gridSize))
                                {
                                    result = 1;
                                }
                                else
                                {
                                    result = 2;
                                }
                                break;
                            }
                        }
                        else
                        {
                            // on the surface
                            result = 1;
                            break;
                        }
                    }
                }
                else
                {
                    for (int idx = 0; idx < hits.Count(); idx++)
                    {
                        double actualValue = hits.ElementAt(idx) + b;
                        if (d == actualValue)
                        {
                            result = 1;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public void Voxelize(ObjRef obj_ref, out List<Point3d> bodyPts, out List<Point3d> surfPts, out List<Point3d> allPts)
        {
            surfPts = new List<Point3d>();
            bodyPts = new List<Point3d>();
            allPts = new List<Point3d>();
           
            //surfPts.Clear();

            #region Convert the selected body to a mesh

            var brep = obj_ref.Brep();

            if (null == brep)
                return;
            var default_mesh_params = MeshingParameters.Default;
            var minimal = MeshingParameters.Minimal;

            var meshes = Mesh.CreateFromBrep(brep, minimal);
            if (meshes == null || meshes.Length == 0)
                return;

            var brep_mesh = new Mesh(); // the mesh of the currently selected body

            foreach (var mesh in meshes)
                brep_mesh.Append(mesh);
            brep_mesh.Faces.ConvertQuadsToTriangles();
            #endregion

            #region get the bounding box of the selected body
            var bbox = brep_mesh.GetBoundingBox(true);

            Point3d[] corners = bbox.GetCorners();
            Point3d[] rayPlanePts = new Point3d[4];

            // select the XY plane as the shooting-ray plane
            rayPlanePts[0] = corners[0];
            rayPlanePts[1] = corners[1];
            rayPlanePts[2] = corners[2];
            rayPlanePts[3] = corners[3];

            // construct the hits array
            double realX = rayPlanePts[0].X - gridSize / 2;
            double realY = rayPlanePts[0].Y - gridSize / 2;
            double realZ = rayPlanePts[0].Z - gridSize / 2;

            double width = rayPlanePts[1].X + gridSize / 2 - realX;
            double height = rayPlanePts[3].Y + gridSize / 2 - realY;
            double depth = corners[4].Z + gridSize / 2 - realZ;

            int num_X = Convert.ToInt32(Math.Ceiling(width / gridSize));
            int num_Y = Convert.ToInt32(Math.Ceiling(height / gridSize));
            int num_Z = Convert.ToInt32(Math.Ceiling(depth / gridSize));

            List<double>[,] hits = new List<double>[num_Y, num_X];
            #endregion

            for (int r = 0; r < num_Y; r++)
            {
                for (int c = 0; c < num_X; c++)
                {
                    hits[r, c] = new List<double>();
                }
            }

            #region shoot ray from the points on the XY-plane found above, calling IntersectRay function
            foreach (MeshFace mf in brep_mesh.Faces)
            {
                // the mesh face is triangle so that C = D
                // call IntersectRay function

                // find the set of points that will potentially shoot rays interacting with the triangle
                Point3d tri_verA = brep_mesh.Vertices.ElementAt(mf.A);
                Point3d tri_verB = brep_mesh.Vertices.ElementAt(mf.B);
                Point3d tri_verC = brep_mesh.Vertices.ElementAt(mf.C);

                double tri_verA_X = tri_verA.X;
                double tri_verA_Y = tri_verA.Y;
                double tri_verB_X = tri_verB.X;
                double tri_verB_Y = tri_verB.Y;
                double tri_verC_X = tri_verC.X;
                double tri_verC_Y = tri_verC.Y;

                double x_max = Math.Max(Math.Max(tri_verA_X, tri_verB_X), Math.Max(tri_verB_X, tri_verC_X));
                double y_max = Math.Max(Math.Max(tri_verA_Y, tri_verB_Y), Math.Max(tri_verB_Y, tri_verC_Y));
                double x_min = Math.Min(Math.Min(tri_verA_X, tri_verB_X), Math.Min(tri_verB_X, tri_verC_X));
                double y_min = Math.Min(Math.Min(tri_verA_Y, tri_verB_Y), Math.Min(tri_verB_Y, tri_verC_Y));

                for (int r = Convert.ToInt32(Math.Floor((y_min - realY) / gridSize)); r < Convert.ToInt32(Math.Ceiling((y_max - realY) / gridSize)); r++)
                {
                    for (int c = Convert.ToInt32(Math.Floor((x_min - realX) / gridSize)); c < Convert.ToInt32(Math.Ceiling((x_max - realX) / gridSize)); c++)
                    {
                        Point3d p = new Point3d(realX + gridSize * c, realY + gridSize * r, realZ);
                        Vector3d dir = new Vector3d(0, 0, 1);
                        //Line ray = new Line(p, dir, 30);
                        //myDoc.Objects.AddLine(ray, redAttribute);

                        double scalar = IntersectRay(p, dir, mf, brep_mesh);

                        if (scalar != -1)
                        {
                            // there is an intersection point
                            hits[r, c].Add(scalar);
                            // order the points
                            hits[r, c].Sort();
                        }
                    }
                }
            }
            #endregion

            #region find all voxels on the surface and in the body

            double[,,] voxels = new double[num_Y, num_X, num_Z];

            for (int r = 0; r < num_Y; r++)
            {
                for (int c = 0; c < num_X; c++)
                {
                    for (int d = 0; d < num_Z; d++)
                    {
                        if (hits[r, c].Count() != 0)
                        {
                            double currDepth = realZ + gridSize * d;

                            if (TestInOutModel(currDepth, hits[r, c], realZ) == 1)
                            {
                                // Surface points
                                Point3d voxelPt = new Point3d(realX + gridSize * c, realY + gridSize * r, currDepth);
                                surfPts.Add(voxelPt);
                            }
                            else if (TestInOutModel(currDepth, hits[r, c], realZ) == 2)
                            {
                                // Body points
                                Point3d voxelPt = new Point3d(realX + gridSize * c, realY + gridSize * r, currDepth);
                                bodyPts.Add(voxelPt);
                            }

                            //// visualize all the voxels
                            //if (TestInOutModel(currDepth, hits[r, c], realZ) != 0)
                            //{
                            //    Point3d voxelPt = new Point3d(realX + gridSize * c, realY + gridSize * r, currDepth);


                            //    bodyPts.Add(voxelPt);
                            //}

                        }
                    }

                }
            }
            #endregion

            #region commented for YZ-plane and XZ-plane surface voxels validation
            //#region validate the surface voxels using YZ-plane
            //// select the YZ plane as the shooting-ray plane
            //Point3d[] YZPlanePts = new Point3d[4];
            //YZPlanePts[0] = corners[0];
            //YZPlanePts[1] = corners[3];
            //YZPlanePts[2] = corners[4];
            //YZPlanePts[3] = corners[7];

            //// construct the hits array
            //double YZ_realX = YZPlanePts[0].X + gridSize / 2;
            //double YZ_realY = YZPlanePts[0].Y + gridSize / 2;
            //double YZ_realZ = YZPlanePts[0].Z + gridSize / 2;

            //double YZ_width = YZPlanePts[1].Y - YZ_realY;
            //double YZ_height = YZPlanePts[2].Z - YZ_realZ;
            //double YZ_depth = corners[1].X - YZ_realX;

            //int YZ_num_X = Convert.ToInt32(Math.Ceiling(YZ_width / gridSize));
            //int YZ_num_Y = Convert.ToInt32(Math.Ceiling(YZ_height / gridSize));
            //int YZ_num_Z = Convert.ToInt32(Math.Ceiling(YZ_depth / gridSize));

            //List<double>[,] YZ_hits = new List<double>[YZ_num_Y, YZ_num_X];

            //for (int r = 0; r < YZ_num_Y; r++)
            //{
            //    for (int c = 0; c < YZ_num_X; c++)
            //    {
            //        YZ_hits[r, c] = new List<double>();
            //    }
            //}

            //foreach (MeshFace mf in brep_mesh.Faces)
            //{
            //    // the mesh face is triangle so that C = D
            //    // call IntersectRay function

            //    // find the set of points that will potentially shoot rays interacting with the triangle
            //    Point3d tri_verA = brep_mesh.Vertices.ElementAt(mf.A);
            //    Point3d tri_verB = brep_mesh.Vertices.ElementAt(mf.B);
            //    Point3d tri_verC = brep_mesh.Vertices.ElementAt(mf.C);

            //    double tri_verA_Z = tri_verA.Z;
            //    double tri_verA_Y = tri_verA.Y;
            //    double tri_verB_Z = tri_verB.Z;
            //    double tri_verB_Y = tri_verB.Y;
            //    double tri_verC_Z = tri_verC.Z;
            //    double tri_verC_Y = tri_verC.Y;

            //    double y_max = Math.Max(Math.Max(tri_verA_Y, tri_verB_Y), Math.Max(tri_verB_Y, tri_verC_Y));
            //    double z_max = Math.Max(Math.Max(tri_verA_Z, tri_verB_Z), Math.Max(tri_verB_Z, tri_verC_Z));
            //    double y_min = Math.Min(Math.Min(tri_verA_Y, tri_verB_Y), Math.Min(tri_verB_Y, tri_verC_Y));
            //    double z_min = Math.Min(Math.Min(tri_verA_Z, tri_verB_Z), Math.Min(tri_verB_Z, tri_verC_Z));

            //    for (int r = Convert.ToInt32(Math.Floor((z_min - YZ_realZ) / gridSize)); r < Convert.ToInt32(Math.Ceiling((z_max - YZ_realZ) / gridSize)); r++)
            //    {
            //        for (int c = Convert.ToInt32(Math.Floor((y_min - YZ_realY) / gridSize)); c < Convert.ToInt32(Math.Ceiling((y_max - YZ_realY) / gridSize)); c++)
            //        {
            //            Point3d p = new Point3d(YZ_realX, YZ_realY + gridSize * c, YZ_realZ + gridSize * r);
            //            Vector3d dir = new Vector3d(1, 0, 0);
            //            //Line ray = new Line(p, dir, 30);
            //            //myDoc.Objects.AddLine(ray, redAttribute);

            //            double scalar = IntersectRay(p, dir, mf, brep_mesh);

            //            if (scalar != -1)
            //            {
            //                // there is an intersection point
            //                YZ_hits[r, c].Add(scalar);
            //                // order the points
            //                YZ_hits[r, c].Sort();
            //            }
            //        }
            //    }
            //}

            //double[,,] YZ_voxels = new double[YZ_num_Y, YZ_num_X, YZ_num_Z];

            //for (int r = 0; r < YZ_num_Y; r++)
            //{
            //    for (int c = 0; c < YZ_num_X; c++)
            //    {
            //        for (int d = 0; d < YZ_num_Z; d++)
            //        {
            //            if (YZ_hits[r, c].Count() != 0)
            //            {
            //                double currDepth = YZ_realX + gridSize * d;

            //                if (TestInOutModel(currDepth, YZ_hits[r, c], YZ_realX) == 1)
            //                {
            //                    // Surface points
            //                    Point3d voxelPt = new Point3d(currDepth, YZ_realY + gridSize * c, YZ_realZ + gridSize * r);
            //                    if (surfPts.IndexOf(voxelPt) == -1)
            //                    {
            //                        // the voxel is not counted as a voxel on the surface
            //                        surfPts.Add(voxelPt);
            //                    }
            //                    int v_idx = bodyPts.IndexOf(voxelPt);
            //                    if (v_idx != -1)
            //                    {
            //                        bodyPts.RemoveAt(v_idx);
            //                    }
            //                }
            //            }
            //        }

            //    }
            //}

            //#endregion

            //#region validate the surface voxels using XZ-plane
            //// select the XZ plane as the shooting-ray plane
            //Point3d[] XZPlanePts = new Point3d[4];
            //XZPlanePts[0] = corners[0];
            //XZPlanePts[1] = corners[1];
            //XZPlanePts[2] = corners[4];
            //XZPlanePts[3] = corners[5];

            //// construct the hits array
            //double XZ_realX = XZPlanePts[0].X + gridSize / 2;
            //double XZ_realY = XZPlanePts[0].Y + gridSize / 2;
            //double XZ_realZ = XZPlanePts[0].Z + gridSize / 2;

            //double XZ_width = XZPlanePts[1].X - XZ_realX;
            //double XZ_height = XZPlanePts[2].Z - XZ_realZ;
            //double XZ_depth = corners[2].Y - XZ_realY;

            //int XZ_num_X = Convert.ToInt32(Math.Ceiling(XZ_width / gridSize));
            //int XZ_num_Y = Convert.ToInt32(Math.Ceiling(XZ_height / gridSize));
            //int XZ_num_Z = Convert.ToInt32(Math.Ceiling(XZ_depth / gridSize));

            //List<double>[,] XZ_hits = new List<double>[XZ_num_Y, XZ_num_X];

            //for (int r = 0; r < XZ_num_Y; r++)
            //{
            //    for (int c = 0; c < XZ_num_X; c++)
            //    {
            //        XZ_hits[r, c] = new List<double>();
            //    }
            //}

            //foreach (MeshFace mf in brep_mesh.Faces)
            //{
            //    // the mesh face is triangle so that C = D
            //    // call IntersectRay function

            //    // find the set of points that will potentially shoot rays interacting with the triangle
            //    Point3d tri_verA = brep_mesh.Vertices.ElementAt(mf.A);
            //    Point3d tri_verB = brep_mesh.Vertices.ElementAt(mf.B);
            //    Point3d tri_verC = brep_mesh.Vertices.ElementAt(mf.C);

            //    double tri_verA_Z = tri_verA.Z;
            //    double tri_verA_X = tri_verA.X;
            //    double tri_verB_Z = tri_verB.Z;
            //    double tri_verB_X = tri_verB.X;
            //    double tri_verC_Z = tri_verC.Z;
            //    double tri_verC_X = tri_verC.X;

            //    double x_max = Math.Max(Math.Max(tri_verA_X, tri_verB_X), Math.Max(tri_verB_X, tri_verC_X));
            //    double z_max = Math.Max(Math.Max(tri_verA_Z, tri_verB_Z), Math.Max(tri_verB_Z, tri_verC_Z));
            //    double x_min = Math.Min(Math.Min(tri_verA_X, tri_verB_X), Math.Min(tri_verB_X, tri_verC_X));
            //    double z_min = Math.Min(Math.Min(tri_verA_Z, tri_verB_Z), Math.Min(tri_verB_Z, tri_verC_Z));

            //    for (int r = Convert.ToInt32(Math.Floor((z_min - XZ_realZ) / gridSize)); r < Convert.ToInt32(Math.Ceiling((z_max - XZ_realZ) / gridSize)); r++)
            //    {
            //        for (int c = Convert.ToInt32(Math.Floor((x_min - XZ_realX) / gridSize)); c < Convert.ToInt32(Math.Ceiling((x_max - XZ_realX) / gridSize)); c++)
            //        {
            //            Point3d p = new Point3d(XZ_realX + gridSize * c, XZ_realY, XZ_realZ + gridSize * r);
            //            Vector3d dir = new Vector3d(0, 1, 0);
            //            //Line ray = new Line(p, dir, 30);
            //            //myDoc.Objects.AddLine(ray, redAttribute);

            //            double scalar = IntersectRay(p, dir, mf, brep_mesh);

            //            if (scalar != -1)
            //            {
            //                // there is an intersection point
            //                XZ_hits[r, c].Add(scalar);
            //                // order the points
            //                XZ_hits[r, c].Sort();
            //            }
            //        }
            //    }
            //}

            //double[,,] XZ_voxels = new double[XZ_num_Y, XZ_num_X, XZ_num_Z];

            //for (int r = 0; r < XZ_num_Y; r++)
            //{
            //    for (int c = 0; c < XZ_num_X; c++)
            //    {
            //        for (int d = 0; d < XZ_num_Z; d++)
            //        {
            //            if (XZ_hits[r, c].Count() != 0)
            //            {
            //                double currDepth = XZ_realY + gridSize * d;

            //                if (TestInOutModel(currDepth, XZ_hits[r, c], XZ_realY) == 1)
            //                {
            //                    // Surface points
            //                    Point3d voxelPt = new Point3d(XZ_realX + gridSize * c, currDepth, XZ_realZ + gridSize * r);
            //                    if (surfPts.IndexOf(voxelPt) == -1)
            //                    {
            //                        // the voxel is not counted as a voxel on the surface
            //                        surfPts.Add(voxelPt);
            //                    }
            //                    int v_idx = bodyPts.IndexOf(voxelPt);
            //                    if (v_idx != -1)
            //                    {
            //                        bodyPts.RemoveAt(v_idx);
            //                    }
            //                }
            //            }
            //        }

            //    }
            //}

            //#endregion
            #endregion

            //foreach (Point3d pt in surfPts)
            //{
            //    //Guid ptID = myDoc.Objects.AddPoint(pt, blueAttribute);
            //    //myDoc.Objects.Hide(ptID, true);
            //    allPts.Add(pt);
            //}
            foreach (Point3d pt in bodyPts)
            {
                //myDoc.Objects.AddPoint(pt, redAttribute);
                allPts.Add(pt);
            }
        }

        private double distPins(List<Pin> pins)
        {
            double result = double.MaxValue;
            foreach(Pin p1 in pins)
            {
                foreach(Pin p2 in pins)
                {
                    if (p1.Equals(p2))
                        continue;
                    else if (p1.Pos.DistanceTo(p2.Pos) == 0)
                        continue;
                    else
                    {
                        if (p1.Pos.DistanceTo(p2.Pos) <= result)
                        {
                            result = p1.Pos.DistanceTo(p2.Pos);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Get the closest point to a given point from a cloud of points
        /// </summary>
        /// <param name="target">the target point</param>
        /// <param name="pCloud">the list of points to examine </param>
        /// <returns></returns>
        private Point3d GetClosestFromPointCloudSimple(Point3d target, List<Point3d> pCloud)
        {
            Point3d result = new Point3d();

            double min_dis = Double.MaxValue;

            foreach (Point3d pt in pCloud)
            {
                double dis = pt.DistanceTo(target);

                if (dis <= min_dis)
                {
                    min_dis = dis;
                    result = pt;
                }
            }

            return result;
        }

        /// <summary>
        /// Get the closest point to a given point from a cloud of points
        /// </summary>
        /// <param name="target">the target point</param>
        /// <param name="pCloud">the list of points to examine</param>
        /// <param name="dir">the direction for finding the closest point</param>
        /// <returns></returns>
        private Point3d GetClosestFromPointCloud(Point3d target, List<Point3d> pCloud, Vector3d dir)
        {
            Point3d result = new Point3d();

            double min_dis = Double.MaxValue;

            foreach (Point3d pt in pCloud)
            {
                // test if the point is in the expected direction

                Vector3d v1 = new Vector3d(pt - target);
                double dot_prod = v1.X * dir.X + v1.Y * dir.Y + v1.Z * dir.Z;

                if(dot_prod > 0)
                {
                    double dis = pt.DistanceTo(target);

                    if (dis <= min_dis)
                    {
                        min_dis = dis;
                        result = pt;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// calculate the heuristic value of the a point to the target point using the Euclidean distance
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="targetPt"></param>
        /// <returns></returns>
        double ComputeHScore(double x, double y, double z, double targetX, double targetY, double targetZ)
        {
            return Math.Sqrt(Math.Pow(targetX - x, 2) + Math.Pow(targetY - y, 2) + Math.Pow(targetZ - z, 2));
        }

        List<Location> GetWalkableAdjacentVoxels(double x, double y, double z, List<Point3d> availableVoxels, List<Location> openList)
        {
            List<Location> proposedLocations = new List<Location>();

            if (availableVoxels.IndexOf(new Point3d(x, y, z + gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == x && l.Y == y && l.Z == (z + gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x, Y = y, Z = z + gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x + gridSize, y - gridSize, z + gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == (x + gridSize) && l.Y == (y - gridSize) && l.Z == (z + gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x + gridSize, Y = y - gridSize, Z = z + gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x + gridSize, y, z + gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == (x + gridSize) && l.Y == y && l.Z == (z + gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x + gridSize, Y = y, Z = z + gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x - gridSize, y, z + gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == (x - gridSize) && l.Y == y && l.Z == (z + gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x - gridSize, Y = y, Z = z + gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x, y + gridSize, z + gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == x && l.Y == (y + gridSize) && l.Z == (z + gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x, Y = y + gridSize, Z = z + gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x, y - gridSize, z + gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == x && l.Y == (y - gridSize) && l.Z == (z + gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x, Y = y - gridSize, Z = z + gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x + gridSize, y + gridSize, z + gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == (x + gridSize) && l.Y == (y + gridSize) && l.Z == (z + gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x + gridSize, Y = y + gridSize, Z = z + gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x - gridSize, y - gridSize, z + gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == (x - gridSize) && l.Y == (y - gridSize) && l.Z == (z + gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x - gridSize, Y = y - gridSize, Z = z + gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x - gridSize, y + gridSize, z + gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == (x - gridSize) && l.Y == (y + gridSize) && l.Z == (z + gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x - gridSize, Y = y + gridSize, Z = z + gridSize });
                else proposedLocations.Add(node);
            }


            if (availableVoxels.IndexOf(new Point3d(x, y, z - gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == x && l.Y == y && l.Z == (z - gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x, Y = y, Z = z - gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x + gridSize, y - gridSize, z - gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == (x + gridSize) && l.Y == (y - gridSize) && l.Z == (z - gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x + gridSize, Y = y - gridSize, Z = z - gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x + gridSize, y, z - gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == (x + gridSize) && l.Y == y && l.Z == (z - gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x + gridSize, Y = y, Z = z - gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x - gridSize, y, z - gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == (x - gridSize) && l.Y == y && l.Z == (z - gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x - gridSize, Y = y, Z = z - gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x, y + gridSize, z - gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == x && l.Y == (y + gridSize) && l.Z == (z - gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x, Y = y + gridSize, Z = z - gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x, y - gridSize, z - gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == x && l.Y == (y - gridSize) && l.Z == (z - gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x, Y = y - gridSize, Z = z - gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x + gridSize, y + gridSize, z - gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == (x + gridSize) && l.Y == (y + gridSize) && l.Z == (z - gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x + gridSize, Y = y + gridSize, Z = z - gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x - gridSize, y - gridSize, z - gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == (x - gridSize) && l.Y == (y - gridSize) && l.Z == (z - gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x - gridSize, Y = y - gridSize, Z = z - gridSize });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x - gridSize, y + gridSize, z - gridSize)) != -1)
            {
                Location node = openList.Find(l => l.X == (x - gridSize) && l.Y == (y + gridSize) && l.Z == (z - gridSize));
                if (node == null) proposedLocations.Add(new Location() { X = x - gridSize, Y = y + gridSize, Z = z - gridSize });
                else proposedLocations.Add(node);
            }


            if (availableVoxels.IndexOf(new Point3d(x + gridSize, y - gridSize, z)) != -1)
            {
                Location node = openList.Find(l => l.X == (x + gridSize) && l.Y == (y - gridSize) && l.Z == z);
                if (node == null) proposedLocations.Add(new Location() { X = x + gridSize, Y = y - gridSize, Z = z });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x + gridSize, y, z)) != -1)
            {
                Location node = openList.Find(l => l.X == (x + gridSize) && l.Y == y && l.Z == z);
                if (node == null) proposedLocations.Add(new Location() { X = x + gridSize, Y = y, Z = z });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x - gridSize, y, z)) != -1)
            {
                Location node = openList.Find(l => l.X == (x - gridSize) && l.Y == y && l.Z == z);
                if (node == null) proposedLocations.Add(new Location() { X = x - gridSize, Y = y, Z = z });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x, y + gridSize, z)) != -1)
            {
                Location node = openList.Find(l => l.X == x && l.Y == (y + gridSize) && l.Z == z);
                if (node == null) proposedLocations.Add(new Location() { X = x, Y = y + gridSize, Z = z });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x, y - gridSize, z)) != -1)
            {
                Location node = openList.Find(l => l.X == x && l.Y == (y - gridSize) && l.Z == z);
                if (node == null) proposedLocations.Add(new Location() { X = x, Y = y - gridSize, Z = z });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x + gridSize, y + gridSize, z)) != -1)
            {
                Location node = openList.Find(l => l.X == (x + gridSize) && l.Y == (y + gridSize) && l.Z == z);
                if (node == null) proposedLocations.Add(new Location() { X = x + gridSize, Y = y + gridSize, Z = z });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x - gridSize, y - gridSize, z)) != -1)
            {
                Location node = openList.Find(l => l.X == (x - gridSize) && l.Y == (y - gridSize) && l.Z == z);
                if (node == null) proposedLocations.Add(new Location() { X = x - gridSize, Y = y - gridSize, Z = z });
                else proposedLocations.Add(node);
            }

            if (availableVoxels.IndexOf(new Point3d(x - gridSize, y + gridSize, z)) != -1)
            {
                Location node = openList.Find(l => l.X == (x - gridSize) && l.Y == (y + gridSize) && l.Z == z);
                if (node == null) proposedLocations.Add(new Location() { X = x - gridSize, Y = y + gridSize, Z = z });
                else proposedLocations.Add(node);
            }

            return proposedLocations;

        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool btnClick = false;
            bool toExeAutoRoute = false;
            List<string> errors = new List<string>();

            #region read the button click and decide whether execute the circuit load or not

            if (!DA.GetData(0, ref btnClick))
                return;

            //if (!btnClick && testBtnClick)
            //{
            //    toExeAutoRoute = false;
            //    testBtnClick = false;
            //}
            //else
            if (!btnClick)
                return;
            else if (btnClick)
            {
                toExeAutoRoute = true;
                testBtnClick = true;
            }

            #endregion

            if (toExeAutoRoute)
            {
                errors.Clear();
                double extLen = 1.2;
                //gridSize = (printMeta.Trace_min_x.Find(x => x.Cond.Equals("minimized resistance")) != null) ?
                //    (Double.Parse(printMeta.Trace_min_x.Find(x => x.Cond.Equals("minimized resistance")).Value) / 1000)*2 : 0.8*2 ;
                gridSize = 2;
                radius = (printMeta.Trace_min_x.Find(x => x.Cond.Equals("minimized resistance")) != null) ?
                    (Double.Parse(printMeta.Trace_min_x.Find(x => x.Cond.Equals("minimized resistance")).Value) / 1000 / 2.0) : 0.4;
                traceSpacing = (printMeta.Trace_min_x.Find(x => x.Cond.Equals("5+mm")) != null)?
                    (Double.Parse(printMeta.Trace_spacing.Find(x => x.Cond.Equals("5+mm")).Value) / 1000) : 0.9;

                double tolerance = 0.9;
                if (Double.Parse(printMeta.Trace_min_x.Find(x => x.Cond.Equals("minimized resistance")).Value) != 0)
                    tolerance = Double.Parse(printMeta.Trace_min_x.Find(x => x.Cond.Equals("minimized resistance")).Value) / 1000;

                #region update the jump wires
                if (myDoc.Layers.FindName("JumpWires") != null)
                {
                    int jw_index = myDoc.Layers.FindName("JumpWires").Index;
                    myDoc.Layers.SetCurrentLayerIndex(jw_index, true);
                    redAttribute.LayerIndex = jw_index;

                    foreach (TraceCluster trC in dataset.traceOracle)
                    {
                        foreach (JumpWire jwOld in trC.Jumpwires)
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
                }


                #endregion

                if (!dataset.isTraceGenerated)
                {
                    #region Step 1: generate the sockets and convert the main body to the final body

                    List<Brep> finalSocketBreps = new List<Brep>();
                    List<Brep> snapFitBreps = new List<Brep>();

                    foreach (partSocket pSocket in currBody.partSockets)
                    {
                        Brep socketBox = pSocket.SocketBrep;
                        Part3D cmPrt = dataset.part3DList.Find(x => x.PartName.Equals(pSocket.PrtName));
                        if (!cmPrt.IsPrintable)
                        {
                            Guid id1 = myDoc.Objects.AddBrep(socketBox);

                            #region First, rotate back the socketBox so that we can get the bounding box

                            Part3D p = dataset.part3DList.Find(x => x.PartName.Equals(pSocket.PrtName));

                            Transform roback = Transform.Rotation((-1) * p.RotationAngle / 180.0 * Math.PI, p.Normal, p.PartPos);
                            Guid id2 = myDoc.Objects.Transform(id1, roback, true);

                            Vector3d reverseNormalVector = p.Normal * (-1);

                            Transform resetTrans = Transform.Translation(reverseNormalVector * p.ExposureLevel * p.Height);
                            Guid id2_1 = myDoc.Objects.Transform(id2, resetTrans, true);

                            Guid id3 = Guid.Empty;

                            if (p.IsFlipped)
                            {
                                Transform flipTrans = Transform.Rotation(p.Normal, reverseNormalVector, p.PartPos);

                                //socketBox.Transform(flipTrans);
                                id3 = myDoc.Objects.Transform(id2_1, flipTrans, true);
                            }
                            else
                            {
                                id3 = id2_1;
                            }

                            //socketBox.Transform(p.TransformReverseSets.ElementAt(0));
                            Guid id4 = myDoc.Objects.Transform(id3, p.TransformReverseSets.ElementAt(0), true);

                            //myDoc.Objects.AddBrep(socketBox, orangeAttribute);
                            //myDoc.Views.Redraw();

                            //BoundingBox scktBoundingBox = socketBox.GetBoundingBox(true);
                            BoundingBox scktBoundingBox = myDoc.Objects.Find(id4).Geometry.GetBoundingBox(true);
                            myDoc.Objects.Delete(id4, true);

                            //myDoc.Objects.AddBrep(scktBoundingBox.ToBrep(), blueAttribute);
                            //myDoc.Views.Redraw();

                            double height = scktBoundingBox.GetCorners().ElementAt(4).Z - scktBoundingBox.GetCorners().ElementAt(0).Z;

                            #endregion

                            #region Second, create a truncated pyramid and snap-fits

                            double slope_degree = (45.0 * Math.PI) / 180.0;
                            double offsetTP = (3 * Math.Tan(slope_degree) * Math.Sqrt(2) * 0.5 + 0.2) * (1 /*+ printMeta.Shrinkage*/);
                            if (height < 3)
                                offsetTP = ((scktBoundingBox.GetCorners()[4].Z - scktBoundingBox.GetCorners()[0].Z) * Math.Tan(slope_degree) * Math.Sqrt(2) * 0.5 + 0.2) * (1 /*+ printMeta.Shrinkage*/);

                            double miniOffsetTP = (/*1.5*/ + 0.2) * (1 /*+ printMeta.Shrinkage*/);
                            Brep truncatedSocket = new Brep();
                            if (p.IsFlipped)
                            {
                                Point3d n4 = scktBoundingBox.GetCorners()[0] + new Vector3d(-offsetTP, -offsetTP, 0);
                                Point3d n5 = scktBoundingBox.GetCorners()[1] + new Vector3d(offsetTP, -offsetTP, 0);
                                Point3d n6 = scktBoundingBox.GetCorners()[2] + new Vector3d(offsetTP, offsetTP, 0);
                                Point3d n7 = scktBoundingBox.GetCorners()[3] + new Vector3d(-offsetTP, offsetTP, 0);
                                Point3d n8 = n4;
                                List<Point3d> topRectCorners = new List<Point3d>();
                                topRectCorners.Add(n4);
                                topRectCorners.Add(n5);
                                topRectCorners.Add(n6);
                                topRectCorners.Add(n7);
                                topRectCorners.Add(n8);

                                Point3d o0 = scktBoundingBox.GetCorners()[4] + new Vector3d(-miniOffsetTP, -miniOffsetTP, 0);
                                Point3d o1 = scktBoundingBox.GetCorners()[5] + new Vector3d(miniOffsetTP, -miniOffsetTP, 0);
                                Point3d o2 = scktBoundingBox.GetCorners()[6] + new Vector3d(miniOffsetTP, miniOffsetTP, 0);
                                Point3d o3 = scktBoundingBox.GetCorners()[7] + new Vector3d(-miniOffsetTP, miniOffsetTP, 0);
                                Point3d o4 = o0;
                                List<Point3d> bottomRectCorners = new List<Point3d>();
                                bottomRectCorners.Add(o0);
                                bottomRectCorners.Add(o1);
                                bottomRectCorners.Add(o2);
                                bottomRectCorners.Add(o3);
                                bottomRectCorners.Add(o4);

                                Polyline topRect = new Polyline(topRectCorners);
                                Polyline bottomRect = new Polyline(bottomRectCorners);
                                Curve topRectCrv = topRect.ToNurbsCurve();
                                Curve bottomRectCrv = bottomRect.ToNurbsCurve();
                                Line crossLn = new Line(o0, n4);
                                Curve crossCrv = crossLn.ToNurbsCurve();

                                var sweep = new SweepOneRail();
                                sweep.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                                sweep.ClosedSweep = false;
                                sweep.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                                //myDoc.Objects.AddCurve(topRectCrv, orangeAttribute);
                                //myDoc.Views.Redraw();
                                //myDoc.Objects.AddCurve(bottomRectCrv, orangeAttribute);
                                //myDoc.Views.Redraw();
                                //myDoc.Objects.AddCurve(crossCrv, orangeAttribute);
                                //myDoc.Views.Redraw();

                                Brep[] tpBreps = sweep.PerformSweep(bottomRectCrv, crossCrv);
                                Brep tpBrep = tpBreps[0];
                                truncatedSocket = tpBrep.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);

                                // Test the direction of the brep
                                var faces = truncatedSocket.Faces;
                                Point3d cen = truncatedSocket.GetBoundingBox(true).Center;
                                double u, v;
                                faces[0].ClosestPoint(cen, out u, out v);
                                Point3d f_closet = faces[0].PointAt(u, v);

                                Vector3d f_normal = faces[0].NormalAt(u, v);
                                Vector3d f_test = cen - f_closet;

                                double dot_product = f_normal.X * f_test.X + f_normal.Y * f_test.Y + f_normal.Z * f_test.Z;
                                if (dot_product > 0)
                                {
                                    truncatedSocket.Flip();
                                }
                            }
                            else
                            {
                                Point3d n4 = scktBoundingBox.GetCorners()[4] + new Vector3d(-offsetTP, -offsetTP, 0);
                                Point3d n5 = scktBoundingBox.GetCorners()[5] + new Vector3d(offsetTP, -offsetTP, 0);
                                Point3d n6 = scktBoundingBox.GetCorners()[6] + new Vector3d(offsetTP, offsetTP, 0);
                                Point3d n7 = scktBoundingBox.GetCorners()[7] + new Vector3d(-offsetTP, offsetTP, 0);
                                Point3d n8 = n4;
                                List<Point3d> topRectCorners = new List<Point3d>();
                                topRectCorners.Add(n4);
                                topRectCorners.Add(n5);
                                topRectCorners.Add(n6);
                                topRectCorners.Add(n7);
                                topRectCorners.Add(n8);

                                Point3d o0 = scktBoundingBox.GetCorners()[0] + new Vector3d(-miniOffsetTP, -miniOffsetTP, 0);
                                Point3d o1 = scktBoundingBox.GetCorners()[1] + new Vector3d(miniOffsetTP, -miniOffsetTP, 0);
                                Point3d o2 = scktBoundingBox.GetCorners()[2] + new Vector3d(miniOffsetTP, miniOffsetTP, 0);
                                Point3d o3 = scktBoundingBox.GetCorners()[3] + new Vector3d(-miniOffsetTP, miniOffsetTP, 0);
                                Point3d o4 = o0;
                                List<Point3d> bottomRectCorners = new List<Point3d>();
                                bottomRectCorners.Add(o0);
                                bottomRectCorners.Add(o1);
                                bottomRectCorners.Add(o2);
                                bottomRectCorners.Add(o3);
                                bottomRectCorners.Add(o4);

                                Polyline topRect = new Polyline(topRectCorners);
                                Polyline bottomRect = new Polyline(bottomRectCorners);
                                Curve topRectCrv = topRect.ToNurbsCurve();
                                Curve bottomRectCrv = bottomRect.ToNurbsCurve();
                                Line crossLn = new Line(o0, n4);
                                Curve crossCrv = crossLn.ToNurbsCurve();

                                var sweep = new SweepOneRail();
                                sweep.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                                sweep.ClosedSweep = false;
                                sweep.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                                //myDoc.Objects.AddCurve(topRectCrv, orangeAttribute);
                                //myDoc.Views.Redraw();
                                //myDoc.Objects.AddCurve(bottomRectCrv, orangeAttribute);
                                //myDoc.Views.Redraw();
                                //myDoc.Objects.AddCurve(crossCrv, orangeAttribute);
                                //myDoc.Views.Redraw();

                                Brep[] tpBreps = sweep.PerformSweep(bottomRectCrv, crossCrv);
                                Brep tpBrep = tpBreps[0];
                                truncatedSocket = tpBrep.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);

                                // Test the direction of the brep
                                var faces = truncatedSocket.Faces;
                                Point3d cen = truncatedSocket.GetBoundingBox(true).Center;
                                double u, v;
                                faces[0].ClosestPoint(cen, out u, out v);
                                Point3d f_closet = faces[0].PointAt(u, v);

                                Vector3d f_normal = faces[0].NormalAt(u, v);
                                Vector3d f_test = cen - f_closet;

                                double dot_product = f_normal.X * f_test.X + f_normal.Y * f_test.Y + f_normal.Z * f_test.Z;
                                if (dot_product > 0)
                                {
                                    truncatedSocket.Flip();
                                }
                            }

                            // create the snap-fits if possible
                            double clearance = 0.2;
                            double fit_angle = 38;

                            Brep snapFit = null;
                            Brep secondSnapFit = null;

                            if((scktBoundingBox.GetCorners()[1].X - scktBoundingBox.GetCorners()[0].X) >= 5 || 
                                (scktBoundingBox.GetCorners()[2].Y - scktBoundingBox.GetCorners()[0].Y) >= 5)
                            {
                                if((scktBoundingBox.GetCorners()[1].X - scktBoundingBox.GetCorners()[0].X) >= 5 && 
                                    (scktBoundingBox.GetCorners()[2].Y - scktBoundingBox.GetCorners()[0].Y) >= 5)
                                {
                                    if ((scktBoundingBox.GetCorners()[1].X - scktBoundingBox.GetCorners()[0].X) >= (scktBoundingBox.GetCorners()[2].Y - scktBoundingBox.GetCorners()[0].Y) &&
                                        (scktBoundingBox.GetCorners()[2].Y - scktBoundingBox.GetCorners()[0].Y) >= (1.5 * 2 - 2 * clearance + 0.2) )
                                    {
                                        // add the snap-fits to the x side
                                        Vector3d x_neg_unit = (scktBoundingBox.GetCorners()[0] - scktBoundingBox.GetCorners()[1]) / scktBoundingBox.GetCorners()[0].DistanceTo(scktBoundingBox.GetCorners()[1]);
                                        Vector3d y_neg_unit = (scktBoundingBox.GetCorners()[0] - scktBoundingBox.GetCorners()[3]) / scktBoundingBox.GetCorners()[0].DistanceTo(scktBoundingBox.GetCorners()[3]);
                                        Vector3d z_pos_unit = (scktBoundingBox.GetCorners()[4] - scktBoundingBox.GetCorners()[0]) / scktBoundingBox.GetCorners()[4].DistanceTo(scktBoundingBox.GetCorners()[0]);
                                        Point3d snapFitCrvStartPt = (scktBoundingBox.GetCorners()[0] + scktBoundingBox.GetCorners()[1]) / 2 + x_neg_unit * 2.5 + y_neg_unit * clearance - z_pos_unit * 0.5;
                                        Point3d snapFitCrvEndPt = (scktBoundingBox.GetCorners()[0] + scktBoundingBox.GetCorners()[1]) / 2 - x_neg_unit * 2.5 + y_neg_unit * clearance - z_pos_unit * 0.5;
                                        Line snapFitOutlineTraj = new Line(snapFitCrvStartPt, snapFitCrvEndPt);
                                        Curve snapFitOutlineTrajCrv = snapFitOutlineTraj.ToNurbsCurve();


                                        double fit_height = 0;
                                        Point3d height_test_pt_start = (scktBoundingBox.GetCorners()[0] + scktBoundingBox.GetCorners()[1]) / 2 + x_neg_unit * 2.5 - z_pos_unit * 0.5 - y_neg_unit * 0.5;
                                        Mesh part_mesh_1 = (Mesh)myDoc.Objects.Find(cmPrt.PrtID).Geometry;
                                        Mesh part_mesh = part_mesh_1.DuplicateMesh();

                                        Transform roback1 = Transform.Rotation((-1) * cmPrt.RotationAngle / 180.0 * Math.PI, cmPrt.Normal, myDoc.Objects.Find(cmPrt.PrtID).Geometry.GetBoundingBox(true).Center);
                                        part_mesh.Transform(roback1);

                                        Vector3d reverseNormalVector1 = cmPrt.Normal * (-1);

                                        Transform resetTrans1 = Transform.Translation(reverseNormalVector1 * cmPrt.ExposureLevel * cmPrt.Height);
                                        part_mesh.Transform(resetTrans1);

                                        if (cmPrt.IsFlipped)
                                        {
                                            Transform flipTrans1 = Transform.Rotation(cmPrt.Normal, reverseNormalVector1, cmPrt.PartPos);
                                            part_mesh.Transform(flipTrans1);
                                        }
                                        part_mesh.Transform(cmPrt.TransformReverseSets.ElementAt(0));


                                        for (int c = 0; c <= 50; c++)
                                        {
                                            // test 50 sample points to get the estimated height of the snap fit

                                            Point3d temp = height_test_pt_start - x_neg_unit * 0.1 * c;
                                            Vector3d dir = new Vector3d(0, 0, 1);

                                            foreach(MeshFace mf in part_mesh.Faces)
                                            {
                                                double scalar = IntersectRay(temp, dir, mf, part_mesh);
                                                if(scalar >= fit_height)
                                                {
                                                    fit_height = scalar;
                                                }
                                            }
                                        }

                                        Point3d out0 = snapFitCrvStartPt;
                                        Point3d out1 = out0 + z_pos_unit * (clearance * 2 + fit_height);
                                        Point3d out2 = out1 + (-1) * y_neg_unit * 1.5;
                                        Point3d out3 = out2 + z_pos_unit * clearance;
                                        Point3d out4 = out1 + z_pos_unit * (clearance + 1.5 * Math.Tan((90-fit_angle)/180 * Math.PI));
                                        Point3d out5 = out4 + y_neg_unit * 1.5;
                                        Point3d out6 = out0 + y_neg_unit * 1.5;
                                        Point3d out7 = out0;

                                        List<Point3d> fitOutline = new List<Point3d>();
                                        fitOutline.Add(out0);
                                        fitOutline.Add(out1);
                                        fitOutline.Add(out2);
                                        fitOutline.Add(out3);
                                        fitOutline.Add(out4);
                                        fitOutline.Add(out5);
                                        fitOutline.Add(out6);
                                        fitOutline.Add(out7);

                                        Polyline fitPolyline = new Polyline(fitOutline);
                                        Curve fitPolylineCrv = fitPolyline.ToNurbsCurve();

                                        var sweep = new SweepOneRail();
                                        sweep.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                                        sweep.ClosedSweep = false;
                                        sweep.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                                        Brep[] fitOutlineBreps = sweep.PerformSweep(fitPolylineCrv, snapFitOutlineTrajCrv);
                                        Brep fitOutlineBrep = fitOutlineBreps[0];
                                        snapFit = fitOutlineBrep.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);

                                        // Test the direction of the brep
                                        var faces = snapFit.Faces;
                                        Point3d cen = snapFit.GetBoundingBox(true).Center;
                                        double u, v;
                                        faces[0].ClosestPoint(cen, out u, out v);
                                        Point3d f_closet = faces[0].PointAt(u, v);

                                        Vector3d f_normal = faces[0].NormalAt(u, v);
                                        Vector3d f_test = cen - f_closet;

                                        double dot_product = f_normal.X * f_test.X + f_normal.Y * f_test.Y + f_normal.Z * f_test.Z;
                                        if (dot_product > 0)
                                        {
                                            snapFit.Flip();
                                        }

                                        // Mirror the first snap-fit to generate the second snap-fit
                                        Transform mirrorFit = Transform.Mirror(scktBoundingBox.Center, y_neg_unit);
                                        secondSnapFit = snapFit.DuplicateBrep();
                                        secondSnapFit.Transform(mirrorFit);
                                        secondSnapFit.Flip();
                                    }
                                    else if((scktBoundingBox.GetCorners()[1].X - scktBoundingBox.GetCorners()[0].X) < (scktBoundingBox.GetCorners()[2].Y - scktBoundingBox.GetCorners()[0].Y) &&
                                        (scktBoundingBox.GetCorners()[1].X - scktBoundingBox.GetCorners()[0].X) >= (1.5 * 2 - 2 * clearance + 0.2))
                                    {
                                        // add the snap-fits to the y side
                                        Vector3d x_neg_unit = (scktBoundingBox.GetCorners()[0] - scktBoundingBox.GetCorners()[1]) / scktBoundingBox.GetCorners()[0].DistanceTo(scktBoundingBox.GetCorners()[1]);
                                        Vector3d y_neg_unit = (scktBoundingBox.GetCorners()[0] - scktBoundingBox.GetCorners()[3]) / scktBoundingBox.GetCorners()[0].DistanceTo(scktBoundingBox.GetCorners()[3]);
                                        Vector3d z_pos_unit = (scktBoundingBox.GetCorners()[4] - scktBoundingBox.GetCorners()[0]) / scktBoundingBox.GetCorners()[4].DistanceTo(scktBoundingBox.GetCorners()[0]);
                                        Point3d snapFitCrvStartPt = (scktBoundingBox.GetCorners()[0] + scktBoundingBox.GetCorners()[3]) / 2 + y_neg_unit * 2.5 + x_neg_unit * clearance - z_pos_unit * 0.5;
                                        Point3d snapFitCrvEndPt = (scktBoundingBox.GetCorners()[0] + scktBoundingBox.GetCorners()[3]) / 2 - y_neg_unit * 2.5 + x_neg_unit * clearance - z_pos_unit * 0.5;
                                        Line snapFitOutlineTraj = new Line(snapFitCrvStartPt, snapFitCrvEndPt);
                                        Curve snapFitOutlineTrajCrv = snapFitOutlineTraj.ToNurbsCurve();

                                        double fit_height = 0;
                                        Point3d height_test_pt_start = (scktBoundingBox.GetCorners()[0] + scktBoundingBox.GetCorners()[3]) / 2 + y_neg_unit * 2.5 - z_pos_unit * 0.5 - x_neg_unit * 0.5;
                                        Mesh part_mesh_1 = (Mesh)myDoc.Objects.Find(cmPrt.PrtID).Geometry;
                                        Mesh part_mesh = part_mesh_1.DuplicateMesh();

                                        Transform roback1 = Transform.Rotation((-1) * cmPrt.RotationAngle / 180.0 * Math.PI, cmPrt.Normal, myDoc.Objects.Find(cmPrt.PrtID).Geometry.GetBoundingBox(true).Center);
                                        part_mesh.Transform(roback1);

                                        Vector3d reverseNormalVector1 = cmPrt.Normal * (-1);

                                        Transform resetTrans1 = Transform.Translation(reverseNormalVector1 * cmPrt.ExposureLevel * cmPrt.Height);
                                        part_mesh.Transform(resetTrans1);

                                        if (cmPrt.IsFlipped)
                                        {
                                            Transform flipTrans1 = Transform.Rotation(cmPrt.Normal, reverseNormalVector1, cmPrt.PartPos);
                                            part_mesh.Transform(flipTrans1);
                                        }
                                        part_mesh.Transform(cmPrt.TransformReverseSets.ElementAt(0));

                                        //myDoc.Objects.AddMesh(part_mesh, orangeAttribute);
                                        //myDoc.Views.Redraw();

                                        for (int c = 0; c <= 50; c++)
                                        {
                                            // test 50 sample points to get the estimated height of the snap fit

                                            Point3d temp = height_test_pt_start - y_neg_unit * 0.1 * c;
                                            Vector3d dir = new Vector3d(0, 0, 1);

                                            //myDoc.Objects.AddPoint(temp, orangeAttribute);
                                            //myDoc.Views.Redraw();

                                            foreach (MeshFace mf in part_mesh.Faces)
                                            {
                                                double scalar = IntersectRay(temp, dir, mf, part_mesh);
                                                if (scalar >= fit_height)
                                                {
                                                    fit_height = scalar;
                                                }
                                            }
                                        }

                                        Point3d out0 = snapFitCrvStartPt;
                                        Point3d out1 = out0 + z_pos_unit * (clearance * 2 + fit_height);
                                        Point3d out2 = out1 + (-1) * x_neg_unit * 1.5;
                                        Point3d out3 = out2 + z_pos_unit * clearance;
                                        Point3d out4 = out1 + z_pos_unit * (clearance + 1.5 * Math.Tan((90 - fit_angle) / 180 * Math.PI));
                                        Point3d out5 = out4 + x_neg_unit * 1.5;
                                        Point3d out6 = out0 + x_neg_unit * 1.5;
                                        Point3d out7 = out0;

                                        List<Point3d> fitOutline = new List<Point3d>();
                                        fitOutline.Add(out0);
                                        fitOutline.Add(out1);
                                        fitOutline.Add(out2);
                                        fitOutline.Add(out3);
                                        fitOutline.Add(out4);
                                        fitOutline.Add(out5);
                                        fitOutline.Add(out6);
                                        fitOutline.Add(out7);

                                        Polyline fitPolyline = new Polyline(fitOutline);
                                        Curve fitPolylineCrv = fitPolyline.ToNurbsCurve();

                                        var sweep = new SweepOneRail();
                                        sweep.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                                        sweep.ClosedSweep = false;
                                        sweep.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                                        Brep[] fitOutlineBreps = sweep.PerformSweep(fitPolylineCrv, snapFitOutlineTrajCrv);
                                        Brep fitOutlineBrep = fitOutlineBreps[0];
                                        snapFit = fitOutlineBrep.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);

                                        // Test the direction of the brep
                                        var faces = snapFit.Faces;
                                        Point3d cen = snapFit.GetBoundingBox(true).Center;
                                        double u, v;
                                        faces[0].ClosestPoint(cen, out u, out v);
                                        Point3d f_closet = faces[0].PointAt(u, v);

                                        Vector3d f_normal = faces[0].NormalAt(u, v);
                                        Vector3d f_test = cen - f_closet;

                                        double dot_product = f_normal.X * f_test.X + f_normal.Y * f_test.Y + f_normal.Z * f_test.Z;
                                        if (dot_product > 0)
                                        {
                                            snapFit.Flip();
                                        }

                                        // Mirror the first snap-fit to generate the second snap-fit
                                        Transform mirrorFit = Transform.Mirror(scktBoundingBox.Center, x_neg_unit);
                                        secondSnapFit = snapFit.DuplicateBrep();
                                        secondSnapFit.Transform(mirrorFit);
                                        secondSnapFit.Flip();
                                    }


                                }
                                else if ((scktBoundingBox.GetCorners()[1].X - scktBoundingBox.GetCorners()[0].X) >= 5 &&
                                    (scktBoundingBox.GetCorners()[2].Y - scktBoundingBox.GetCorners()[0].Y) >= (1.5 * 2 - 2 * clearance + 0.2))
                                {
                                    // add the snap-fits to the x side
                                    Vector3d x_neg_unit = (scktBoundingBox.GetCorners()[0] - scktBoundingBox.GetCorners()[1]) / scktBoundingBox.GetCorners()[0].DistanceTo(scktBoundingBox.GetCorners()[1]);
                                    Vector3d y_neg_unit = (scktBoundingBox.GetCorners()[0] - scktBoundingBox.GetCorners()[3]) / scktBoundingBox.GetCorners()[0].DistanceTo(scktBoundingBox.GetCorners()[3]);
                                    Vector3d z_pos_unit = (scktBoundingBox.GetCorners()[4] - scktBoundingBox.GetCorners()[0]) / scktBoundingBox.GetCorners()[4].DistanceTo(scktBoundingBox.GetCorners()[0]);
                                    Point3d snapFitCrvStartPt = (scktBoundingBox.GetCorners()[0] + scktBoundingBox.GetCorners()[1]) / 2 + x_neg_unit * 2.5 + y_neg_unit * clearance - z_pos_unit * 0.5;
                                    Point3d snapFitCrvEndPt = (scktBoundingBox.GetCorners()[0] + scktBoundingBox.GetCorners()[1]) / 2 - x_neg_unit * 2.5 + y_neg_unit * clearance - z_pos_unit * 0.5;
                                    Line snapFitOutlineTraj = new Line(snapFitCrvStartPt, snapFitCrvEndPt);
                                    Curve snapFitOutlineTrajCrv = snapFitOutlineTraj.ToNurbsCurve();

                                    double fit_height = 0;
                                    Point3d height_test_pt_start = (scktBoundingBox.GetCorners()[0] + scktBoundingBox.GetCorners()[1]) / 2 + x_neg_unit * 2.5 - z_pos_unit * 0.5 - y_neg_unit * 0.5;
                                    Mesh part_mesh_1 = (Mesh)myDoc.Objects.Find(cmPrt.PrtID).Geometry;
                                    Mesh part_mesh = part_mesh_1.DuplicateMesh();

                                    Transform roback1 = Transform.Rotation((-1) * cmPrt.RotationAngle / 180.0 * Math.PI, cmPrt.Normal, myDoc.Objects.Find(cmPrt.PrtID).Geometry.GetBoundingBox(true).Center);
                                    part_mesh.Transform(roback1);

                                    Vector3d reverseNormalVector1 = cmPrt.Normal * (-1);

                                    Transform resetTrans1 = Transform.Translation(reverseNormalVector1 * cmPrt.ExposureLevel * cmPrt.Height);
                                    part_mesh.Transform(resetTrans1);

                                    if (cmPrt.IsFlipped)
                                    {
                                        Transform flipTrans1 = Transform.Rotation(cmPrt.Normal, reverseNormalVector1, cmPrt.PartPos);
                                        part_mesh.Transform(flipTrans1);
                                    }
                                    part_mesh.Transform(cmPrt.TransformReverseSets.ElementAt(0));

                                    for (int c = 0; c <= 50; c++)
                                    {
                                        // test 50 sample points to get the estimated height of the snap fit

                                        Point3d temp = height_test_pt_start - x_neg_unit * 0.1 * c;
                                        Vector3d dir = new Vector3d(0, 0, 1);

                                        foreach (MeshFace mf in part_mesh.Faces)
                                        {
                                            double scalar = IntersectRay(temp, dir, mf, part_mesh);
                                            if (scalar >= fit_height)
                                            {
                                                fit_height = scalar;
                                            }
                                        }
                                    }

                                    Point3d out0 = snapFitCrvStartPt;
                                    Point3d out1 = out0 + z_pos_unit * (clearance * 2 + fit_height);
                                    Point3d out2 = out1 + (-1) * y_neg_unit * 1.5;
                                    Point3d out3 = out2 + z_pos_unit * clearance;
                                    Point3d out4 = out1 + z_pos_unit * (clearance + 1.5 * Math.Tan((90 - fit_angle) / 180 * Math.PI));
                                    Point3d out5 = out4 + y_neg_unit * 1.5;
                                    Point3d out6 = out0 + y_neg_unit * 1.5;
                                    Point3d out7 = out0;

                                    List<Point3d> fitOutline = new List<Point3d>();
                                    fitOutline.Add(out0);
                                    fitOutline.Add(out1);
                                    fitOutline.Add(out2);
                                    fitOutline.Add(out3);
                                    fitOutline.Add(out4);
                                    fitOutline.Add(out5);
                                    fitOutline.Add(out6);
                                    fitOutline.Add(out7);

                                    Polyline fitPolyline = new Polyline(fitOutline);
                                    Curve fitPolylineCrv = fitPolyline.ToNurbsCurve();

                                    var sweep = new SweepOneRail();
                                    sweep.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                                    sweep.ClosedSweep = false;
                                    sweep.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                                    Brep[] fitOutlineBreps = sweep.PerformSweep(fitPolylineCrv, snapFitOutlineTrajCrv);
                                    Brep fitOutlineBrep = fitOutlineBreps[0];
                                    snapFit = fitOutlineBrep.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);

                                    // Test the direction of the brep
                                    var faces = snapFit.Faces;
                                    Point3d cen = snapFit.GetBoundingBox(true).Center;
                                    double u, v;
                                    faces[0].ClosestPoint(cen, out u, out v);
                                    Point3d f_closet = faces[0].PointAt(u, v);

                                    Vector3d f_normal = faces[0].NormalAt(u, v);
                                    Vector3d f_test = cen - f_closet;

                                    double dot_product = f_normal.X * f_test.X + f_normal.Y * f_test.Y + f_normal.Z * f_test.Z;
                                    if (dot_product > 0)
                                    {
                                        snapFit.Flip();
                                    }

                                    // Mirror the first snap-fit to generate the second snap-fit
                                    Transform mirrorFit = Transform.Mirror(scktBoundingBox.Center, y_neg_unit);
                                    secondSnapFit = snapFit.DuplicateBrep();
                                    secondSnapFit.Transform(mirrorFit);
                                    secondSnapFit.Flip();

                                }
                                else if((scktBoundingBox.GetCorners()[2].Y - scktBoundingBox.GetCorners()[0].Y) >= 5 &&
                                    (scktBoundingBox.GetCorners()[1].X - scktBoundingBox.GetCorners()[0].X) >= (1.5 * 2 - 2 * clearance + 0.2))
                                {
                                    // add the snap-fits to the y side
                                    Vector3d x_neg_unit = (scktBoundingBox.GetCorners()[0] - scktBoundingBox.GetCorners()[1]) / scktBoundingBox.GetCorners()[0].DistanceTo(scktBoundingBox.GetCorners()[1]);
                                    Vector3d y_neg_unit = (scktBoundingBox.GetCorners()[0] - scktBoundingBox.GetCorners()[3]) / scktBoundingBox.GetCorners()[0].DistanceTo(scktBoundingBox.GetCorners()[3]);
                                    Vector3d z_pos_unit = (scktBoundingBox.GetCorners()[4] - scktBoundingBox.GetCorners()[0]) / scktBoundingBox.GetCorners()[4].DistanceTo(scktBoundingBox.GetCorners()[0]);
                                    Point3d snapFitCrvStartPt = (scktBoundingBox.GetCorners()[0] + scktBoundingBox.GetCorners()[3]) / 2 + y_neg_unit * 2.5 + x_neg_unit * clearance - z_pos_unit * 0.5;
                                    Point3d snapFitCrvEndPt = (scktBoundingBox.GetCorners()[0] + scktBoundingBox.GetCorners()[3]) / 2 - y_neg_unit * 2.5 + x_neg_unit * clearance - z_pos_unit * 0.5;
                                    Line snapFitOutlineTraj = new Line(snapFitCrvStartPt, snapFitCrvEndPt);
                                    Curve snapFitOutlineTrajCrv = snapFitOutlineTraj.ToNurbsCurve();

                                    double fit_height = 0;
                                    Point3d height_test_pt_start = (scktBoundingBox.GetCorners()[0] + scktBoundingBox.GetCorners()[3]) / 2 + y_neg_unit * 2.5 - z_pos_unit * 0.5 - x_neg_unit * 0.5;
                                    Mesh part_mesh_1 = (Mesh)myDoc.Objects.Find(cmPrt.PrtID).Geometry;
                                    Mesh part_mesh = part_mesh_1.DuplicateMesh();

                                    Transform roback1 = Transform.Rotation((-1) * cmPrt.RotationAngle / 180.0 * Math.PI, cmPrt.Normal, myDoc.Objects.Find(cmPrt.PrtID).Geometry.GetBoundingBox(true).Center);
                                    part_mesh.Transform(roback1);

                                    Vector3d reverseNormalVector1 = cmPrt.Normal * (-1);

                                    Transform resetTrans1 = Transform.Translation(reverseNormalVector1 * cmPrt.ExposureLevel * cmPrt.Height);
                                    part_mesh.Transform(resetTrans1);

                                    if (cmPrt.IsFlipped)
                                    {
                                        Transform flipTrans1 = Transform.Rotation(cmPrt.Normal, reverseNormalVector1, cmPrt.PartPos);
                                        part_mesh.Transform(flipTrans1);
                                    }
                                    part_mesh.Transform(cmPrt.TransformReverseSets.ElementAt(0));

                                    for (int c = 0; c <= 50; c++)
                                    {
                                        // test 50 sample points to get the estimated height of the snap fit

                                        Point3d temp = height_test_pt_start - y_neg_unit * 0.1 * c;
                                        Vector3d dir = new Vector3d(0, 0, 1);

                                        foreach (MeshFace mf in part_mesh.Faces)
                                        {
                                            double scalar = IntersectRay(temp, dir, mf, part_mesh);
                                            if (scalar >= fit_height)
                                            {
                                                fit_height = scalar;
                                            }
                                        }
                                    }

                                    Point3d out0 = snapFitCrvStartPt;
                                    Point3d out1 = out0 + z_pos_unit * (clearance * 2 + fit_height);
                                    Point3d out2 = out1 + (-1) * x_neg_unit * 1.5;
                                    Point3d out3 = out2 + z_pos_unit * clearance;
                                    Point3d out4 = out1 + z_pos_unit * (clearance + 1.5 * Math.Tan((90 - fit_angle) / 180 * Math.PI));
                                    Point3d out5 = out4 + x_neg_unit * 1.5;
                                    Point3d out6 = out0 + x_neg_unit * 1.5;
                                    Point3d out7 = out0;

                                    List<Point3d> fitOutline = new List<Point3d>();
                                    fitOutline.Add(out0);
                                    fitOutline.Add(out1);
                                    fitOutline.Add(out2);
                                    fitOutline.Add(out3);
                                    fitOutline.Add(out4);
                                    fitOutline.Add(out5);
                                    fitOutline.Add(out6);
                                    fitOutline.Add(out7);

                                    Polyline fitPolyline = new Polyline(fitOutline);
                                    Curve fitPolylineCrv = fitPolyline.ToNurbsCurve();

                                    var sweep = new SweepOneRail();
                                    sweep.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                                    sweep.ClosedSweep = false;
                                    sweep.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                                    Brep[] fitOutlineBreps = sweep.PerformSweep(fitPolylineCrv, snapFitOutlineTrajCrv);
                                    Brep fitOutlineBrep = fitOutlineBreps[0];
                                    snapFit = fitOutlineBrep.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);

                                    // Test the direction of the brep
                                    var faces = snapFit.Faces;
                                    Point3d cen = snapFit.GetBoundingBox(true).Center;
                                    double u, v;
                                    faces[0].ClosestPoint(cen, out u, out v);
                                    Point3d f_closet = faces[0].PointAt(u, v);

                                    Vector3d f_normal = faces[0].NormalAt(u, v);
                                    Vector3d f_test = cen - f_closet;

                                    double dot_product = f_normal.X * f_test.X + f_normal.Y * f_test.Y + f_normal.Z * f_test.Z;
                                    if (dot_product > 0)
                                    {
                                        snapFit.Flip();
                                    }

                                    // Mirror the first snap-fit to generate the second snap-fit
                                    Transform mirrorFit = Transform.Mirror(scktBoundingBox.Center, x_neg_unit);
                                    secondSnapFit = snapFit.DuplicateBrep();
                                    secondSnapFit.Transform(mirrorFit);
                                    secondSnapFit.Flip();
                                }
                            }

                            #endregion

                            #region Third, rotate the generated tuncated socket to the final orientation

                            truncatedSocket.Transform(p.TransformSets.ElementAt(1));

                            if(snapFit != null && secondSnapFit != null)
                            {
                                snapFit.Transform(p.TransformSets.ElementAt(1));
                                secondSnapFit.Transform(p.TransformSets.ElementAt(1));
                            }
                            
                            #endregion

                            #region Finally, flip and rotate the socket part to the correct orientation

                            if (p.IsFlipped)
                            {
                                Transform flipTrans = Transform.Rotation(p.Normal, reverseNormalVector, p.PartPos);

                                truncatedSocket.Transform(flipTrans);
                            }

                            Transform setExpTrans = Transform.Translation(p.Normal * p.ExposureLevel * p.Height);
                            truncatedSocket.Transform(setExpTrans);

                            if (snapFit != null && secondSnapFit != null)
                            {
                                snapFit.Transform(setExpTrans);
                                secondSnapFit.Transform(setExpTrans);
                            }

                            Transform rotateAgain = Transform.Rotation(p.RotationAngle / 180.0 * Math.PI, p.Normal, p.PartPos);
                            truncatedSocket.Transform(rotateAgain);

                            if (snapFit != null && secondSnapFit != null)
                            {
                                snapFit.Transform(rotateAgain);
                                secondSnapFit.Transform(rotateAgain);
                            }

                            #endregion

                            finalSocketBreps.Add(truncatedSocket);

                            if (snapFit != null && secondSnapFit != null)
                            {
                                snapFitBreps.Add(snapFit);
                                snapFitBreps.Add(secondSnapFit);
                            }

                            //if(snapFit != null) {
                            //    myDoc.Objects.AddBrep(snapFit, blueAttribute);
                            //}
                            //if(secondSnapFit != null)
                            //{
                            //    myDoc.Objects.AddBrep(secondSnapFit, blueAttribute);
                            //}

                            //myDoc.Views.Redraw();
                        }
                    }

                    List<Brep> bodyToDiff = new List<Brep>();

                    Brep bodyDupBrep = ((Brep)myDoc.Objects.Find(currBody.objID).Geometry).DuplicateBrep();
                    #region move the original body to a new layer called "OriginalBody"
                    if (myDoc.Layers.FindName("OriginalBody") == null)
                    {
                        // create a new layer named "OriginalBody"
                        string layer_name = "OriginalBody";
                        Layer p_ly = myDoc.Layers.CurrentLayer;

                        Layer new_ly = new Layer();
                        new_ly.Name = layer_name;
                        new_ly.ParentLayerId = p_ly.ParentLayerId;
                        new_ly.Color = Color.FromArgb(0, 0, 0);

                        int index = myDoc.Layers.Add(new_ly);
                        blueAttribute.LayerIndex = index;
                        myDoc.Layers.ElementAt(index).IsVisible = false;
                        //myDoc.Layers.SetCurrentLayerIndex(index, true);

                        myDoc.Objects.Find(currBody.objID).Attributes.LayerIndex = index;

                        if (index < 0) return;
                    }
                    else
                    {
                        int index = myDoc.Layers.FindName("OriginalBody").Index;
                        myDoc.Objects.Find(currBody.objID).Attributes.LayerIndex = index;
                        myDoc.Layers.ElementAt(index).IsVisible = false;
                    }
                    #endregion
                    myDoc.Objects.Hide(currBody.objID, true);

                    //bodyToDiff.Add(bodyDupBrep);

                    //foreach (Brep b in finalSocketBreps)
                    //{
                    //    myDoc.Objects.AddBrep(b, blueAttribute);
                    //    myDoc.Views.Redraw();
                    //}

                    #region create a new layer to store the converted main body
                    if (myDoc.Layers.FindName("ConvertedBody") == null)
                    {
                        // create a new layer named "Parts"
                        string layer_name = "ConvertedBody";
                        Layer p_ly = myDoc.Layers.CurrentLayer;

                        Layer new_ly = new Layer();
                        new_ly.Name = layer_name;
                        new_ly.ParentLayerId = p_ly.ParentLayerId;
                        new_ly.Color = Color.FromArgb(16, 150, 206);

                        int index = myDoc.Layers.Add(new_ly);
                        blueAttribute.LayerIndex = index;
                        myDoc.Layers.SetCurrentLayerIndex(index, true);

                        if (index < 0) return;
                    }
                    else
                    {
                        int index = myDoc.Layers.FindName("ConvertedBody").Index;
                        myDoc.Layers.SetCurrentLayerIndex(index, true);
                    }
                    #endregion

                    #region generate the base for all the components if they are outside of the main body

                    List<Brep> bases = new List<Brep>(); // used for storing all the possible bases for the components
                    foreach(Part3D p3d in dataset.part3DList)
                    {
                        bool isPinOutside = false;
                        bool isCorrectOrientation = true;

                        foreach (Pin p in p3d.Pins)
                        {
                            if (bodyDupBrep.IsPointInside(p.Pos, myDoc.ModelAbsoluteTolerance, false))
                                continue;
                            else
                            {
                                // test if the pins are farther than the part center from the brep body
                                
                                if(currBody.bodyBrep.ClosestPoint(p3d.PartPos).DistanceTo(p3d.PartPos) <= currBody.bodyBrep.ClosestPoint(p.Pos).DistanceTo(p.Pos))
                                {
                                    isCorrectOrientation = false;
                                    string error = p3d.PartName + "has pins outside of the model body. Please flip " + p3d.PartName + ".";
                                    errors.Add(error);
                                }

                                isPinOutside = true;
                                break;
                            }
                        }

                        if (isPinOutside && isCorrectOrientation)
                        {
                            // generate the base and add it to the bases list
                            partSocket psoc = currBody.partSockets.Find(x => (x.PrtName.Equals(p3d.PartName)));
                            Brep psocBrep = psoc.SocketBrep;
                            Brep[] outBlends;
                            Brep[] outWalls;
                            Brep[] dupOffsetBody = Brep.CreateOffsetBrep(psocBrep, 0.4, false, true, myDoc.ModelRelativeTolerance, out outBlends, out outWalls);

                            Brep baseBrep = dupOffsetBody[0];

                            Transform tranOffset = Transform.Translation(p3d.Normal * ((-1) * p3d.Height * p3d.ExposureLevel + 0.4));
                            baseBrep.Transform(tranOffset);

                            bases.Add(baseBrep);
                        }
                    }

                    #endregion
                    bases.Add(bodyDupBrep);

                    var newMainBody = Brep.CreateBooleanUnion(bases, myDoc.ModelAbsoluteTolerance);
                    if (newMainBody.Length > 0)
                    {
                        bodyToDiff.Add(newMainBody[0]);
                    }
                    else
                    {
                        return;
                    }

                    Guid finalBodyID = Guid.Empty;
                    if (bodyToDiff.Count > 0)
                    {
                        var bodyWithSockets = Brep.CreateBooleanDifference(bodyToDiff, finalSocketBreps, myDoc.ModelAbsoluteTolerance);

                        
                        if (bodyWithSockets.Length > 0)
                        {
                            List<Brep> snapfits_final = new List<Brep>();
                            snapfits_final.Add(bodyWithSockets[0]);

                            if (snapFitBreps.Count != 0)
                            {
                                // Boolean different all the snap-fits with the external electroinc parts
                                List<Brep> partBrepDups = new List<Brep>();
                                foreach(Part3D prt3D in dataset.part3DList)
                                {
                                    Mesh part_mesh = (Mesh)myDoc.Objects.Find(prt3D.PrtID).Geometry;
                                    Brep part_brep = Brep.CreateFromMesh(part_mesh, false);

                                    //Brep[] innerShells;
                                    //Brep[] innerWalls;

                                    //Brep[] shells = Brep.CreateOffsetBrep(part_brep, 0.2, false, true, myDoc.ModelRelativeTolerance, out innerShells, out innerWalls);
                                    //if(shells != null && shells.Length > 0)
                                    //    partBrepDups.Add(shells[0].DuplicateBrep());
                                    //else
                                    Brep part_brep_dup = part_brep.DuplicateBrep();
      
                                    partBrepDups.Add(part_brep_dup);
                                }
                                var snapfits = Brep.CreateBooleanDifference(snapFitBreps, partBrepDups, myDoc.ModelAbsoluteTolerance);
                                
                                if(snapfits != null && snapfits.Length > 0)
                                {
                                    foreach(Brep sf in snapfits)
                                    {
                                        // add all the processed snap-fits into the array for final boolean union
                                        snapfits_final.Add(sf);
                                    }
                                }
                                else
                                {
                                    foreach (Brep sf in snapFitBreps)
                                    {
                                        // add all the processed snap-fits into the array for final boolean union
                                        snapfits_final.Add(sf);
                                    }
                                }
                            }

                            
                            var convertedBodyFinal = Brep.CreateBooleanUnion(snapfits_final, myDoc.ModelAbsoluteTolerance);
                            if(convertedBodyFinal != null && convertedBodyFinal.Length > 0)
                            {
                                finalBodyID = myDoc.Objects.AddBrep(convertedBodyFinal[0], blueAttribute);
                                if (!currBody.convertedObjID.Equals(Guid.Empty))
                                    myDoc.Objects.Delete(currBody.convertedObjID, true);
                                currBody.convertedObjID = finalBodyID;
                                myDoc.Views.Redraw();
                            }
                            else
                            {
                                return;
                            }  
                        }
                        else
                        {
                            return;
                        }

                        //foreach(Brep b in bodyWithSockets)
                        //{
                        //    myDoc.Objects.AddBrep(b, blueAttribute);
                        //    myDoc.Views.Redraw();
                        //}
                    }

                    #endregion

                    #region Step 2: prepare the voxels for trace generation 

                    Brep convertedBody = (Brep)myDoc.Objects.Find(finalBodyID).Geometry;

                    #region First, cover the body brep into a mesh

                    var minimal = MeshingParameters.Minimal;
                    var meshes = Mesh.CreateFromBrep(convertedBody, minimal);
                    if (meshes == null || meshes.Length == 0)
                        return;

                    var brep_mesh = new Mesh(); // the mesh of the currently selected body

                    foreach (var mesh in meshes)
                        brep_mesh.Append(mesh);
                    brep_mesh.Faces.ConvertQuadsToTriangles();

                    #endregion

                    #region Second, voxelize the body (without the already generated traces and gaps) and find the points inside the body 

                    prcWin.Show();
                    ObjRef objSel_ref = new ObjRef(finalBodyID);
                    List<Point3d> bodyPts = new List<Point3d>();
                    List<Point3d> allPts = new List<Point3d>();
                    List<Point3d> surfPts = new List<Point3d>();

                    currBody.allPts.Clear();
                    currBody.bodyPts.Clear();
                    currBody.surfPts.Clear();
                    currBody.forRtPts.Clear();

                    Voxelize(objSel_ref, out bodyPts, out surfPts, out allPts);

                    foreach (Point3d pt in allPts)
                    {
                        currBody.allPts.Add(pt);
                    }

                    foreach (Point3d pt in bodyPts)
                    {
                        currBody.bodyPts.Add(pt);
                    }

                    foreach (Point3d pt in surfPts)
                    {
                        currBody.surfPts.Add(pt);
                    }

                    double offsetDis = Double.Parse(printMeta.Trace_min_x.Find(x => x.Cond.Equals("minimized resistance")).Value) / 1000;   // it is currently 0.8mm, but free to be any other value

                    Brep seleBrep = objSel_ref.Brep();

                    foreach (Point3d p in currBody.allPts)
                    {
                        if (seleBrep.ClosestPoint(p).DistanceTo(p) >= offsetDis/2)
                        {
                            currBody.forRtPts.Add(p);
                        }
                    }

                    prcWin.Hide();

                    #endregion

                    #endregion

                    #region Step 3: obtain all the trace clusters, including the cluster center and all the connected point positions
                    prcWin.Show();
                    if (dataset.deployedTraces.Count != 0)
                    {

                        // clean all the deployed traces that come from the circuit schematic
                        List<int> toRemoveIdx = new List<int>();
                        foreach(Trace tr in dataset.deployedTraces)
                        {
                            if(tr.Type == 1)
                            {
                                int idx = dataset.deployedTraces.IndexOf(tr);
                                toRemoveIdx.Add(idx); 
                            }
                        }

                        toRemoveIdx.OrderByDescending(i => i);

                        foreach(int i in toRemoveIdx)
                        {
                            dataset.deployedTraces.RemoveAt(i);
                        }
                    }

                    //if (dataset.traceOracle.Count != 0)
                    //    dataset.traceOracle.Clear();

                    if (dataset.traceOracle.Count > 0)
                    {
                        // connection information has been added to the data set
                        foreach (TraceCluster trC in dataset.traceOracle)
                        {
                            foreach (JumpWire jwOld in trC.Jumpwires)
                            {

                                myDoc.Objects.Delete(jwOld.CrvID, true);

                            }
                        }
                        myDoc.Views.Redraw();

                        dataset.traceOracle.Clear();

                        #region create a new layer if not exist to store all jump wires

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

                            myDoc.Layers.SetCurrentLayerIndex(index, true);
                            redAttribute.LayerIndex = index;

                            if (index < 0) return;
                        }
                        else
                        {
                            int index = myDoc.Layers.FindName("JumpWires").Index;
                            myDoc.Layers.SetCurrentLayerIndex(index, true);
                            redAttribute.LayerIndex = index;
                        }

                        #endregion

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

                    }
                    else
                    {
                        #region create a new layer if not exist to store all jump wires

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

                            myDoc.Layers.SetCurrentLayerIndex(index, true);
                            redAttribute.LayerIndex = index;

                            if (index < 0) return;
                        }
                        else
                        {
                            int index = myDoc.Layers.FindName("JumpWires").Index;
                            myDoc.Layers.SetCurrentLayerIndex(index, true);
                            redAttribute.LayerIndex = index;
                        }

                        #endregion

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
                    }

                    #region old code commented
                    //foreach (ConnectionNet conNet in dataset.connectionNetList)
                    //{
                    //    TraceCluster temp = new TraceCluster();

                    //    Point3d cc = new Point3d(0, 0, 0);

                    //    foreach (PartPinPair ppp in conNet.PartPinPairs)
                    //    {
                    //        // Obtain the pin's position in 3D
                    //        Point3d pinPt = GetPinPointByNames(ppp.PartName, ppp.PinName);

                    //        TraceClusterMember tcTemp = new TraceClusterMember();
                    //        tcTemp.PartName = ppp.PartName;
                    //        tcTemp.PinName = ppp.PinName;
                    //        tcTemp.Pos = pinPt;

                    //        cc += pinPt;
                    //        temp.ClusterMembers.Add(tcTemp);

                    //        //myDoc.Objects.AddPoint(tcTemp.Pos, redAttribute);
                    //        //myDoc.Views.Redraw();
                    //    }

                    //    cc = cc / conNet.PartPinPairs.Count;

                    //    //myDoc.Objects.AddPoint(cc, redAttribute);
                    //    //myDoc.Views.Redraw();

                    //    temp.ClusterCenter = cc;

                    //    dataset.traceOracle.Add(temp);
                    //}
                    #endregion

                    prcWin.Hide();
                    #endregion

                    #region Step 4: check if all the direct straight traces are in the model and not overlapping

                    prcWin.Show();
                    List<Trace> unSettledTraces = new List<Trace>();

                    foreach (TraceCluster trcC in dataset.traceOracle)
                    {
                        foreach (TraceClusterMember trcCM in trcC.ClusterMembers)
                        {
                            // Get the closest point of the part's pin in the point cloud that is on the normal direction of the part for routing
                            //Point3d prtPinPos = GetClosestFromPointCloud(trcCM.Pos, currBody.forRtPts);
                            Vector3d dir = dataset.part3DList.Find(x => x.PartName.Substring(x.PartName.IndexOf('-') + 1, x.PartName.Length - x.PartName.IndexOf('-') - 1).Equals(trcCM.PartName)).Normal;
                            Part3D cmPrt = dataset.part3DList.Find(x => x.PartName.Substring(x.PartName.IndexOf('-') + 1, x.PartName.Length - x.PartName.IndexOf('-') - 1).Equals(trcCM.PartName));
                            Point3d extStart0 = trcCM.Pos - extLen * dir;
                            Vector3d pt_dir = trcCM.Pos - cmPrt.PartPos;
                            Plane prt_bottom_plane = new Plane(extStart0, dir);
                            Point3d pt_dir_proj = prt_bottom_plane.ClosestPoint(extStart0 + pt_dir);
                            Vector3d pt_dir_inplane = pt_dir_proj - extStart0;
                            Point3d extStart1;
                            if(pt_dir_inplane.Length == 0)
                                extStart1 = extStart0; 
                            else
                                extStart1 = pt_dir_inplane / pt_dir_inplane.Length * extLen + extStart0;

                            Point3d prtPinPos = GetClosestFromPointCloud(extStart1, currBody.forRtPts, new Vector3d(extStart1 - trcCM.Pos));

                            Line tempLn = new Line(prtPinPos, trcC.ClusterCenter);
                            Curve tempCrv = tempLn.ToNurbsCurve();

                            if (CurveInModel(tempCrv, (Brep)myDoc.Objects.Find(finalBodyID).Geometry,tolerance))
                            {
                                // remember the trace is the free wire. The real wire is: trcCM.Pos (contact box) -> extStart1 -> prtPinPos -> trcC.ClusterCenter
                                Trace tempTrc = new Trace();
                                tempTrc.PrtName = trcCM.PartName;
                                tempTrc.PrtPinName = trcCM.PinName;
                                tempTrc.PrtAPos = trcCM.Pos;
                                //tempTrc.PrtBPos = trcC.ClusterCenter;
                                tempTrc.PrtBPos = GetClosestFromPointCloudSimple(trcC.ClusterCenter, currBody.forRtPts);
                                tempTrc.Type = 1;

                                // As now, we don't capture the destination part and pin

                                dataset.deployedTraces.Add(tempTrc);
                            }
                            else
                            {
                                // remember the trace is the free wire. The real wire is: trcCM.Pos (contact box) -> extStart1 -> prtPinPos -> trcC.ClusterCenter
                                Trace tempTrc = new Trace();
                                tempTrc.PrtName = trcCM.PartName;
                                tempTrc.PrtPinName = trcCM.PinName;
                                tempTrc.PrtAPos = trcCM.Pos;
                                //tempTrc.PrtBPos = trcC.ClusterCenter;
                                tempTrc.PrtBPos = GetClosestFromPointCloudSimple(trcC.ClusterCenter, currBody.forRtPts);
                                tempTrc.Type = 1;

                                // As now, we don't capture the destination part and pin

                                unSettledTraces.Add(tempTrc);
                            }

                        }
                    }

                    List<int> posToRemove = new List<int>();

                    for (int i = 0; i < dataset.deployedTraces.Count; i++)
                    {

                        for (int j = i + 1; j < dataset.deployedTraces.Count; ++j)
                        {
                            // currently, all the traces are straight pipes

                            if (PipeIsIntersecting(dataset.deployedTraces.ElementAt(i), dataset.deployedTraces.ElementAt(j), (2* radius + traceSpacing / 2) * 2))
                            {
                                // two pipes are intersecting with each other, one pipe should be moved to the unSettledTraces list
                                // move the first pipe to the unSettledTraces list and stop the loop

                                if(posToRemove.IndexOf(i) == -1)
                                {
                                    posToRemove.Add(i);

                                    Trace temp = dataset.deployedTraces.ElementAt(i);
                                    unSettledTraces.Add(temp);
                                }
                                

                                if(posToRemove.IndexOf(j) == -1)
                                {
                                    posToRemove.Add(j);

                                    Trace temp = dataset.deployedTraces.ElementAt(j);
                                    unSettledTraces.Add(temp);
                                }
                                
                            }
                        }
                    }

                    posToRemove.Sort((a, b) => b.CompareTo(a));
                    for (int t = 0; t < posToRemove.Count; t++)
                    {
                        dataset.deployedTraces.RemoveAt(posToRemove.ElementAt(t));
                    }

                    prcWin.Hide();
                    #endregion

                    #region Step 5: generate the traces using deployedTraces list (straight pipes) in dataset and the unSettledTraces list (polyline pipes)

                    #region create a new layer if not exist to store all generated traces

                    if (myDoc.Layers.FindName("Traces") == null)
                    {
                        // create a new layer named "JumpWires"
                        string layer_name = "Traces";
                        Layer p_ly = myDoc.Layers.CurrentLayer;

                        Layer new_ly = new Layer();
                        new_ly.Name = layer_name;
                        new_ly.ParentLayerId = p_ly.ParentLayerId;
                        new_ly.Color = Color.Red;

                        int index = myDoc.Layers.Add(new_ly);

                        myDoc.Layers.SetCurrentLayerIndex(index, true);
                        orangeAttribute.LayerIndex = index;

                        if (index < 0) return;
                    }
                    else
                    {
                        int index = myDoc.Layers.FindName("Traces").Index;
                        myDoc.Layers.SetCurrentLayerIndex(index, true);
                        orangeAttribute.LayerIndex = index;
                    }

                    #endregion

                    #region First, generate the stragiht pipes, which are stored in deployedTraces list

                    foreach (Trace s_trc in dataset.deployedTraces)
                    {
                        // The real wire is: s_trc.PrtAPos -> extStart1 -> fromPt -> toPt (s_trc.PrtBPos)
                        Vector3d dir = dataset.part3DList.Find(x => x.PartName.Substring(x.PartName.IndexOf('-') + 1, x.PartName.Length - x.PartName.IndexOf('-') - 1).Equals(s_trc.PrtName)).Normal;
                        Part3D cmPrt = dataset.part3DList.Find(x => x.PartName.Substring(x.PartName.IndexOf('-') + 1, x.PartName.Length - x.PartName.IndexOf('-') - 1).Equals(s_trc.PrtName));
                        Point3d extStart0 = s_trc.PrtAPos - extLen * dir;
                        Vector3d pt_dir = s_trc.PrtAPos - cmPrt.PartPos;
                        Plane prt_bottom_plane = new Plane(extStart0, dir);
                        Point3d pt_dir_proj = prt_bottom_plane.ClosestPoint(extStart0 + pt_dir);
                        Vector3d pt_dir_inplane = pt_dir_proj - extStart0;
                        Point3d extStart1;
                        if (pt_dir_inplane.Length == 0)
                            extStart1 = extStart0;
                        else
                            extStart1 = pt_dir_inplane / pt_dir_inplane.Length * extLen + extStart0;

                        Point3d fromPt = GetClosestFromPointCloud(extStart1, currBody.forRtPts, new Vector3d(extStart1 - s_trc.PrtAPos));
                        Point3d toPt = s_trc.PrtBPos;  // this point is always inside the body and no need to get the cloeset point from the cloud

                        #region create the straight pipe and remove those points that are close to the generated pipe from the routing candidates

                        #region create the straight pipe

                        Brep pipeS = new Brep();
                        if (toPt.DistanceTo(fromPt) <= 0.8)
                        {
                            Point3d c = (fromPt + toPt) / 2;
                            Sphere s = new Sphere(c, fromPt.DistanceTo(c));
                            pipeS = s.ToBrep();
                        }
                        else
                        {
                            Line lnS = new Line(fromPt, toPt);
                            Curve crvS = lnS.ToNurbsCurve();
                            pipeS = Brep.CreatePipe(crvS, radius, false, PipeCapMode.Round, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians)[0];
                        }

                        #endregion

                        #region create the contact box for the electrical component pad
                        Point3d boxC_init = (extStart0 + extStart1) / 2;
                        Point3d boxC = (boxC_init + extStart0) / 2;
                        Vector3d boxDir = s_trc.PrtAPos - extStart0;
                        Line ln1 = new Line(boxC + boxDir, boxC);
                        Curve crv1 = ln1.ToNurbsCurve();

                        //Vector3d xp = (extStart0 - boxC)/(extStart0.DistanceTo(boxC)) * (extStart0.DistanceTo(boxC) + radius / 2);
                        Vector3d xp = (extStart0 - boxC) / extStart0.DistanceTo(boxC) * (extStart0.DistanceTo(boxC));
                        Transform box_rot = Transform.Rotation(Math.PI / 2, boxDir, boxC);
                        Vector3d yp = xp;
                        yp.Transform(box_rot);
                        Vector3d xn = yp;
                        xn.Transform(box_rot);
                        Vector3d yn = xn;
                        yn.Transform(box_rot);

                        var sweep = new Rhino.Geometry.SweepOneRail();
                        sweep.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                        sweep.ClosedSweep = true;
                        sweep.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                        Point3d[] boxPts = new Point3d[5];

                        boxPts[0] = boxC + xp + yp;
                        boxPts[1] = boxC + xn + yp;
                        boxPts[2] = boxC + xn + yn;
                        boxPts[3] = boxC + xp + yn;
                        boxPts[4] = boxPts[0];

                        //Curve segBoxRect = new Polyline(boxPts).ToNurbsCurve();
                        //var boxPipe = sweep.PerformSweep(crv1, segBoxRect);

                        //Brep pipe1 = boxPipe[0].CapPlanarHoles(myDoc.ModelAbsoluteTolerance);
                        #endregion

                        #region create the protrusion on the pin/pad
                        Brep pipe_pad = new Brep();
                        Point3d boxCenter = new Point3d();
                        Line ln2 = new Line(extStart0 + boxDir / boxDir.Length * (boxDir.Length + 0.1), extStart0);
                        Curve crv2 = ln2.ToNurbsCurve();

                        if (distPins(cmPrt.Pins) > 2.5)
                        {
                            pipe_pad = Brep.CreatePipe(crv2, 1, false, PipeCapMode.Flat, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians)[0];
                            //Brep pipe_pad_cylinder = Brep.CreatePipe(crv2, 1, false, PipeCapMode.Flat, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians)[0];
                            //Sphere pipe_tip = new Sphere(extStart0 + boxDir / boxDir.Length * (boxDir.Length + 0.5), 1);
                            //Brep pipe_tip_sphere = pipe_tip.ToBrep();
                            //pipe_pad = Brep.CreateBooleanUnion(new List<Brep> { pipe_pad_cylinder, pipe_tip_sphere }, myDoc.ModelAbsoluteTolerance)[0];
                            ////boxCenter = (extStart0 + extStart0 + boxDir / boxDir.Length * (boxDir.Length + 2)) / 2;
                            boxCenter = boxC + boxDir / 2;
                        }
                        else
                        {
                            Curve segBoxRect1 = new Polyline(boxPts).ToNurbsCurve();
                            var boxPipe1 = sweep.PerformSweep(crv2, segBoxRect1);

                            pipe_pad = boxPipe1[0].CapPlanarHoles(myDoc.ModelAbsoluteTolerance);
                            boxCenter = boxC + boxDir / 2;
                        }

                        #endregion


                        #region create the connection between the center of the box extrusion and fromPt
                        //Point3d boxCenter = boxC + boxDir / 2;
                        Brep pipe3 = new Brep();
                        if (boxCenter.DistanceTo(fromPt) <= 0.8)
                        {
                            Point3d c = (fromPt + boxCenter) / 2;
                            Sphere s = new Sphere(c, fromPt.DistanceTo(c));
                            pipe3 = s.ToBrep();
                        }
                        else
                        {
                            Line ln3 = new Line(boxCenter, fromPt);
                            Curve crv3 = ln3.ToNurbsCurve();
                            pipe3 = Brep.CreatePipe(crv3, radius, false, PipeCapMode.Round, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians)[0];
                        }

                        #endregion

                        //var pipeTrc = Brep.CreateBooleanUnion(new List<Brep> { pipe_pad, pipeS, pipe3 }, myDoc.ModelAbsoluteTolerance)[0];
                        var pipeTrc = Brep.CreateBooleanUnion(new List<Brep> { pipeS, pipe3 }, myDoc.ModelAbsoluteTolerance);
                        int trc_idx = dataset.deployedTraces.IndexOf(s_trc);

                        if (pipeTrc != null && pipeTrc.Count() > 0)
                        {
                            Guid trc_ID = myDoc.Objects.AddBrep(pipeTrc[0], orangeAttribute);
                            
                            dataset.deployedTraces.ElementAt(trc_idx).TrID.Add(trc_ID);
                        }
                        else
                        {
                            // the generated pipes cannont be unioned
                            Guid trc_pipeS_ID = myDoc.Objects.AddBrep(pipeS, orangeAttribute);
                            Guid trc_pipe3_ID = myDoc.Objects.AddBrep(pipe3, orangeAttribute);

                            dataset.deployedTraces.ElementAt(trc_idx).TrID.Add(trc_pipeS_ID);
                            dataset.deployedTraces.ElementAt(trc_idx).TrID.Add(trc_pipe3_ID);
                        }

                        Guid pad_ID = myDoc.Objects.AddBrep(pipe_pad, orangeAttribute);
                        dataset.pads.Add(pad_ID);

                        myDoc.Views.Redraw();
                        #endregion
                    }

                    #endregion
                   
                    #region Second, generate the polyline using A* search 

                    // sort the unSettledTraces list by the ascending trace length order

                    unSettledTraces.Sort((a, b) => (a.PrtBPos.DistanceTo(a.PrtAPos)).CompareTo(b.PrtBPos.DistanceTo(b.PrtAPos)));
                    
                    // back up all the points for routes
                    List<Point3d> backup_ForRt = new List<Point3d>();
                    foreach (Point3d pt in currBody.forRtPts)
                    {
                        backup_ForRt.Add(pt);
                    }

                    List<int> deployed_idx = new List<int>();
                    foreach (Trace un_trc in unSettledTraces)
                    {
                        #region update currBody.forRtPts by excluding those voxels that overlaps with the already-deployed traces

                        // reset currBody.forRtPts
                        currBody.forRtPts.Clear();
                        foreach (Point3d pt in backup_ForRt)
                        {
                            currBody.forRtPts.Add(pt);
                        }

                        // find all the traces that should be excluded from the route generation
                        List<Guid> exc_trace_brep_IDs = new List<Guid>();
                        foreach(Trace tr in dataset.deployedTraces)
                        {
                            if(tr.PrtBPos.X==un_trc.PrtBPos.X && tr.PrtBPos.Y == un_trc.PrtBPos.Y && tr.PrtBPos.Z == un_trc.PrtBPos.Z)
                            {
                                // find the net and exclude the traces in that net
                                continue;
                            }
                            else
                            {
                                foreach(Guid id in tr.TrID)
                                {
                                    exc_trace_brep_IDs.Add(id);
                                }
                            }

                        }

                        // update currBody.forRtPts by excluding those points within the range of those excluded traces
                        List<Point3d> toRemovePts = new List<Point3d>();
                        foreach(Point3d rtPt in currBody.forRtPts)
                        { 
                            foreach(Guid id in exc_trace_brep_IDs)
                            {
                                Brep trBrep = new Brep();
                                if (myDoc.Objects.Find(id)!=null)
                                    trBrep = (Brep)myDoc.Objects.Find(id).Geometry;

                                if(trBrep != null)
                                    if(trBrep.ClosestPoint(rtPt).DistanceTo(rtPt) <= (radius + traceSpacing) || trBrep.IsPointInside(rtPt,myDoc.ModelAbsoluteTolerance,false))
                                    {
                                        toRemovePts.Add(rtPt);
                                        break;
                                    }
                            }
                        }
                        List<int> positions = new List<int>();

                        foreach(Point3d trPt in toRemovePts)
                        {
                            int idx = currBody.forRtPts.IndexOf(trPt);
                            positions.Add(idx);
                            
                        }
                        positions.Sort((a, b) => b.CompareTo(a));
                        foreach(int idx in positions)
                        {
                            currBody.forRtPts.RemoveAt(idx);
                        }
                        #endregion

                        // The real wire is: un_trc.PrtAPos -> extStart1 -> fromPt -> toPt -> un_trc.PrtBPos
                        // Remember to check it the trace overlaps with existing traces, except for the same net
                        Vector3d dir = dataset.part3DList.Find(x => x.PartName.Substring(x.PartName.IndexOf('-') + 1, x.PartName.Length - x.PartName.IndexOf('-') - 1).Equals(un_trc.PrtName)).Normal;
                        Part3D cmPrt = dataset.part3DList.Find(x => x.PartName.Substring(x.PartName.IndexOf('-') + 1, x.PartName.Length - x.PartName.IndexOf('-') - 1).Equals(un_trc.PrtName));
                        
                        Point3d extStart0 = un_trc.PrtAPos - extLen * dir;
                        Vector3d pt_dir = un_trc.PrtAPos - cmPrt.PartPos;
                        Plane prt_bottom_plane = new Plane(extStart0, dir);
                        Point3d pt_dir_proj = prt_bottom_plane.ClosestPoint(extStart0 + pt_dir);
                        Vector3d pt_dir_inplane = pt_dir_proj - extStart0;
                        Point3d extStart1;
                        if (pt_dir_inplane.Length == 0)
                            extStart1 = extStart0;
                        else
                            extStart1 = pt_dir_inplane / pt_dir_inplane.Length * extLen + extStart0;


                        Point3d fromPt = GetClosestFromPointCloud(extStart1, currBody.forRtPts, new Vector3d(extStart1 - un_trc.PrtAPos));
                        Point3d toPt = GetClosestFromPointCloudSimple(un_trc.PrtBPos, currBody.forRtPts);
                        //Point3d toPt = un_trc.PrtBPos;

                        Route currRoute = new Route();
                        currRoute.RoutePts.Add(fromPt);
                        currRoute.RoutePts.Add(toPt);

                        // remember to connect un_trc.PrtBPos and toPt in the point cloud

                        #region generate the route between the two points in route list using A* search algorithm.

                        #region Old version of A* algorithm (commented)
                        //Location current = null;
                        //route.Clear();
                        //Point3d startPt = new Point3d();
                        //Point3d targetPt = new Point3d();

                        //// find the real starting point inside the body
                        //Point3d pt_start = currRoute.RoutePts.ElementAt(0);

                        //#region commented for future reference

                        ////foreach (Surface surf in seleBrep.Surfaces)
                        ////{
                        ////    double u, v;
                        ////    surf.ClosestPoint(pt_start, out u, out v);
                        ////    if (pt_start.DistanceTo(surf.PointAt(u, v)) <= 0.4)
                        ////    {
                        ////        Point3d pt_route_start = surf.NormalAt(u, v) * offsetDis * gridSize / 2 + pt_start;

                        ////        // test if the point is inside the object
                        ////        Point3d tempPt = GetClosestFromPointCloudSimple(pt_start, currBody.forRtPts);
                        ////        Vector3d v1 = tempPt - pt_start;
                        ////        Vector3d v2 = pt_route_start - pt_start;
                        ////        if ((v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z) < 0)
                        ////        {
                        ////            pt_route_start = surf.NormalAt(u, v) * offsetDis * (-1) * gridSize / 2 + pt_start;
                        ////        }

                        ////        currRoute.RoutePts.Add(pt_route_start);

                        ////        //myDoc.Objects.AddPoint(pt_route_start, orangeAttribute);
                        ////        //myDoc.Views.Redraw();
                        ////        break;
                        ////    }
                        ////}

                        //#endregion

                        //// find the real ending point inside the body
                        //Point3d pt_end = currRoute.RoutePts.ElementAt(1);

                        //#region commented for future reference
                        ////foreach (Surface surf in seleBrep.Surfaces)
                        ////{
                        ////    double u, v;
                        ////    surf.ClosestPoint(pt_end, out u, out v);
                        ////    if (pt_end.DistanceTo(surf.PointAt(u, v)) <= 0.4)
                        ////    {
                        ////        Point3d pt_route_end = surf.NormalAt(u, v) * offsetDis * gridSize / 2 + pt_end;

                        ////        // test if the point is inside the object
                        ////        Point3d tempPt = GetClosestFromPointCloudSimple(pt_end, currBody.forRtPts);
                        ////        Vector3d v1 = tempPt - pt_end;
                        ////        Vector3d v2 = pt_route_end - pt_end;
                        ////        if ((v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z) < 0)
                        ////        {
                        ////            pt_route_end = surf.NormalAt(u, v) * offsetDis * (-1) * gridSize / 2 + pt_end;
                        ////        }

                        ////        currRoute.RoutePts.Add(pt_route_end);

                        ////        //myDoc.Objects.AddPoint(pt_route_start, orangeAttribute);
                        ////        //myDoc.Views.Redraw();
                        ////        break;
                        ////    }
                        ////}
                        //#endregion

                        //// startPt = GetClosestFromPointCloud(currRoute.RoutePts.ElementAt(2), currBody.forRtPts);   // currRoute.routePts.ElementAt(2) is pt_route_start
                        //// targetPt = GetClosestFromPointCloud(currRoute.RoutePts.ElementAt(3), currBody.forRtPts);  // currRoute.routePts.ElementAt(3) is pt_route_end

                        //startPt = pt_start;
                        //targetPt = pt_end;

                        //Location start = new Location { X = startPt.X, Y = startPt.Y, Z = startPt.Z };
                        //Location target = new Location { X = targetPt.X, Y = targetPt.Y, Z = targetPt.Z };

                        //start.G = 0;
                        //start.H = ComputeHScore(start.X, start.Y, start.Z, target.X, target.Y, target.Z);
                        //start.F = start.G + start.H;
                        //List<Location> openList = new List<Location>();
                        //List<Location> closedList = new List<Location>();
                        //double g = 0;

                        //// start by adding the original position to the open list
                        //openList.Add(start);

                        //bool isfoundPath = false;

                        //while (openList.Count > 0)
                        //{
                        //    // get the square with the lowest F score
                        //    var lowest = openList.Min(l => l.F);
                        //    current = openList.First(l => l.F == lowest);

                        //    // add the current square to the closed list
                        //    closedList.Add(current);

                        //    // remove it from the open list
                        //    openList.Remove(current);

                        //    // record the current as the route
                        //    route.Add(new Point3d(current.X, current.Y, current.Z));

                        //    // if we added the destination to the closed list, we've found a path
                        //    if (closedList.FirstOrDefault(l => l.X == target.X && l.Y == target.Y && l.Z == target.Z) != null)
                        //    {
                        //        isfoundPath = true;
                        //        break;
                        //    }

                        //    int pos_remove = currBody.forRtPts.IndexOf(new Point3d(current.X, current.Y, current.Z));
                        //    currBody.forRtPts.RemoveAt(pos_remove);

                        //    var adjacentVoxels = GetWalkableAdjacentVoxels(current.X, current.Y, current.Z, currBody.forRtPts, openList);
                        //    //g = current.G + gridSize;

                        //    foreach (var adjacentVoxel in adjacentVoxels)
                        //    {
                        //        // if this adjacent voxel is already in the closed list, ignore it
                        //        if (closedList.FirstOrDefault(l => l.X == adjacentVoxel.X
                        //                && l.Y == adjacentVoxel.Y && l.Z == adjacentVoxel.Z) != null)
                        //            continue;

                        //        // if it's not in the open list...
                        //        if (openList.FirstOrDefault(l => l.X == adjacentVoxel.X
                        //                && l.Y == adjacentVoxel.Y && l.Z == adjacentVoxel.Z) == null)
                        //        {
                        //            // compute its score, set the parent
                        //            adjacentVoxel.G = 0;
                        //            adjacentVoxel.H = ComputeHScore(adjacentVoxel.X,
                        //                adjacentVoxel.Y, adjacentVoxel.Z, target.X, target.Y, target.Z);
                        //            adjacentVoxel.F = adjacentVoxel.G + adjacentVoxel.H;
                        //            adjacentVoxel.Parent = current;

                        //            // and add it to the open list
                        //            openList.Insert(0, adjacentVoxel);
                        //        }
                        //    }

                        //}

                        //if (!isfoundPath)
                        //{
                        //    continue;
                        //}

                        #endregion

                        VoxClass startVox = new VoxClass();
                        startVox.X = fromPt.X;
                        startVox.Y = fromPt.Y;
                        startVox.Z = fromPt.Z;

                        VoxClass endVox = new VoxClass();
                        endVox.X = toPt.X;
                        endVox.Y = toPt.Y;
                        endVox.Z = toPt.Z;

                        AStarHelper astartHelper = new AStarHelper();
                        astartHelper.Start = startVox;
                        astartHelper.End = endVox;
                        astartHelper.InitializePathFinding();

                        List<Point3d> pathPoints = new List<Point3d>();
                        pathPoints = astartHelper.ExecuteAStarPathFinding(currBody.forRtPts, gridSize);

                        if (pathPoints.Count == 0)
                            continue;

                        #region finish the route

                        // The real wire is: un_trc.PrtAPos -> extStart1 -> fromPt -> toPt -> un_trc.PrtBPos
                        // Remember to clear the voxels around toPt->un_trc.PrtBPos

                        List<Point3d> routeTrace = new List<Point3d>();
                        Point3d Pt0 = un_trc.PrtAPos;
                        Point3d Pt1 = extStart1;
                        Point3d Pt2 = fromPt;
                        Point3d Pt3 = toPt;
                        //Point3d Pt4 = un_trc.PrtBPos;

                        #region create the contact box

                        Point3d boxC_init = (extStart0 + extStart1) / 2;
                        Point3d boxC = (boxC_init + extStart0) / 2;
                        Vector3d boxDir = Pt0 - extStart0;
                        Line ln1 = new Line(boxC + boxDir, boxC);
                        Curve crv1 = ln1.ToNurbsCurve();

                        //Vector3d xp = (extStart0 - boxC) / (extStart0.DistanceTo(boxC)) * (extStart0.DistanceTo(boxC) + radius / 2);
                        Vector3d xp = (extStart0 - boxC) / extStart0.DistanceTo(boxC) * (extStart0.DistanceTo(boxC));
                        Transform box_rot = Transform.Rotation(Math.PI / 2, boxDir, boxC);
                        Vector3d yp = xp;
                        yp.Transform(box_rot);
                        Vector3d xn = yp;
                        xn.Transform(box_rot);
                        Vector3d yn = xn;
                        yn.Transform(box_rot);

                        var sweep = new Rhino.Geometry.SweepOneRail();
                        sweep.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                        sweep.ClosedSweep = true;
                        sweep.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                        Point3d[] boxPts = new Point3d[5];

                        boxPts[0] = boxC + xp + yp;
                        boxPts[1] = boxC + xn + yp;
                        boxPts[2] = boxC + xn + yn;
                        boxPts[3] = boxC + xp + yn;
                        boxPts[4] = boxPts[0];

                        //Curve segBoxRect = new Polyline(boxPts).ToNurbsCurve();
                        //var boxPipe = sweep.PerformSweep(crv1, segBoxRect);

                        //Brep pipe1 = boxPipe[0].CapPlanarHoles(myDoc.ModelAbsoluteTolerance);

                        #region create the protrusion on the pin/pad

                        Brep pipe_pad = new Brep();
                        Point3d boxCenter = new Point3d();
                        Line ln2 = new Line(extStart0 + boxDir / boxDir.Length * (boxDir.Length + 0.1), extStart0);
                        Curve crv2 = ln2.ToNurbsCurve();

                        if (distPins(cmPrt.Pins) > 2.5)
                        {
                            pipe_pad = Brep.CreatePipe(crv2, 1, false, PipeCapMode.Flat, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians)[0];
                            //Brep pipe_pad_cylinder = Brep.CreatePipe(crv2, 1, false, PipeCapMode.Flat, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians)[0];
                            //Sphere pipe_tip = new Sphere(extStart0 + boxDir / boxDir.Length * (boxDir.Length + 0.5), 1);
                            //Brep pipe_tip_sphere = pipe_tip.ToBrep();
                            //pipe_pad = Brep.CreateBooleanUnion(new List<Brep> { pipe_pad_cylinder, pipe_tip_sphere }, myDoc.ModelAbsoluteTolerance)[0];

                            ////pipe_pad = Brep.CreatePipe(crv2, 1, false, PipeCapMode.Round, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians)[0];
                            ////boxCenter = (extStart0 + extStart0 + boxDir / boxDir.Length * (boxDir.Length + 2)) / 2;
                            boxCenter = boxC + boxDir / 2;
                        }
                        else
                        {
                            Curve segBoxRect1 = new Polyline(boxPts).ToNurbsCurve();
                            var boxPipe1 = sweep.PerformSweep(crv2, segBoxRect1);
                            boxCenter = boxC + boxDir / 2;

                            pipe_pad = boxPipe1[0].CapPlanarHoles(myDoc.ModelAbsoluteTolerance);
                        }
                        #endregion 

                        #endregion

                        #region create the connection between the center of the box extrusion and fromPt
                        //Point3d boxCenter = boxC + boxDir / 2;
                        Brep pipe3 = new Brep();
                        if (boxCenter.DistanceTo(fromPt) <= 0.8)
                        {
                            Point3d c = (fromPt + boxCenter) / 2;
                            Sphere s = new Sphere(c, fromPt.DistanceTo(c));
                            pipe3 = s.ToBrep();

                        }
                        else
                        {
                            Line ln3 = new Line(boxCenter, fromPt);
                            Curve crv3 = ln3.ToNurbsCurve();
                            pipe3 = Brep.CreatePipe(crv3, radius, false, PipeCapMode.Round, false, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians)[0];
                        }

                        #endregion

                        //routeTrace.Add(Pt1);
                        //routeTrace.Add(Pt2);

                        //foreach (Point3d pt in route)
                        //{
                        //    routeTrace.Add(pt);
                        //}
                        for(int i = pathPoints.Count-1; i>=0; i--)
                        {
                            routeTrace.Add(pathPoints.ElementAt(i));
                        }
                        // routeTrace.Add(Pt3);
                        //routeTrace.Add(Pt4);

                        #region old code (commented)

                        //currRoute.RoutePts.Clear();
                        ////currRoute.RoutePts.Add(Pt0);
                        //currRoute.RoutePts.Add(Pt1);
                        //currRoute.RoutePts.Add(Pt2);
                        //foreach (Point3d pt in route)
                        //{
                        //    currRoute.RoutePts.Add(pt);
                        //}
                        //currRoute.RoutePts.Add(Pt3);
                        //currRoute.RoutePts.Add(Pt4);
                        ////currBody.routes.Add(currRoute);

                        #endregion

                        #endregion

                        #region generate the discrete pipes as the route
                        List<Brep> routeBreps = new List<Brep>();

                        //routeBreps.Add(pipe_pad);
                        ////routeBreps.Add(pipe1);  // add the contact box first
                        
                        ////if (currBody.RoutePts.Last().r_IDs.Count() != 0)
                        ////    currBody.routes.Last().r_IDs.Clear();

                        List<Curve> crvs = new List<Curve>();

                        #region old code (commented)

                        //for (int i = 1; i < currRoute.RoutePts.Count(); i++)
                        //{
                        //    Point3d prev_pt = currRoute.RoutePts.ElementAt(i - 1);
                        //    Point3d curr_pt = currRoute.RoutePts.ElementAt(i);

                        //    Line tempLn = new Line(prev_pt, curr_pt);
                        //    Curve tempCrv = tempLn.ToNurbsCurve();

                        //    if (printMeta.TrShape == 1)
                        //    {
                        //        // round
                        //        var tempPipe = Brep.CreatePipe(tempCrv, radius, true, PipeCapMode.Round, true, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians);

                        //        foreach (Brep rp in tempPipe)
                        //        {
                        //            routeBreps.Add(rp);
                        //        }

                        //    }
                        //    else if (printMeta.TrShape == 2)
                        //    {
                        //        // square
                        //        Plane squarePln = new Plane(tempCrv.PointAtStart, tempCrv.TangentAtStart);

                        //        Point3d[] segPts = new Point3d[5];
                        //        Transform txp_rect = Transform.Translation(squarePln.XAxis * radius);
                        //        Transform typ_rect = Transform.Translation(squarePln.YAxis * radius);
                        //        Transform txn_rect = Transform.Translation(squarePln.XAxis * (-1) * radius);
                        //        Transform tyn_rect = Transform.Translation(squarePln.YAxis * (-1) * radius);

                        //        segPts[0] = tempCrv.PointAtStart;
                        //        segPts[1] = tempCrv.PointAtStart;
                        //        segPts[2] = tempCrv.PointAtStart;
                        //        segPts[3] = tempCrv.PointAtStart;
                        //        segPts[4] = tempCrv.PointAtStart;

                        //        segPts[0].Transform(txp_rect); segPts[0].Transform(typ_rect);
                        //        segPts[1].Transform(txn_rect); segPts[1].Transform(typ_rect);
                        //        segPts[2].Transform(txn_rect); segPts[2].Transform(tyn_rect);
                        //        segPts[3].Transform(txp_rect); segPts[3].Transform(tyn_rect);
                        //        segPts[4] = segPts[0];

                        //        Curve segRect = new Polyline(segPts).ToNurbsCurve();
                        //        var routePipe = sweep.PerformSweep(tempCrv, segRect);

                        //        foreach (Brep rp in routePipe)
                        //        {
                        //            Brep rpdup = rp.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);
                        //            routeBreps.Add(rpdup);
                        //        }

                        //    }
                        //    else if (printMeta.TrShape == 3)
                        //    {
                        //        // triangle
                        //        Plane triPln = new Plane(tempCrv.PointAtStart, tempCrv.TangentAtStart);
                        //        Point3d[] segPts = new Point3d[4];
                        //        Transform t_tri = Transform.Translation(triPln.XAxis * radius);
                        //        Transform t_rotate_tri = Transform.Rotation(2 * Math.PI / 3, tempCrv.TangentAtStart, tempCrv.PointAtStart);

                        //        segPts[0] = tempCrv.PointAtStart;
                        //        segPts[1] = tempCrv.PointAtStart;
                        //        segPts[2] = tempCrv.PointAtStart;
                        //        segPts[3] = tempCrv.PointAtStart;

                        //        segPts[0].Transform(t_tri);
                        //        segPts[1].Transform(t_tri); segPts[1].Transform(t_rotate_tri);
                        //        segPts[2].Transform(t_tri); segPts[2].Transform(t_rotate_tri); segPts[2].Transform(t_rotate_tri);
                        //        segPts[3] = segPts[0];

                        //        Curve segTriangle = new Polyline(segPts).ToNurbsCurve();
                        //        var routePipe = sweep.PerformSweep(tempCrv, segTriangle);

                        //        foreach (Brep rp in routePipe)
                        //        {
                        //            Brep rpdup = rp.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);
                        //            routeBreps.Add(rpdup);
                        //        }
                        //    }
                        //}

                        #endregion

                        for (int i = 1; i < routeTrace.Count(); i++)
                        {
                            Point3d prev_pt = routeTrace.ElementAt(i - 1);
                            Point3d curr_pt = routeTrace.ElementAt(i);

                            Line tempLn = new Line(prev_pt, curr_pt);
                            Curve tempCrv = tempLn.ToNurbsCurve();

                            if (printMeta.TrShape == 1)
                            {
                                // round
                                var tempPipe = Brep.CreatePipe(tempCrv, radius, true, PipeCapMode.Round, true, myDoc.ModelAbsoluteTolerance, myDoc.ModelAngleToleranceRadians);

                                foreach (Brep rp in tempPipe)
                                {
                                    routeBreps.Add(rp);
                                }

                            }
                            else if (printMeta.TrShape == 2)
                            {
                                // square
                                Plane squarePln = new Plane(tempCrv.PointAtStart, tempCrv.TangentAtStart);

                                Point3d[] segPts = new Point3d[5];
                                Transform txp_rect = Transform.Translation(squarePln.XAxis * radius);
                                Transform typ_rect = Transform.Translation(squarePln.YAxis * radius);
                                Transform txn_rect = Transform.Translation(squarePln.XAxis * (-1) * radius);
                                Transform tyn_rect = Transform.Translation(squarePln.YAxis * (-1) * radius);

                                segPts[0] = tempCrv.PointAtStart;
                                segPts[1] = tempCrv.PointAtStart;
                                segPts[2] = tempCrv.PointAtStart;
                                segPts[3] = tempCrv.PointAtStart;
                                segPts[4] = tempCrv.PointAtStart;

                                segPts[0].Transform(txp_rect); segPts[0].Transform(typ_rect);
                                segPts[1].Transform(txn_rect); segPts[1].Transform(typ_rect);
                                segPts[2].Transform(txn_rect); segPts[2].Transform(tyn_rect);
                                segPts[3].Transform(txp_rect); segPts[3].Transform(tyn_rect);
                                segPts[4] = segPts[0];

                                Curve segRect = new Polyline(segPts).ToNurbsCurve();
                                var routePipe = sweep.PerformSweep(tempCrv, segRect);

                                foreach (Brep rp in routePipe)
                                {
                                    Brep rpdup = rp.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);
                                    routeBreps.Add(rpdup);
                                }

                            }
                            else if (printMeta.TrShape == 3)
                            {
                                // triangle
                                Plane triPln = new Plane(tempCrv.PointAtStart, tempCrv.TangentAtStart);
                                Point3d[] segPts = new Point3d[4];
                                Transform t_tri = Transform.Translation(triPln.XAxis * radius);
                                Transform t_rotate_tri = Transform.Rotation(2 * Math.PI / 3, tempCrv.TangentAtStart, tempCrv.PointAtStart);

                                segPts[0] = tempCrv.PointAtStart;
                                segPts[1] = tempCrv.PointAtStart;
                                segPts[2] = tempCrv.PointAtStart;
                                segPts[3] = tempCrv.PointAtStart;

                                segPts[0].Transform(t_tri);
                                segPts[1].Transform(t_tri); segPts[1].Transform(t_rotate_tri);
                                segPts[2].Transform(t_tri); segPts[2].Transform(t_rotate_tri); segPts[2].Transform(t_rotate_tri);
                                segPts[3] = segPts[0];

                                Curve segTriangle = new Polyline(segPts).ToNurbsCurve();
                                var routePipe = sweep.PerformSweep(tempCrv, segTriangle);

                                foreach (Brep rp in routePipe)
                                {
                                    Brep rpdup = rp.CapPlanarHoles(myDoc.ModelAbsoluteTolerance);
                                    routeBreps.Add(rpdup);
                                }
                            }
                        }

                        routeBreps.Add(pipe3);
                        #endregion

                        //elecPos.Clear();
                        #endregion

                        var traceUnit = Brep.CreateBooleanUnion(routeBreps, myDoc.ModelAbsoluteTolerance);

                        Trace currTrace = new Trace();
                        
                        currTrace.PrtName = un_trc.PrtName;
                        currTrace.PrtPinName = un_trc.PrtPinName;
                        currTrace.PrtAPos = un_trc.PrtAPos;
                        currTrace.PrtBPos = un_trc.PrtBPos;
                        currTrace.Pts = routeTrace;
                        currTrace.Type = 1;

                        if (traceUnit != null && traceUnit.Count() > 0)
                        {
                            Guid tr_id = myDoc.Objects.AddBrep(traceUnit[0], orangeAttribute);
                            currTrace.TrID.Add(tr_id);
                        }
                        else
                        {
                            foreach(Brep b in routeBreps)
                            {
                                Guid temp_id = myDoc.Objects.AddBrep(b, orangeAttribute);
                                currTrace.TrID.Add(temp_id);
                            }
                        }

                        dataset.deployedTraces.Add(currTrace);

                        Guid pad_id = myDoc.Objects.AddBrep(pipe_pad, orangeAttribute);
                        dataset.pads.Add(pad_id);

                        myDoc.Views.Redraw();

                        int un_idx = unSettledTraces.IndexOf(un_trc);
                        deployed_idx.Add(un_idx);
                        
                    }

                    deployed_idx.Sort((a, b) => b.CompareTo(a));
                    foreach(int i in deployed_idx)
                    {
                        unSettledTraces.RemoveAt(i);
                    }
                    #endregion

                    #endregion

                    #region Third, deal with the unsettled traces (keep the red connections there and hide those already deployed) and update the currently selected trace

                    int jw_index = myDoc.Layers.FindName("JumpWires").Index;
                    myDoc.Layers.SetCurrentLayerIndex(jw_index, true);
                    redAttribute.LayerIndex = jw_index;
                    myDoc.Layers.SetCurrentLayerIndex(jw_index, true);
                    

                    foreach (TraceCluster trCluster in dataset.traceOracle)
                    {
                        foreach (JumpWire jw in trCluster.Jumpwires)
                        {
                            bool isPartAFound = false;
                            bool isPartBFound = false;

                            foreach (Trace tr in dataset.deployedTraces)
                            {
                                if (tr.PrtName.Equals(jw.StartPrtName))
                                    isPartAFound = true;

                                if (tr.PrtName.Equals(jw.EndPrtName))
                                    isPartBFound = true;
                            }

                            int idx = trCluster.Jumpwires.IndexOf(jw);

                            if (isPartAFound && isPartBFound)
                            {
                                // both ends are found
                                trCluster.Jumpwires.ElementAt(idx).IsDeployed = true;
                            }
                        }
                    }


                    foreach (TraceCluster trCluster in dataset.traceOracle)
                    {
                        foreach (JumpWire jw in trCluster.Jumpwires)
                        {
                            //Part3D p3dStart = dataset.part3DList.Find(x => x.PartName.Substring(x.PartName.IndexOf('-') + 1, x.PartName.Length - x.PartName.IndexOf('-') - 1).Equals(jw.StartPrtName));
                            //Point3d newStartPos = p3dStart.Pins.Find(x => x.PinName.Equals(jw.StartPinName)).Pos;
                            Point3d newStartPos = GetPinPointByNames(jw.StartPrtName, jw.StartPinName);

                            //Part3D p3dEnd = dataset.part3DList.Find(x => x.PartName.Substring(x.PartName.IndexOf('-') + 1, x.PartName.Length - x.PartName.IndexOf('-') - 1).Equals(jw.EndPrtName));
                            //Point3d newEndPos = p3dEnd.Pins.Find(x => x.PinName.Equals(jw.EndPinName)).Pos;
                            Point3d newEndPos = GetPinPointByNames(jw.EndPrtName, jw.EndPinName);

                            Line l = new Line(newStartPos, newEndPos);
                            Curve c = l.ToNurbsCurve();
                            myDoc.Objects.Delete(jw.CrvID, true);
                            myDoc.Views.Redraw();

                            Guid c_id = myDoc.Objects.AddCurve(c, redAttribute);

                            int idx = trCluster.Jumpwires.IndexOf(jw);
                            trCluster.Jumpwires.ElementAt(idx).Crv = c;
                            trCluster.Jumpwires.ElementAt(idx).CrvID = c_id;

                            if (trCluster.Jumpwires.ElementAt(idx).IsDeployed)
                            {
                                // hide the red jump wires
                                myDoc.Objects.Hide(trCluster.Jumpwires.ElementAt(idx).CrvID, true);
                            }
                            
                        }
                    }

                    myDoc.Views.Redraw();

                    // update the trace list and set the currently selected trace
                    // dataset.currTrace = dataset.deployedTraces.ElementAt(dataset.deployedTraces.Count - 1);

                    #endregion

                    dataset.isTraceGenerated = true;
                }

                DA.SetData(0, true);
            }
            else
            {
                DA.SetData(0, false);
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
            get { return new Guid("e399ea8d-f28d-42dd-aadd-3222ef35a2c9"); }
        }
    }
}