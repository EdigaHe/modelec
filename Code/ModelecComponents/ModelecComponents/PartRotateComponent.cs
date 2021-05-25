using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class PartRotateComponent : GH_Component
    {
        double oldAngle;
        RhinoDoc myDoc;
        SharedData dataset;
        TargetBody currBody;

        /// <summary>
        /// Initializes a new instance of the PartRotateComponent class.
        /// </summary>
        public PartRotateComponent()
          : base("PartRotateComponent", "PRot",
              "Rotate the part",
              "ModElec", "PartOperations")
        {
            oldAngle = 0;
            myDoc = RhinoDoc.ActiveDoc;
            dataset = SharedData.Instance;
            currBody = TargetBody.Instance;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("RotatePartAngle", "RA", "Rotational angle", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Part3D", "P3D", "The ModElec part unit", GH_ParamAccess.item);
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
            string readAngleString = "";
            double readAngle = 0;
            double diff = 0;
            bool toExePrtRotateSet = false;
            double partAngle = 0;
            //Part3D selPrt = new Part3D();
            Part3D selPrt = dataset.currPart3D;

            #region read the button click and decide whether execute the part rotation or not
            if (!DA.GetData(0, ref readAngleString))
                return;

            if (selPrt == null) return;
            //if (!DA.GetData(1, ref selPrt))
            //    return;

            partAngle = selPrt.RotationAngle;

            try
            {
                readAngle = double.Parse(readAngleString);
            }
            catch { return; }
            

            if (readAngle != partAngle)
            {
                toExePrtRotateSet = true;
                diff = readAngle - partAngle;
                partAngle = readAngle;
            }
            else 
            {
                toExePrtRotateSet = false;
            }

            #endregion

            if (toExePrtRotateSet)
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

                    #region rotate the part in place

                    Vector3d rotationAxis = dataset.part3DList.ElementAt(prt_idx).Normal;

                    dataset.part3DList.ElementAt(prt_idx).PartPos = myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center;

                    //Line testln = new Line(dataset.part3DList.ElementAt(prt_idx).PartPos, dataset.part3DList.ElementAt(prt_idx).PartPos + rotationAxis);
                    //Curve testCrv = testln.ToNurbsCurve();
                    //myDoc.Objects.AddCurve(testCrv);
                    //myDoc.Views.Redraw();

                    //Transform rotTrans = Transform.Rotation(diff / 180 * Math.PI, rotationAxis, myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center);
                    Transform rotTrans = Transform.Rotation(diff / 180 * Math.PI, rotationAxis, dataset.part3DList.ElementAt(prt_idx).PartPos);

                    Guid newID = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, rotTrans, true);
                    dataset.part3DList.ElementAt(prt_idx).PrtID = newID;

                    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                    {
                        Point3d tempPt = p.Pos;
                        tempPt.Transform(rotTrans);
                        int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = tempPt;
                    }
                    dataset.part3DList.ElementAt(prt_idx).RotationAngle = readAngle;
                    myDoc.Views.Redraw();

                    dataset.currPart3D = dataset.part3DList.ElementAt(prt_idx);
                    #endregion

                    #region rotate the part socket in place
                    int socketIdx = -1;
                    foreach (partSocket prtS in currBody.partSockets)
                    {
                        if (prtS.PrtName.Equals(selPrt.PartName))
                        {
                            socketIdx = currBody.partSockets.IndexOf(prtS);
                        }
                    }

                    Brep prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(rotTrans);
                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    //myDoc.Objects.AddBrep(currBody.partSockets.ElementAt(socketIdx).SocketBrep);
                    //myDoc.Views.Redraw();
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
            get { return new Guid("2e1af936-3b1d-4254-aec5-84550b963b5a"); }
        }
    }
}