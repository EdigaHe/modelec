using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using ModelecComponents.A_star;
using Rhino.Input;

namespace ModelecComponents
{
    public class ManualRouteComponent : GH_Component
    {
        bool testBtnClick;
        RhinoDoc myDoc;
        SharedData dataset;
        TargetBody currBody;
        ProcessingWindow prcWin;
        Point3d selPrtPosPt;
        ObjectAttributes orangeAttribute, redAttribute, blueAttribute;
        PrinterProfile printMeta;

        double gridSize;
        double radius;
        double traceSpacing;

        List<Point3d> route;

        int startType = -1; // 1: electronic component; 2: 3D-printed part; 3: trace
        int endType = -1; // 1: electronic component; 2: 3D-printed part; 3: trace

        bool isStartSelect = false;
        bool isEndSelect = false;

        Part3D startPart;
        Part3D endPart;
        Part3DP startPart3D;
        Part3DP endPart3D;
        Trace startTrace;
        Trace endTrace;
        int closerStartPtIdx;
        int closerEndPtIdx;
        double ctrlPtRadius;

        Point3d startPoint;
        Point3d endPoint;

        /// <summary>
        /// Initializes a new instance of the ManualRouteComponent class.
        /// </summary>
        public ManualRouteComponent()
          : base("ManualRouteComponent", "ManRou",
              "Allows the user to manually create the route between two points",
              "ModElec", "TraceOperations")
        {
            testBtnClick = false;
            myDoc = RhinoDoc.ActiveDoc;
            dataset = SharedData.Instance;
            currBody = TargetBody.Instance;
            prcWin = new ProcessingWindow();
            selPrtPosPt = new Point3d();
            printMeta = PrinterProfile.Instance;
            gridSize = 2;
            radius = 0;
            traceSpacing = 0;

            startType = -1;
            endType = -1;
            isStartSelect = false;
            isEndSelect = false;
            closerStartPtIdx = -1;
            closerEndPtIdx = -1;
            ctrlPtRadius = 0.5;

            startPoint = new Point3d();
            endPoint = new Point3d();

            route = new List<Point3d>();

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
            pManager.AddBooleanParameter("ManualRouteBtnClicked", "MC", "Manual-routing", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("AddManualTraceListUpdated", "ListUp2", "the list is updated based on the manually added trace", GH_ParamAccess.item);
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
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool btnClick = false;
            bool toExeAutoRoute = false;

            List<string> errors = new List<string>();

            RhinoApp.KeyboardEvent += GetKey;

            #region read the button click and decide whether execute the circuit load or not

            if (!DA.GetData(0, ref btnClick))
                return;

            if (!btnClick && testBtnClick)
            {
                toExeAutoRoute = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }

            #endregion

            if (toExeAutoRoute)
            {
                gridSize = 2;
                radius = (printMeta.Trace_min_x.Find(x => x.Cond.Equals("minimized resistance")) != null) ?
                    (Double.Parse(printMeta.Trace_min_x.Find(x => x.Cond.Equals("minimized resistance")).Value) / 1000 / 2.0) : 0.4;

                startType = -1;
                startPart = new Part3D();
                startPart3D = new Part3DP();
                startTrace = new Trace();
                endType = -1;
                endPart = new Part3D();
                endPart3D = new Part3DP();
                endTrace = new Trace();
                closerStartPtIdx = -1;
                closerEndPtIdx = -1;

                #region convert the body first if the body has not been converted

                
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

                if (!dataset.isTraceGenerated)
                {
                    #region Step 1: generate the sockets and convert the main body to the final body

                    List<Brep> finalSocketBreps = new List<Brep>();

                    foreach (partSocket pSocket in currBody.partSockets)
                    {
                        Brep socketBox = pSocket.SocketBrep;
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

                        #region Second, create a truncated pyramid

                        double offsetTP = 2;
                        if (height < 3)
                            offsetTP = (scktBoundingBox.GetCorners()[4].Z - scktBoundingBox.GetCorners()[0].Z) * Math.Sqrt(2) * 0.5 * (1 + printMeta.Shrinkage);

                        double miniOffsetTP = 0.4 * (1 + printMeta.Shrinkage);
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

                            var sweep1 = new SweepOneRail();
                            sweep1.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                            sweep1.ClosedSweep = false;
                            sweep1.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                            //myDoc.Objects.AddCurve(topRectCrv, orangeAttribute);
                            //myDoc.Views.Redraw();
                            //myDoc.Objects.AddCurve(bottomRectCrv, orangeAttribute);
                            //myDoc.Views.Redraw();
                            //myDoc.Objects.AddCurve(crossCrv, orangeAttribute);
                            //myDoc.Views.Redraw();

                            Brep[] tpBreps = sweep1.PerformSweep(bottomRectCrv, crossCrv);
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

                            var sweep1 = new SweepOneRail();
                            sweep1.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                            sweep1.ClosedSweep = false;
                            sweep1.SweepTolerance = myDoc.ModelAbsoluteTolerance;

                            //myDoc.Objects.AddCurve(topRectCrv, orangeAttribute);
                            //myDoc.Views.Redraw();
                            //myDoc.Objects.AddCurve(bottomRectCrv, orangeAttribute);
                            //myDoc.Views.Redraw();
                            //myDoc.Objects.AddCurve(crossCrv, orangeAttribute);
                            //myDoc.Views.Redraw();

                            Brep[] tpBreps = sweep1.PerformSweep(bottomRectCrv, crossCrv);
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

                        #endregion

                        #region Third, rotate the generated tuncated socket to the final orientation

                        truncatedSocket.Transform(p.TransformSets.ElementAt(1));

                        #endregion

                        #region Finally, flip and rotate the socket part to the correct orientation

                        if (p.IsFlipped)
                        {
                            Transform flipTrans = Transform.Rotation(p.Normal, reverseNormalVector, p.PartPos);

                            truncatedSocket.Transform(flipTrans);
                        }

                        Transform setExpTrans = Transform.Translation(p.Normal * p.ExposureLevel * p.Height);
                        truncatedSocket.Transform(setExpTrans);

                        Transform rotateAgain = Transform.Rotation(p.RotationAngle / 180.0 * Math.PI, p.Normal, p.PartPos);
                        truncatedSocket.Transform(rotateAgain);

                        #endregion

                        finalSocketBreps.Add(truncatedSocket);

                        //myDoc.Objects.AddBrep(truncatedSocket, orangeAttribute);
                        //myDoc.Views.Redraw();

                    }

                    List<Brep> bodyToDiff = new List<Brep>();

                    Brep bodyDupBrep = ((Brep)myDoc.Objects.Find(currBody.objID).Geometry).DuplicateBrep();
                    #region move the original body to a new layer called "OriginalBody"
                    if (myDoc.Layers.FindName("OriginalBody") == null)
                    {
                        // create a new layer named "Parts"
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
                    foreach (Part3D p3d in dataset.part3DList)
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

                                if (currBody.bodyBrep.ClosestPoint(p3d.PartPos).DistanceTo(p3d.PartPos) <= currBody.bodyBrep.ClosestPoint(p.Pos).DistanceTo(p.Pos))
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
                            finalBodyID = myDoc.Objects.AddBrep(bodyWithSockets[0], blueAttribute);
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

                    #endregion
                }

                #endregion

                #region ask the user to specify the start point type

                Rhino.Input.Custom.GetPoint gp1 = new Rhino.Input.Custom.GetPoint();
                gp1.SetCommandPrompt("Pick the start point. Press \"1\" for electronic component, \"2\" for 3D-printed part, or \"3\" for trace. Press enter to continue.");
                gp1.AcceptNothing(true);
                isStartSelect = true;
                gp1.MouseMove += Gp1_MouseMove;
                gp1.DynamicDraw += Gp1_DynamicDraw;
                Rhino.Input.GetResult r1;

                do
                {
                    r1 = gp1.Get(true);

                } while (r1 != Rhino.Input.GetResult.Nothing);
                isStartSelect = false;
                //startType = -1;
                //startPart = new Part3D();
                //startPart3D = new Part3DP();
                //startTrace = new Trace();
                #endregion

                #region ask the user to specify the end point type

                Rhino.Input.Custom.GetPoint gp2 = new Rhino.Input.Custom.GetPoint();
                gp2.SetCommandPrompt("Pick the end point. Press \"1\" for electronic component, \"2\" for 3D-printed part, or \"3\" for trace. Press enter to continue.");
                gp2.AcceptNothing(true);
                isEndSelect = true;
                gp2.MouseMove += Gp2_MouseMove;
                gp2.DynamicDraw += Gp2_DynamicDraw;
                Rhino.Input.GetResult r2;
                do
                {
                    r2 = gp2.Get(true);

                } while (r2 != Rhino.Input.GetResult.Nothing);

                isEndSelect = false;
                //endType = -1;
                //endPart = new Part3D();
                //endPart3D = new Part3DP();
                //endTrace = new Trace();
                #endregion

                #region generate the trace: first check if a straight line is possible; if not, using A* path finding algorithm

                #region switch to the "Trace" layer
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

                #region voxelize the body (without the already generated traces and gaps) and find the points inside the body 

                prcWin.Show();
                ObjRef objSel_ref = null;
                if (currBody.convertedObjID!=Guid.Empty)
                    // the traces from the circuit are generated and the body has been converted
                    objSel_ref = new ObjRef(currBody.convertedObjID);
                else
                    // no traces from the circuit are generated and the body has not been converted
                    objSel_ref = new ObjRef(currBody.objID);

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
                    if (seleBrep.ClosestPoint(p).DistanceTo(p) >= offsetDis)
                    {
                        currBody.forRtPts.Add(p);
                    }
                }

                prcWin.Hide();
                #endregion

                #region update currBody.forRtPts by excluding those voxels that overlaps with the already-deployed traces

                // find all the traces that should be excluded from the route generation
                List<Guid> exc_trace_brep_IDs = new List<Guid>();
                foreach (Trace tr in dataset.deployedTraces)
                {
                    // ToDo: this part can be optimized by testing the start and end point's type and selectively exclude the traces. 
                    // As now, excluding all the deployed traces.
                    foreach(Guid id in tr.TrID)
                    {
                        exc_trace_brep_IDs.Add(id);
                    } 
                }

                // update currBody.forRtPts by excluding those points within the range of those excluded traces
                List<Point3d> toRemovePts = new List<Point3d>();
                foreach (Point3d rtPt in currBody.forRtPts)
                {
                    foreach (Guid id in exc_trace_brep_IDs)
                    {
                        Brep trBrep = new Brep();
                        if (myDoc.Objects.Find(id) != null)
                            trBrep = (Brep)myDoc.Objects.Find(id).Geometry;

                        if (trBrep != null)
                            if (trBrep.ClosestPoint(rtPt).DistanceTo(rtPt) <= (radius + traceSpacing))
                            {
                                toRemovePts.Add(rtPt);
                                break;
                            }
                    }
                }
                List<int> positions = new List<int>();

                foreach (Point3d trPt in toRemovePts)
                {
                    int idx = currBody.forRtPts.IndexOf(trPt);
                    positions.Add(idx);

                }
                positions.Sort((a, b) => b.CompareTo(a));
                foreach (int idx in positions)
                {
                    currBody.forRtPts.RemoveAt(idx);
                }

                #endregion

                #region apply A* pathfinding algorithm

                Point3d realFromPt = GetClosestFromPointCloudSimple(startPoint, currBody.forRtPts);
                Point3d realToPt = GetClosestFromPointCloudSimple(endPoint, currBody.forRtPts);

                VoxClass startVox = new VoxClass();
                startVox.X = realFromPt.X;
                startVox.Y = realFromPt.Y;
                startVox.Z = realFromPt.Z;

                VoxClass endVox = new VoxClass();
                endVox.X = realToPt.X;
                endVox.Y = realToPt.Y;
                endVox.Z = realToPt.Z;

                AStarHelper astartHelper = new AStarHelper();
                astartHelper.Start = startVox;
                astartHelper.End = endVox;
                astartHelper.InitializePathFinding();

                List<Point3d> pathPoints = new List<Point3d>();
                pathPoints = astartHelper.ExecuteAStarPathFinding(currBody.forRtPts, gridSize);

                if (pathPoints.Count == 0)
                    RhinoApp.WriteLine("Error: Manual trace failed!");

                #endregion

                #region create the trace

                List<Point3d> routeTrace = new List<Point3d>();
                routeTrace.Add(startPoint);
                for (int i = pathPoints.Count - 1; i >= 0; i--)
                {
                    routeTrace.Add(pathPoints.ElementAt(i));
                }
                routeTrace.Add(endPoint);

                List<Brep> routeBreps = new List<Brep>();
                List<Curve> crvs = new List<Curve>();

                var sweep = new Rhino.Geometry.SweepOneRail();
                sweep.AngleToleranceRadians = myDoc.ModelAngleToleranceRadians;
                sweep.ClosedSweep = true;
                sweep.SweepTolerance = myDoc.ModelAbsoluteTolerance;

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


                #region register the new manual trace
                var traceUnit = Brep.CreateBooleanUnion(routeBreps, myDoc.ModelAbsoluteTolerance);
                Trace currTrace = new Trace();

                if (traceUnit != null){
                    Guid tr_id = myDoc.Objects.AddBrep(traceUnit[0], orangeAttribute);
                    currTrace.TrID.Add(tr_id);
                }
                else{
                    //DA.SetData(0, true);
                    //return;

                    foreach(Brep b in routeBreps)
                    {
                        Guid tr_id = myDoc.Objects.AddBrep(b, orangeAttribute);
                        currTrace.TrID.Add(tr_id);
                    }
                }
                    
                switch (startType)
                {
                    case 1:
                        {
                            currTrace.PrtName = startPart.PartName.Substring(startPart.PartName.IndexOf('-') + 1, startPart.PartName.Length - startPart.PartName.IndexOf('-') - 1);
                            currTrace.PrtPinName = startPart.Pins.ElementAt(closerStartPtIdx).PinName;
                            
                        }
                        break;
                    case 2:
                        {
                            currTrace.PrtName = startPart3D.PartName;
                            currTrace.PrtPinName = "";
                        }
                        break;
                    case 3:
                        {
                            currTrace.PrtName = "Trace (" + startTrace.TrID + ")";
                            currTrace.PrtPinName = "";
                        }break;
                    default:break;
                }

                switch (endType)
                {
                    case 1:
                        {
                            currTrace.DesPrtName = endPart.PartName.Substring(endPart.PartName.IndexOf('-') + 1, endPart.PartName.Length - endPart.PartName.IndexOf('-') - 1);
                            currTrace.DesPinName = endPart.Pins.ElementAt(closerEndPtIdx).PinName;
                        }
                        break;
                    case 2:
                        {
                            currTrace.DesPrtName = endPart3D.PartName;
                            currTrace.DesPinName = "";
                        }
                        break;
                    case 3:
                        {
                            currTrace.DesPrtName = "Trace (" + endTrace.TrID + ")";
                            currTrace.DesPinName = "";
                        }
                        break;
                    default: break;
                }

                currTrace.PrtAPos = startPoint;
                currTrace.PrtBPos = endPoint;

                currTrace.Pts = routeTrace;

                if (startType == 2 && endType == 2)
                {
                    // manual-3DP part to 3DP part (2<->2)
                    currTrace.Type = 2;
                }
                else if ((startType == 2 && endType == 3) || (startType == 3 && endType == 2)) 
                {
                    // manual-3DP part to trace (2<->3 or 3<->2)
                    currTrace.Type = 3;
                }
                else if ((startType == 1 && endType == 2) || (startType == 2 && endType == 1)) 
                {
                    // manual-3DP part to pad (1<->2 or 2<->1)
                    currTrace.Type = 4;
                }
                else if (startType == 3 && endType == 3)
                {
                    // manual-trace to trace (3<->3)
                    currTrace.Type = 5;
                }
                else if ((startType == 1 && endType == 3) || (startType == 3 && endType == 1))
                {
                    // manual-trace to pad (3<->1 or 1<->3)
                    currTrace.Type = 6;
                }
                else if (startType == 1 && endType == 1)
                {
                    // manual-pad to pad (1<->1)
                    currTrace.Type = 7;
                }

                dataset.deployedTraces.Add(currTrace);
                myDoc.Views.Redraw();

                #endregion

                #endregion

                #endregion

                DA.SetData(0, true);
            }
            else
            {
                DA.SetData(0, false);
            }
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

        private void Gp2_DynamicDraw(object sender, Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            switch (endType)
            {
                case 1:
                    {
                        e.Display.DrawSphere(new Sphere(endPart.Pins.ElementAt(closerEndPtIdx).Pos, ctrlPtRadius), Color.Yellow);
                    }
                    break;
                case 2:
                    {
                        Guid id = endPart3D.PrtID;
                        myDoc.Objects.UnselectAll();
                        myDoc.Objects.Select(id);
                        myDoc.Views.Redraw();
                    }
                    break;
                case 3:
                    {
                        myDoc.Objects.UnselectAll();
                        myDoc.Objects.Select(endTrace.TrID);
                        myDoc.Views.Redraw();
                    }
                    break;
                default: break;
            }
        }

        private void Gp2_MouseMove(object sender, Rhino.Input.Custom.GetPointMouseEventArgs e)
        {
            switch (endType)
            {
                case 1:
                    {
                        double minDis = double.MaxValue;
                        int idx = -1;

                        foreach (Part3D p3d in dataset.part3DList)
                        {
                            foreach (Pin p in p3d.Pins)
                            {
                                if (p.Pos.DistanceTo(e.Point) <= minDis)
                                {
                                    minDis = p.Pos.DistanceTo(e.Point);
                                    idx = dataset.part3DList.IndexOf(p3d);
                                    closerEndPtIdx = p3d.Pins.IndexOf(p);
                                }
                            }
                        }

                        endPart = dataset.part3DList.ElementAt(idx);

                        Vector3d dir = endPart.Normal;
                        double extLen = 1;
                        Point3d extStart0 = dataset.part3DList.ElementAt(idx).Pins.ElementAt(closerEndPtIdx).Pos - extLen * dir;
                        Vector3d pt_dir = dataset.part3DList.ElementAt(idx).Pins.ElementAt(closerEndPtIdx).Pos - endPart.PartPos;
                        Plane prt_bottom_plane = new Plane(extStart0, dir);
                        Point3d pt_dir_proj = prt_bottom_plane.ClosestPoint(extStart0 + pt_dir);
                        Vector3d pt_dir_inplane = pt_dir_proj - extStart0;
                        Point3d extStart1 = pt_dir_inplane / pt_dir_inplane.Length * extLen + extStart0;

                        Point3d boxC = ((extStart0 + extStart1) / 2 + extStart0) / 2;
                        Vector3d boxDir = dataset.part3DList.ElementAt(idx).Pins.ElementAt(closerEndPtIdx).Pos - extStart0;
                        endPoint = boxC + boxDir / 2;

                    }
                    break;
                case 2:
                    {
                        double minDis = double.MaxValue;
                        int idx = -1;

                        foreach (Part3DP p3dp in dataset.part3DPList)
                        {
                            if (p3dp.PartPos.DistanceTo(e.Point) <= minDis)
                            {
                                minDis = p3dp.PartPos.DistanceTo(e.Point);
                                idx = dataset.part3DPList.IndexOf(p3dp);
                            }
                        }

                        endPart3D = dataset.part3DPList.ElementAt(idx);
                        endPoint = dataset.part3DPList.ElementAt(idx).PartPos;
                    }
                    break;
                case 3:
                    {
                        double minDis = double.MaxValue;
                        int idx = -1;
                        Point3d endPtTest = new Point3d();

                        foreach (Trace trc in dataset.deployedTraces)
                        {
                            foreach(Guid t_id in trc.TrID)
                            {
                                Brep trcBrep = (Brep)myDoc.Objects.Find(t_id).Geometry;

                                if (trcBrep.ClosestPoint(e.Point).DistanceTo(e.Point) <= minDis)
                                {
                                    endPtTest = trcBrep.ClosestPoint(e.Point);
                                    minDis = trcBrep.ClosestPoint(e.Point).DistanceTo(e.Point);
                                    idx = dataset.deployedTraces.IndexOf(trc);
                                }
                            }
                            
                        }

                        endTrace = dataset.deployedTraces.ElementAt(idx);
                        endPoint = endPtTest;

                        //Brep targetBrep = (Brep)myDoc.Objects.Find(dataset.deployedTraces.ElementAt(idx).TrID).Geometry;
                        //endPoint = targetBrep.ClosestPoint(e.Point);
                    }
                    break;
                default: break;
            }
        }

        private void Gp1_DynamicDraw(object sender, Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            switch (startType)
            {
                case 1:
                    {
                        e.Display.DrawSphere(new Sphere(startPart.Pins.ElementAt(closerStartPtIdx).Pos, ctrlPtRadius), Color.Yellow);

                    }
                    break;
                case 2:
                    {
                        Guid id = startPart3D.PrtID;
                        myDoc.Objects.UnselectAll();
                        myDoc.Objects.Select(id);
                        myDoc.Views.Redraw();
                    }
                    break;
                case 3:
                    {
                        myDoc.Objects.UnselectAll();
                        myDoc.Objects.Select(startTrace.TrID);
                        myDoc.Views.Redraw();
                    }
                    break;
                default: break;
            }
        }

        private void Gp1_MouseMove(object sender, Rhino.Input.Custom.GetPointMouseEventArgs e)
        {
            switch (startType)
            {
                case 1:
                    {
                        double minDis = double.MaxValue;
                        int idx = -1;

                        foreach (Part3D p3d in dataset.part3DList)
                        {
                            foreach(Pin p in p3d.Pins)
                            {
                                if(p.Pos.DistanceTo(e.Point) <= minDis)
                                {
                                    minDis = p.Pos.DistanceTo(e.Point);
                                    idx = dataset.part3DList.IndexOf(p3d);
                                    closerStartPtIdx = p3d.Pins.IndexOf(p);
                                }
                            }
                        }

                        startPart = dataset.part3DList.ElementAt(idx);

                        Vector3d dir = startPart.Normal;
                        double extLen = 1;
                        Point3d extStart0 = dataset.part3DList.ElementAt(idx).Pins.ElementAt(closerStartPtIdx).Pos - extLen * dir;
                        Vector3d pt_dir = dataset.part3DList.ElementAt(idx).Pins.ElementAt(closerStartPtIdx).Pos - startPart.PartPos;
                        Plane prt_bottom_plane = new Plane(extStart0, dir);
                        Point3d pt_dir_proj = prt_bottom_plane.ClosestPoint(extStart0 + pt_dir);
                        Vector3d pt_dir_inplane = pt_dir_proj - extStart0;
                        Point3d extStart1 = pt_dir_inplane / pt_dir_inplane.Length * extLen + extStart0;

                        Point3d boxC = ((extStart0 + extStart1) / 2 + extStart0) / 2;
                        Vector3d boxDir = dataset.part3DList.ElementAt(idx).Pins.ElementAt(closerStartPtIdx).Pos - extStart0;
                        startPoint = boxC + boxDir / 2;
                    }
                    break;
                case 2:
                    {
                        double minDis = double.MaxValue;
                        int idx = -1;

                        foreach (Part3DP p3dp in dataset.part3DPList)
                        {
                            if(p3dp.PartPos.DistanceTo(e.Point) <= minDis)
                            {
                                minDis = p3dp.PartPos.DistanceTo(e.Point);
                                idx = dataset.part3DPList.IndexOf(p3dp);
                            }
                        }

                        startPart3D = dataset.part3DPList.ElementAt(idx);
                        startPoint = dataset.part3DPList.ElementAt(idx).PartPos;
                    }
                    break;
                case 3:
                    {
                        double minDis = double.MaxValue;
                        int idx = -1;
                        Point3d startPtTest = new Point3d();

                        foreach(Trace trc in dataset.deployedTraces)
                        {

                            foreach(Guid t_id in trc.TrID)
                            {
                                Brep trcBrep = (Brep)myDoc.Objects.Find(t_id).Geometry;

                                if (trcBrep.ClosestPoint(e.Point).DistanceTo(e.Point) <= minDis)
                                {
                                    startPtTest = trcBrep.ClosestPoint(e.Point);
                                    minDis = trcBrep.ClosestPoint(e.Point).DistanceTo(e.Point);
                                    idx = dataset.deployedTraces.IndexOf(trc);
                                }
                            }
                        }

                        startTrace = dataset.deployedTraces.ElementAt(idx);

                        //Brep targetBrep = (Brep)myDoc.Objects.Find(dataset.deployedTraces.ElementAt(idx).TrID).Geometry;
                        //startPoint = targetBrep.ClosestPoint(e.Point);
                        startPoint = startPtTest;
                    }
                    break;
                default:break;
            }
        }

        private void GetKey(int key)
        {
            if (key == 49) //1
            {
                if (isStartSelect)
                    startType = 1;

                if (isEndSelect)
                    endType = 1;
            }
            else if (key == 50)//P or p
            {
                if (isStartSelect)
                    startType = 2;

                if (isEndSelect)
                    endType = 2;
            }
            else if (key == 51)//T or t
            {
                if (isStartSelect)
                    startType = 3;

                if (isEndSelect)
                    endType = 3;
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
            get { return new Guid("6c774572-369e-48fa-b8ae-7834ca0371ed"); }
        }
    }
}