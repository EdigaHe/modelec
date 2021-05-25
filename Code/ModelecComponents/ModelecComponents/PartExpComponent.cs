using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class PartExpComponent : GH_Component
    {
        double oldExposure;
        RhinoDoc myDoc;
        SharedData dataset;
        TargetBody currBody;

        /// <summary>
        /// Initializes a new instance of the PartExpComponent class.
        /// </summary>
        public PartExpComponent()
          : base("PartExpComponent", "PExp",
              "Expose the part",
              "ModElec", "PartOperations")
        {
            oldExposure = 0;
            myDoc = RhinoDoc.ActiveDoc;
            dataset = SharedData.Instance;
            currBody = TargetBody.Instance;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("PartExposure", "PE", "Part exposure", GH_ParamAccess.item);
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
            string readExpString = "";
            double readExposureLevel = 0;
            double partExp = 0;
            bool toExePrtExposureSet = false;
            //Part3D selPrt = new Part3D();
            Part3D selPrt = dataset.currPart3D;

            #region read the button click and decide whether execute the exposure setting or not
            if (!DA.GetData(0, ref readExpString))
                return;

            if (selPrt == null) return;
            try
            {
                readExposureLevel = double.Parse(readExpString);
                if (readExposureLevel > 1)
                {
                    readExposureLevel = readExposureLevel / 100;
                }
            }
            catch { return; }
            

            partExp = selPrt.ExposureLevel;
            //if (!DA.GetData(1, ref selPrt))
            //    return;

            if (readExposureLevel != partExp)
            {
                toExePrtExposureSet = true;
                oldExposure = readExposureLevel;
            }
            else
            {
                toExePrtExposureSet = false;
            }

            #endregion


            if (toExePrtExposureSet)
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

                    #region adjust the exposure of the part in place

                    Vector3d expAxis = dataset.part3DList.ElementAt(prt_idx).Normal;

                    //dataset.part3DList.ElementAt(prt_idx).PartPos = myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center;

                    Point3d old_prt_pos = dataset.part3DList.ElementAt(prt_idx).PartPos;
                    
                    double oldPartExp = dataset.part3DList.ElementAt(prt_idx).ExposureLevel;
                    double partH = dataset.part3DList.ElementAt(prt_idx).Height;

                    // translate back to fully hidden
                    Transform resetTrans = Transform.Translation(expAxis * (-1) * oldPartExp * partH);
                    Guid newIDTemp = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, resetTrans, true);
                    old_prt_pos.Transform(resetTrans);

                    // translate the part to the new exposure level
                    Transform setExpTrans = Transform.Translation(expAxis * readExposureLevel * partH);
                    Guid newID = myDoc.Objects.Transform(newIDTemp, setExpTrans, true);
                    old_prt_pos.Transform(setExpTrans);

                    dataset.part3DList.ElementAt(prt_idx).PrtID = newID;
                    dataset.part3DList.ElementAt(prt_idx).PartPos = old_prt_pos;

                    // Update the pin info
                    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                    {
                        Point3d tempPt = p.Pos;
                        tempPt.Transform(resetTrans);
                        tempPt.Transform(setExpTrans);

                        int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = tempPt;
                    }

                    dataset.part3DList.ElementAt(prt_idx).ExposureLevel = readExposureLevel;
                    myDoc.Views.Redraw();

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
                    prtBrep.Transform(resetTrans);
                    prtBrep.Transform(setExpTrans);

                    currBody.partSockets.ElementAt(socketIdx).SocketBrep = prtBrep;

                    //myDoc.Objects.AddBrep(currBody.partSockets.ElementAt(socketIdx).SocketBrep);
                    //myDoc.Views.Redraw();
                    #endregion

                    dataset.currPart3D = dataset.part3DList.ElementAt(prt_idx);
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
            get { return new Guid("8e8035d4-28b6-49f3-981e-ab464ddfd69c"); }
        }
    }
}