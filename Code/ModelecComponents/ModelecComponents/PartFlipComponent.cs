using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class PartFlipComponent : GH_Component
    {
        bool testBtnClick;
        int count;
        RhinoDoc myDoc;
        SharedData dataset;
        TargetBody currBody;

        /// <summary>
        /// Initializes a new instance of the PartFlipComponent class.
        /// </summary>
        public PartFlipComponent()
          : base("PartFlipComponent", "PFlip",
              "Flip the part",
              "ModElec", "PartOperations")
        {
            testBtnClick = false;
            count = 0;
            myDoc = RhinoDoc.ActiveDoc;
            dataset = SharedData.Instance;
            currBody = TargetBody.Instance;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("FlipPartBtnClicked", "BC", "flip the part", GH_ParamAccess.item);
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
            bool btnClick = false;
            bool toExePrtFlipSet = false;
            //Part3D selPrt = new Part3D();
            Part3D selPrt = dataset.currPart3D;

            #region read the button click and decide whether execute the circuit load or not
            if (!DA.GetData(0, ref btnClick))
                return;

            //if (!DA.GetData(1, ref selPrt))
            //    return;

            if (!btnClick && testBtnClick && selPrt.PartName != "")
            {
                toExePrtFlipSet = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                toExePrtFlipSet = true;
                testBtnClick = true;
            }

            #endregion

            if (toExePrtFlipSet)
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

                    if (testBtnClick && dataset.part3DList.ElementAt(prt_idx).IsFlipped) return;
                    if (!testBtnClick && !dataset.part3DList.ElementAt(prt_idx).IsFlipped) return;

                    #region flip the part in place
                    Point3d prt_pos = dataset.part3DList.ElementAt(prt_idx).PartPos;

                    Vector3d reverseNormalVector = dataset.part3DList.ElementAt(prt_idx).Normal * (-1);
                    //dataset.part3DList.ElementAt(prt_idx).PartPos = myDoc.Objects.Find(dataset.part3DList.ElementAt(prt_idx).PrtID).Geometry.GetBoundingBox(true).Center;
                    Transform flipTrans = Transform.Rotation(dataset.part3DList.ElementAt(prt_idx).Normal, reverseNormalVector, prt_pos);
                    
                    Guid newID = myDoc.Objects.Transform(dataset.part3DList.ElementAt(prt_idx).PrtID, flipTrans, true);
                    dataset.part3DList.ElementAt(prt_idx).PrtID = newID;
                    dataset.part3DList.ElementAt(prt_idx).IsFlipped = !dataset.part3DList.ElementAt(prt_idx).IsFlipped;

                    foreach (Pin p in dataset.part3DList.ElementAt(prt_idx).Pins)
                    {
                        Point3d tempPt = p.Pos;
                        tempPt.Transform(flipTrans);
                        int idx = dataset.part3DList.ElementAt(prt_idx).Pins.IndexOf(p);
                        dataset.part3DList.ElementAt(prt_idx).Pins.ElementAt(idx).Pos = tempPt;
                    }

                    #endregion

                    #region flip the part socket in place
                    int socketIdx = -1;
                    foreach(partSocket prtS in currBody.partSockets)
                    {
                        if (prtS.PrtName.Equals(selPrt.PartName))
                        {
                            socketIdx = currBody.partSockets.IndexOf(prtS);
                        }
                    }

                    Brep prtBrep = currBody.partSockets.ElementAt(socketIdx).SocketBrep;
                    prtBrep.Transform(flipTrans);
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
            get { return new Guid("e97092bb-fdaa-4e09-a5b7-2003911eebee"); }
        }
    }
}