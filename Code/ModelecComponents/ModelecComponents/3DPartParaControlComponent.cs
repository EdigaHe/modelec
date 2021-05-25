using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class _3DPartParaControlComponent : GH_Component
    {
        SharedData dataset;
        bool testBtnClick;
        RhinoDoc myDoc;
        TargetBody currBody;
        PrinterProfile printerProfile;

        /// <summary>
        /// Initializes a new instance of the _3DPartParaControlComponent class.
        /// </summary>
        public _3DPartParaControlComponent()
          : base("3DPartParaControlComponent", "3DPParaCon",
              "Adjust the parameter of the 3D-printed part",
              "Modelec", "3DP-Control")
        {
            dataset = SharedData.Instance;
            testBtnClick = false;
            currBody = TargetBody.Instance;
            myDoc = RhinoDoc.ActiveDoc;
            printerProfile = PrinterProfile.Instance;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Level", "L", "the level of the 3D-printed part parameter value", GH_ParamAccess.item);
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
            int readVal = -1;
            bool isOn = false;

            if (!DA.GetData(0, ref readVal))
                return;

            if(readVal != dataset.currPart3DP.Level)
            {
                isOn = true;
            }

            if (isOn)
            {
                if (!dataset.currPart3DP.PartName.Equals("null"))
                {
                    switch (dataset.currPart3DP.Type)
                    {
                        case 1:
                            {
                                // change the dot size

                                double r = (readVal - 1) + 2;
                                Point3d pos = dataset.currPart3DP.PartPos;

                                Sphere canSphere = new Sphere(pos, r);
                                Brep canSphereBrep = canSphere.ToBrep();
                                Brep bodyDup = currBody.bodyBrep.DuplicateBrep();

                                var finalDotBreps = Brep.CreateBooleanIntersection(canSphereBrep, bodyDup, myDoc.ModelAbsoluteTolerance);
                                Brep finalDotBrep;

                                if (finalDotBreps == null)
                                {
                                    finalDotBrep = canSphereBrep;
                                }
                                else
                                {
                                    if (finalDotBreps.Count() <= 0)
                                        finalDotBrep = canSphereBrep;
                                    else
                                        finalDotBrep = finalDotBreps[0];
                                }

                                Guid finalDotID = myDoc.Objects.AddBrep(finalDotBrep);
                                myDoc.Objects.Delete(dataset.currPart3DP.PrtID, true);

                                myDoc.Views.Redraw();

                                // udpate the currently selected 3D-printed part
                                dataset.currPart3DP.PrtID = finalDotID;
                                dataset.currPart3DP.Depth = r;
                                dataset.currPart3DP.Height = r;
                                dataset.currPart3DP.Length = r;
                                dataset.currPart3DP.Level = readVal;

                                // update the corresponding part in the 3D-printed part list
                                foreach (Part3DP p3d in dataset.part3DPList)
                                {
                                    if (p3d.PartName.Equals(dataset.currPart3DP.PartName))
                                    {
                                        int idx = dataset.part3DPList.IndexOf(p3d);

                                        dataset.part3DPList.ElementAt(idx).PrtID = finalDotID;
                                        dataset.part3DPList.ElementAt(idx).Height = r;
                                        dataset.part3DPList.ElementAt(idx).Depth = r;
                                        dataset.part3DPList.ElementAt(idx).Length = r;
                                        dataset.part3DPList.ElementAt(idx).Level = readVal;
                                    }
                                }
                            }
                            break;
                        case 2:
                            {
                                // change the touch area size
                                double r = (readVal - 1) + 2;
                                Point3d pos = dataset.currPart3DP.PartPos;
                                Point3d cen = currBody.bodyBrep.ClosestPoint(pos);

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
                                myDoc.Objects.Delete(dataset.currPart3DP.PrtID, true);
                                myDoc.Views.Redraw();

                                // update the currently selected 3D-printed part
                                dataset.currPart3DP.PrtID = areaID;
                                dataset.currPart3DP.Level = readVal;
                                dataset.currPart3DP.Depth = r;
                                dataset.currPart3DP.Height = r;
                                dataset.currPart3DP.Length = r;
                                dataset.currPart3DP.PartPos = cen;
                                dataset.currPart3DP.Pins.Clear();
                                dataset.currPart3DP.Pins.Add(new Pin("pin", 1, cen.X, cen.Y, cen.Z));

                                // update the corresponding part in the list
                                foreach (Part3DP p3d in dataset.part3DPList)
                                {
                                    if (p3d.PartName.Equals(dataset.currPart3DP.PartName))
                                    {
                                        int idx = dataset.part3DPList.IndexOf(p3d);

                                        dataset.part3DPList.ElementAt(idx).PrtID = areaID;
                                        dataset.part3DPList.ElementAt(idx).Height = r;
                                        dataset.part3DPList.ElementAt(idx).Depth = r;
                                        dataset.part3DPList.ElementAt(idx).Length = r;
                                        dataset.part3DPList.ElementAt(idx).Level = readVal;
                                        dataset.part3DPList.ElementAt(idx).PartPos = cen;
                                        dataset.part3DPList.ElementAt(idx).Pins.Clear();
                                        dataset.part3DPList.ElementAt(idx).Pins.Add(new Pin("pin", 1, cen.X, cen.Y, cen.Z));
                                    }
                                }
                            }
                            break;
                        case 3:
                            {
                                // change the resistor size
                                double r_val = 10;
                                r_val = readVal; // placeholder
                                dataset.currPart3DP.Height = Double.Parse(printerProfile.Trace_min_x.Find(p => p.Cond.Equals("5+mm")).Value) / 1000;
                                dataset.currPart3DP.Depth = Double.Parse(printerProfile.Trace_min_y.Find(p => p.Cond.Equals("5+mm")).Value) / 1000;
                                dataset.currPart3DP.Length = r_val / (printerProfile.Trace_resistivity * 1000) * dataset.currPart3DP.Height * dataset.currPart3DP.Depth;

                                Point3d cen = dataset.currPart3DP.PartPos;

                                Plane boxBasePlane = new Plane(cen, new Vector3d(0, 0, 1));
                                List<Point3d> boxPts = new List<Point3d>();
                                boxPts.Add(new Point3d(cen.X - dataset.currPart3DP.Height / 2, cen.Y - dataset.currPart3DP.Depth / 2, cen.Z - dataset.currPart3DP.Length / 2));
                                boxPts.Add(new Point3d(cen.X + dataset.currPart3DP.Height / 2, cen.Y - dataset.currPart3DP.Depth / 2, cen.Z - dataset.currPart3DP.Length / 2));
                                boxPts.Add(new Point3d(cen.X - dataset.currPart3DP.Height / 2, cen.Y + dataset.currPart3DP.Depth / 2, cen.Z - dataset.currPart3DP.Length / 2));
                                boxPts.Add(new Point3d(cen.X + dataset.currPart3DP.Height / 2, cen.Y + dataset.currPart3DP.Depth / 2, cen.Z - dataset.currPart3DP.Length / 2));
                                boxPts.Add(new Point3d(cen.X - dataset.currPart3DP.Height / 2, cen.Y - dataset.currPart3DP.Depth / 2, cen.Z + dataset.currPart3DP.Length / 2));
                                boxPts.Add(new Point3d(cen.X + dataset.currPart3DP.Height / 2, cen.Y - dataset.currPart3DP.Depth / 2, cen.Z + dataset.currPart3DP.Length / 2));
                                boxPts.Add(new Point3d(cen.X - dataset.currPart3DP.Height / 2, cen.Y + dataset.currPart3DP.Depth / 2, cen.Z + dataset.currPart3DP.Length / 2));
                                boxPts.Add(new Point3d(cen.X + dataset.currPart3DP.Height / 2, cen.Y + dataset.currPart3DP.Depth / 2, cen.Z + dataset.currPart3DP.Length / 2));
                                Box resBox = new Box(boxBasePlane, boxPts);
                                Guid resistorID = myDoc.Objects.AddBox(resBox);

                                myDoc.Objects.Delete(dataset.currPart3DP.PrtID, true);
                                myDoc.Views.Redraw();

                                // update the currently selected 3D-printed part

                                dataset.currPart3DP.PrtID = resistorID;
                                dataset.currPart3DP.Level = readVal;
                                dataset.currPart3DP.Depth = dataset.currPart3DP.Depth;
                                dataset.currPart3DP.Height = dataset.currPart3DP.Height;
                                dataset.currPart3DP.Length = dataset.currPart3DP.Length;
                                dataset.currPart3DP.Pins.Clear();
                                dataset.currPart3DP.Pins.Add(new Pin("pin1", 1, cen.X, cen.Y, cen.Z - dataset.currPart3DP.Length / 2));
                                dataset.currPart3DP.Pins.Add(new Pin("pin2", 2, cen.X, cen.Y, cen.Z + dataset.currPart3DP.Length / 2));


                                // update the corresponding part in the list
                                foreach (Part3DP p3d in dataset.part3DPList)
                                {
                                    if (p3d.PartName.Equals(dataset.currPart3DP.PartName))
                                    {
                                        int idx = dataset.part3DPList.IndexOf(p3d);

                                        dataset.part3DPList.ElementAt(idx).PrtID = resistorID;
                                        dataset.part3DPList.ElementAt(idx).Height = dataset.currPart3DP.Height;
                                        dataset.part3DPList.ElementAt(idx).Depth = dataset.currPart3DP.Depth;
                                        dataset.part3DPList.ElementAt(idx).Length = dataset.currPart3DP.Length;
                                        dataset.part3DPList.ElementAt(idx).Level = readVal;
                                        dataset.part3DPList.ElementAt(idx).Pins.Clear();
                                        dataset.part3DPList.ElementAt(idx).Pins.Add(new Pin("pin1", 1, cen.X, cen.Y, cen.Z - dataset.currPart3DP.Length / 2));
                                        dataset.part3DPList.ElementAt(idx).Pins.Add(new Pin("pin2", 2, cen.X, cen.Y, cen.Z + dataset.currPart3DP.Length / 2));
                                    }
                                }
                            }
                            break;
                        default: break;
                    }
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
            get { return new Guid("81948070-e74d-49dc-abea-c79dcf6a8ef5"); }
        }
    }
}