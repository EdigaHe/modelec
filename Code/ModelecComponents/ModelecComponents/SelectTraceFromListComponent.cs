using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class SelectTraceFromListComponent : GH_Component
    {
        SharedData dataset;
        bool testBtnClick;
        RhinoDoc myDoc;
        TargetBody currBody;
        /// <summary>
        /// Initializes a new instance of the SelectTraceFromListComponent class.
        /// </summary>
        public SelectTraceFromListComponent()
          : base("SelectTraceFromListComponent", "SelTraceFromList",
              "Select a trace from the list",
              "ModElec", "TraceOperations")
        {
            dataset = SharedData.Instance;
            testBtnClick = false;
            myDoc = RhinoDoc.ActiveDoc;
            currBody = TargetBody.Instance;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("TraceDesp", "TD", "The description of the connected parts", GH_ParamAccess.item);
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
            string readTraceDesc = "";

            if (!DA.GetData(0, ref readTraceDesc))
                return;

            if (!readTraceDesc.Equals(""))
            {
                List<Guid> highlightedGuids = new List<Guid>();

                // parse the description
                int idx_splitor = readTraceDesc.IndexOf('<');
                string fromDesc = readTraceDesc.Substring(0, idx_splitor);
                string toDesc = readTraceDesc.Substring(idx_splitor + 4, readTraceDesc.Length - idx_splitor - 4);

                if(fromDesc.IndexOf('-') != -1 && !fromDesc.Substring(0,3).Equals("Tra"))
                {
                    // the from point has pin info
                    string prtName = fromDesc.Substring(0, fromDesc.IndexOf('-'));
                    string pinName = fromDesc.Substring(fromDesc.IndexOf('-') + 1, fromDesc.Length - fromDesc.IndexOf('-') - 1);

                    Trace selectedTrace = dataset.deployedTraces.Find(x => x.PrtName.Equals(prtName) && x.PrtPinName.Equals(pinName));
                    foreach(Guid tr_id in selectedTrace.TrID)
                    {
                        highlightedGuids.Add(tr_id);
                    }
                    
                    if (toDesc.IndexOf('-') != -1 && !toDesc.Substring(0, 3).Equals("Tra"))
                    {
                        // the to point has pin info
                        string prtToName = toDesc.Substring(0, toDesc.IndexOf('-'));
                        string pinToName = toDesc.Substring(toDesc.IndexOf('-') + 1, toDesc.Length - toDesc.IndexOf('-') - 1);

                        Trace selectedToTrace = dataset.deployedTraces.Find(x => x.PrtName.Equals(prtToName) && x.PrtPinName.Equals(pinToName));

                        foreach(Guid tr_id in selectedToTrace.TrID)
                        {
                            highlightedGuids.Add(tr_id);
                        }  
                    }
                    else
                    {
                        Trace selectedTraceSpecial = dataset.deployedTraces.Find(x => (x.PrtName.Equals(prtName) && x.PrtPinName.Equals(pinName) && x.DesPrtName.Equals(toDesc)) ||
                                                                                    (x.DesPrtName.Equals(prtName) && x.DesPinName.Equals(pinName) && x.PrtName.Equals(toDesc)));

                        foreach(Guid tr_id in selectedTraceSpecial.TrID)
                        {
                            highlightedGuids.Add(tr_id);
                        }
                        
                    }
                }
                else
                {
                    if (toDesc.IndexOf('-') != -1 && !toDesc.Substring(0, 3).Equals("Tra"))
                    {
                        // the to point has pin info
                        string prtToName = toDesc.Substring(0, toDesc.IndexOf('-'));
                        string pinToName = toDesc.Substring(toDesc.IndexOf('-') + 1, toDesc.Length - toDesc.IndexOf('-') - 1);

                        Trace selectedTraceSpecial = dataset.deployedTraces.Find(x => (x.PrtName.Equals(prtToName) && x.PrtPinName.Equals(pinToName) && x.DesPrtName.Equals(fromDesc)) ||
                                                                                    (x.DesPrtName.Equals(prtToName) && x.DesPinName.Equals(pinToName) && x.PrtName.Equals(fromDesc)));
                        foreach(Guid tr_id in selectedTraceSpecial.TrID)
                        {
                            highlightedGuids.Add(tr_id);
                        }
                    }
                    else
                    {
                        Trace selectedTraceSpecial = dataset.deployedTraces.Find(x => (x.PrtName.Equals(fromDesc) && x.DesPrtName.Equals(toDesc)) ||
                                                                                    (x.DesPrtName.Equals(fromDesc) && x.PrtName.Equals(toDesc)));
                        foreach(Guid tr_id in selectedTraceSpecial.TrID)
                        {
                            highlightedGuids.Add(tr_id);
                        }
                        
                    }

                }

                dataset.currTrace.Clear();
                foreach(Guid trcID in highlightedGuids)
                {
                    Trace dupTrace = new Trace();
                    Trace foundTrace = dataset.deployedTraces.Find(x => x.TrID.Contains(trcID));

                    if (dataset.currTrace.IndexOf(foundTrace) != -1)
                        continue;
                    else
                    {
                        dupTrace.DesPinName = foundTrace.DesPinName;
                        dupTrace.DesPrtName = foundTrace.DesPrtName;
                        dupTrace.PrtAPos = foundTrace.PrtAPos;
                        dupTrace.PrtBPos = foundTrace.PrtBPos;
                        dupTrace.PrtName = foundTrace.PrtName;
                        dupTrace.PrtPinName = foundTrace.PrtPinName;
                        foreach(Guid i in foundTrace.TrID)
                        {
                            dupTrace.TrID.Add(i);
                        }
                        dupTrace.Type = foundTrace.Type;
                        foreach (Point3d pt in foundTrace.Pts)
                        {
                            dupTrace.Pts.Add(pt);
                        }

                        dataset.currTrace.Add(dupTrace);
                    }
                }

                // highlight all the traces in Rhino scene
                myDoc.Objects.UnselectAll();
                myDoc.Objects.Select(highlightedGuids);
                myDoc.Views.Redraw();
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
            get { return new Guid("74754f76-6d23-4efd-82c5-7f7bc1401859"); }
        }
    }
}