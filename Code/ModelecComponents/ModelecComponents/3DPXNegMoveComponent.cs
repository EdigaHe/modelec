using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Timers;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class _3DPXNegMoveComponent : GH_Component
    {
        SharedData dataset;
        bool testBtnClick;
        RhinoDoc myDoc;
        TargetBody currBody;
        /// <summary>
        /// Initializes a new instance of the _3DPXNegMoveComponent class.
        /// </summary>
        public _3DPXNegMoveComponent()
          : base("3DPrintedPartMoveNegX", "PPMoveXN",
              "Move the 3D-printed part toward the negative direction of the X axis",
              "Modelec", "3DP-Control")
        {
            dataset = SharedData.Instance;
            testBtnClick = false;
            currBody = TargetBody.Instance;
            myDoc = RhinoDoc.ActiveDoc;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("AddBtnClicked", "BC", "click the button to add the selected 3D-printed part", GH_ParamAccess.item);
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
            bool isOn = false;

            if (!DA.GetData(0, ref btnClick))
                return;

            #region read the button click 
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

            if (isOn && !dataset.currPart3DP.PartName.Equals("null"))
            {
                #region Step 0: locate in the proper layer

                if (myDoc.Layers.FindName("3DParts") == null)
                {
                    // create a new layer named "Parts"
                    string layer_name = "3DParts";
                    Layer p_ly = myDoc.Layers.CurrentLayer;

                    Layer new_ly = new Layer();
                    new_ly.Name = layer_name;
                    new_ly.ParentLayerId = p_ly.ParentLayerId;
                    new_ly.Color = Color.FromArgb(61, 61, 61);

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

                #region Step 1: get the currently selected object

                Guid currID = dataset.currPart3DP.PrtID;

                #endregion

                #region Step 2: execute the movement
                Transform m = Transform.Translation(new Vector3d(-1, 0, 0));

                switch (dataset.currPart3DP.Type)
                {
                    case 1:
                        {
                            // dot can move to anywhere as long as it is in or on the object
                            Point3d predictPos = dataset.currPart3DP.PartPos;
                            predictPos.Transform(m);

                            if (currBody.bodyBrep.IsPointInside(predictPos, myDoc.ModelAbsoluteTolerance, false))
                            {
                                // update the part position
                                dataset.currPart3DP.PartPos = predictPos;


                                // update the 3D printed part's Guid
                                Guid newID = myDoc.Objects.Transform(currID, m, true);

                                // boolean intersect the new dot brep with the object brep
                                Brep curr3DPBrep = (Brep)myDoc.Objects.FindId(newID).Geometry;
                                Brep bodyDup = currBody.bodyBrep.DuplicateBrep();


                                var finalDotBreps = Brep.CreateBooleanIntersection(curr3DPBrep, bodyDup, myDoc.ModelAbsoluteTolerance);
                                Brep finalDotBrep;

                                if (finalDotBreps == null)
                                {
                                    finalDotBrep = curr3DPBrep;
                                }
                                else
                                {
                                    if (finalDotBreps.Count() <= 0)
                                        finalDotBrep = curr3DPBrep;
                                    else
                                        finalDotBrep = finalDotBreps[0];
                                }

                                Guid finalDotID = myDoc.Objects.AddBrep(finalDotBrep);
                                myDoc.Objects.Delete(newID, true);
                                myDoc.Views.Redraw();

                                dataset.currPart3DP.PrtID = finalDotID;
                                dataset.currPart3DP.Pins.Clear();
                                dataset.currPart3DP.Pins.Add(new Pin("pin", 1, predictPos.X, predictPos.Y, predictPos.Z));

                                // update the 3D-printed part in the part list

                                foreach (Part3DP p3d in dataset.part3DPList)
                                {
                                    if (p3d.PartName.Equals(dataset.currPart3DP.PartName))
                                    {
                                        int idx = dataset.part3DPList.IndexOf(p3d);
                                        dataset.part3DPList.ElementAt(idx).PrtID = finalDotID;
                                        dataset.part3DPList.ElementAt(idx).PartPos = predictPos;
                                        dataset.part3DPList.ElementAt(idx).Pins.Clear();
                                        dataset.part3DPList.ElementAt(idx).Pins.Add(new Pin("pin", 1, predictPos.X, predictPos.Y, predictPos.Z));
                                    }
                                }

                            }
                            else{
                                // pop up the warning window "already on the boundary"
                                oobWindows oobWarningWin = new oobWindows();
                                oobWarningWin.Show();
                            }

                        }break;
                    case 2:
                        {
                            // area can only move on the surface of the object
                            Point3d predictPos = dataset.currPart3DP.PartPos;
                            predictPos.Transform(m);

                            Point3d cen = currBody.bodyBrep.ClosestPoint(predictPos);

                            //if (currBody.bodyBrep.IsPointInside(predictPos, myDoc.ModelAbsoluteTolerance, false))
                            //{
                                
                                double r = dataset.currPart3DP.Depth;

                                Sphere rangeSphere = new Sphere(cen, r);
                                Brep rangBrep = rangeSphere.ToBrep();

                                Brep dupBody = currBody.bodyBrep.DuplicateBrep();

                                Brep[] outBlends;
                                Brep[] outWalls;
                                Brep[] dupOffsetBody = Brep.CreateOffsetBrep(dupBody, -1, false, true, myDoc.ModelRelativeTolerance, out outBlends, out outWalls);

                                Brep innerShell = dupOffsetBody[0];

                                Brep interBrep = Brep.CreateBooleanIntersection(dupBody, rangBrep, myDoc.ModelAbsoluteTolerance)[0];
                                Brep areaBrep = Brep.CreateBooleanDifference(interBrep, innerShell, myDoc.ModelAbsoluteTolerance)[0];

                                Guid areaID = myDoc.Objects.AddBrep(areaBrep);

                                // delete the original sphere

                                myDoc.Objects.Delete(dataset.currPart3DP.PrtID, true);

                                myDoc.Views.Redraw();

                                dataset.currPart3DP.Depth = r;
                                dataset.currPart3DP.Height = r;
                                dataset.currPart3DP.Length = r;
                                dataset.currPart3DP.PrtID = areaID;
                                dataset.currPart3DP.PartPos = cen;
                                dataset.currPart3DP.IsDeployed = true;
                                dataset.currPart3DP.Pins.Clear();
                                dataset.currPart3DP.Pins.Add(new Pin("pin", 1, cen.X, cen.Y, cen.Z));

                                // update the 3D-printed part in the part list


                                foreach (Part3DP p3d in dataset.part3DPList)
                                {
                                    if (p3d.PartName.Equals(dataset.currPart3DP.PartName))
                                    {
                                        int idx = dataset.part3DPList.IndexOf(p3d);
                                        dataset.part3DPList.ElementAt(idx).PrtID = areaID;
                                        dataset.part3DPList.ElementAt(idx).PartPos = cen;
                                        dataset.part3DPList.ElementAt(idx).Pins.Clear();
                                        dataset.part3DPList.ElementAt(idx).Pins.Add(new Pin("pin", 1, cen.X, cen.Y, cen.Z));
                                    }
                                }

                            //}
                            //else
                            //{
                            //    // pop up the warning window "already on the boundary"
                            //    oobWindows oobWarningWin = new oobWindows();
                            //    oobWarningWin.Show();
                            //}

                        }
                        break;
                    case 3:
                        {
                            // resistor can only move around within the object body
                            Point3d predictPos = dataset.currPart3DP.PartPos;
                            predictPos.Transform(m);

                            // test if the boundingbox of the resistor is not fully inside the body brep

                            Brep resistorBrep = (Brep)myDoc.Objects.FindId(dataset.currPart3DP.PrtID).Geometry;
                            resistorBrep.Transform(m);
                            BoundingBox rBBox = resistorBrep.GetBoundingBox(true);

                            bool isInside = true;

                            foreach(Point3d p in rBBox.GetCorners())
                            {
                                if(!currBody.bodyBrep.IsPointInside(p, myDoc.ModelAbsoluteTolerance, false))
                                {
                                    isInside = false;
                                    break;
                                }
                            }

                            if (isInside)
                            {
                                Point3d cen = predictPos;
                                double length = dataset.currPart3DP.Length;
                                double depth = dataset.currPart3DP.Depth;
                                double height = dataset.currPart3DP.Height;

                                Point3d pt0 = new Point3d(cen.X - height / 2, cen.Y - depth / 2, cen.Z - length / 2);
                                Point3d pt1 = new Point3d(cen.X + height / 2, cen.Y + depth / 2, cen.Z + length / 2);
                                BoundingBox box = new BoundingBox(pt0, pt1);
                                Brep brep = box.ToBrep();

                                Guid resistorID = myDoc.Objects.AddBrep(brep);

                                myDoc.Objects.Delete(dataset.currPart3DP.PrtID, true);

                                myDoc.Views.Redraw();

                                dataset.currPart3DP.Depth = depth;
                                dataset.currPart3DP.Height = height;
                                dataset.currPart3DP.Length = length;
                                dataset.currPart3DP.PrtID = resistorID;

                                dataset.currPart3DP.PartPos = cen;
                                dataset.currPart3DP.IsDeployed = true;
                                dataset.currPart3DP.Pins.Clear();
                                dataset.currPart3DP.Pins.Add(new Pin("pin1", 1, cen.X, cen.Y, cen.Z - length / 2));
                                dataset.currPart3DP.Pins.Add(new Pin("pin2", 2, cen.X, cen.Y, cen.Z + length / 2));

                                // update the 3D-printed part in the part list

                                foreach (Part3DP p3d in dataset.part3DPList)
                                {
                                    if (p3d.PartName.Equals(dataset.currPart3DP.PartName))
                                    {
                                        int idx = dataset.part3DPList.IndexOf(p3d);
                                        dataset.part3DPList.ElementAt(idx).PrtID = resistorID;
                                        dataset.part3DPList.ElementAt(idx).PartPos = cen;
                                        dataset.part3DPList.ElementAt(idx).Pins.Clear();
                                        dataset.part3DPList.ElementAt(idx).Pins.Add(new Pin("pin1", 1, cen.X, cen.Y, cen.Z - length / 2));
                                        dataset.part3DPList.ElementAt(idx).Pins.Add(new Pin("pin2", 2, cen.X, cen.Y, cen.Z + length / 2));
                                    }
                                }
                            }
                            else
                            {
                                // pop up the warning window "already on the boundary"
                                oobWindows oobWarningWin = new oobWindows();
                                oobWarningWin.Show();
                            }
                        }
                        break;
                    default:break;
                }
                #endregion
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
            get { return new Guid("57efc420-0057-4d8b-9d41-f502368f5d45"); }
        }
    }
}