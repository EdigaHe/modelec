using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class UpdateTraceListComponent : GH_Component
    {
        SharedData dataset;
        bool listUpdatedAddAuto;
        bool listUpdatedAddMan;
        bool listUpdatedDel;

        bool old_updated;

        /// <summary>
        /// Initializes a new instance of the Update3DPListComponent class.
        /// </summary>
        public UpdateTraceListComponent()
          : base("UpdateTraceListComponent", "UpdateTraceList",
              "Update the trace list",
              "Modelec", "TraceOperations")
        {
            dataset = SharedData.Instance;
            listUpdatedAddAuto = false;
            listUpdatedDel = false;
            listUpdatedAddMan = false;
            old_updated = false;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("AddAutoListUpdated", "ListUp1", "the list is updated by adding auto-generated traces", GH_ParamAccess.item);
            pManager.AddBooleanParameter("AddManualListUpdated", "ListUp2", "the list is updated by adding manual traces", GH_ParamAccess.item);
            pManager.AddBooleanParameter("DelListUpdated", "ListUp3", "the list is updated by deleting", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("TraceList", "TraceList", "List of generated traces", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        { 
            bool isOn = true;
            List<string> output = new List<string>();

            //if (!DA.GetData(0, ref listUpdatedAddAuto))
            //    return;
            //if (!DA.GetData(1, ref listUpdatedAddMan))
            //    return;
            //if (!DA.GetData(2, ref listUpdatedDel))
            //    return;

            if (isOn)
            {
                foreach (Trace tr in dataset.deployedTraces)
                {
                    switch (tr.Type)
                    {
                        case 1: // auto
                        case 7: // manual
                            {
                                // electrical part to electrical part
                                // string con = tr.PrtName + "-" + tr.PrtPinName + "<-->" + tr.DesPrtName + "-" + tr.DesPinName;
                                string con = "";
                                foreach (TraceCluster trCluster in dataset.traceOracle)
                                {
                                    foreach (JumpWire jw in trCluster.Jumpwires)
                                    {
                                        if (jw.IsDeployed && (jw.StartPrtName.Equals(tr.PrtName) || jw.EndPrtName.Equals(tr.PrtName)))
                                        {
                                            con = jw.StartPrtName + "-" + jw.StartPinName + "<-->" + jw.EndPrtName + "-" + jw.EndPinName;

                                            bool isExist = false;

                                            foreach(string c in output)
                                            {
                                                int idx_splitor = c.IndexOf('<');
                                                string fromDesc = c.Substring(0, idx_splitor);
                                                string toDesc = c.Substring(idx_splitor + 4, c.Length - idx_splitor - 4);

                                                if(fromDesc.IndexOf('-') != -1 && toDesc.IndexOf('-') != -1)
                                                {
                                                    string con1 = jw.EndPrtName + "-" + jw.EndPinName + "<-->" + jw.StartPrtName + "-" + jw.StartPinName;

                                                    if (c.Equals(con) || c.Equals(con1))
                                                        isExist = true;
                                                }
                                            }

                                            if (!isExist)
                                                output.Add(con);
                                        }
                                    }
                                }

                                
                            }
                            break;
                        case 2:
                            {
                                // 3D-printed part to 3D-printed part
                                string con = tr.PrtName + "<-->" + tr.DesPrtName;
                                output.Add(con);

                            }
                            break;
                        case 3:
                            {
                                // 3D-printed part to 3D-printed trace
                                string con = tr.PrtName + "<-->" + tr.DesPrtName;
                                output.Add(con);
                            }
                            break;
                        case 4:
                            {
                                // 3D-printed part to electrical part

                                bool isFoundA = false;
                                bool isFoundB = false;

                                foreach (Part3D p3d in dataset.part3DList)
                                {
                                    if (p3d.PartName.Substring(p3d.PartName.IndexOf('-') + 1, p3d.PartName.Length - p3d.PartName.IndexOf('-') - 1).Equals(tr.PrtName))
                                    {
                                        isFoundA = true;
                                        break;
                                    }

                                    if (p3d.PartName.Substring(p3d.PartName.IndexOf('-') + 1, p3d.PartName.Length - p3d.PartName.IndexOf('-') - 1).Equals(tr.DesPrtName))
                                    {
                                        isFoundB = true;
                                        break;
                                    }
                                }

                                if (isFoundA)
                                {
                                    string con = tr.PrtName + "-" + tr.PrtPinName + "<-->" + tr.DesPrtName;
                                    output.Add(con);
                                }

                                if (isFoundB)
                                {
                                    string con = tr.PrtName + "<-->" + tr.DesPrtName + "-" + tr.DesPinName;
                                    output.Add(con);
                                }

                            }break;
                        case 5:
                            {
                                // 3D-printed trace to 3D-printed trace
                                string con = tr.PrtName + "<-->" + tr.DesPrtName;
                                output.Add(con);
                            }
                            break;
                        case 6:
                            {
                                // 3D-printed trace to electrical part

                                bool isFoundA = false;
                                bool isFoundB = false;

                                foreach (Part3D p3d in dataset.part3DList)
                                {
                                    if (p3d.PartName.Substring(p3d.PartName.IndexOf('-') + 1, p3d.PartName.Length - p3d.PartName.IndexOf('-') - 1).Equals(tr.PrtName))
                                    {
                                        isFoundA = true;
                                        break;
                                    }

                                    if (p3d.PartName.Substring(p3d.PartName.IndexOf('-') + 1, p3d.PartName.Length - p3d.PartName.IndexOf('-') - 1).Equals(tr.DesPrtName))
                                    {
                                        isFoundB = true;
                                        break;
                                    }
                                }

                                if (isFoundA)
                                {
                                    string con = tr.PrtName + "-" + tr.PrtPinName + "<-->" + tr.DesPrtName;
                                    output.Add(con);
                                }

                                if (isFoundB)
                                {
                                    string con = tr.PrtName + "<-->" + tr.DesPrtName + "-" + tr.DesPinName;
                                    output.Add(con);
                                }

                            }
                            break;
                        default:break;
                    }
                }
            }
            else
            {
                output.Add(" ");
                output.Add(" ");
            }

            DA.SetDataList(0, output);
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
            get { return new Guid("7978e741-3939-4507-9ed7-e08644533ba7"); }
        }
    }
}