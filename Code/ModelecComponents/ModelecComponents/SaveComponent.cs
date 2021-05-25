using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using System.Drawing;
using System.Linq;

namespace ModelecComponents
{
    public class SaveComponent : GH_Component
    {
        bool testBtnClick;
        RhinoDoc myDoc;
        TargetBody currBody;
        SharedData dataset;
        ProcessingWindow prcWin;
        ObjectAttributes daAttribute, orangeAttribute, blueAttribute;

        /// <summary>
        /// Initializes a new instance of the SaveComponent class.
        /// </summary>
        public SaveComponent()
          : base("SaveComponent", "Save",
              "Save files for print",
              "ModElec", "Utility")
        {
            testBtnClick = false;
            myDoc = RhinoDoc.ActiveDoc;

            currBody = TargetBody.Instance;
            dataset = SharedData.Instance;
            prcWin = new ProcessingWindow();

            int daIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material daMat = myDoc.Materials[daIndex];
            daMat.DiffuseColor = System.Drawing.Color.FromArgb(63, 217, 206);
            daMat.SpecularColor = System.Drawing.Color.FromArgb(63, 217, 206);
            daMat.Transparency = 0.7f;
            daMat.TransparentColor = System.Drawing.Color.FromArgb(63, 217, 206);
            daMat.CommitChanges();
            daAttribute = new ObjectAttributes();
            //blueAttribute.LayerIndex = 5;
            daAttribute.MaterialIndex = daIndex;
            daAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            daAttribute.ObjectColor = Color.FromArgb(63, 217, 206);
            daAttribute.ColorSource = ObjectColorSource.ColorFromObject;

            int orangeIndex = myDoc.Materials.Add();
            Rhino.DocObjects.Material orangeMat = myDoc.Materials[orangeIndex];
            orangeMat.DiffuseColor = System.Drawing.Color.Orange;
            orangeMat.SpecularColor = System.Drawing.Color.Orange;
            orangeMat.Transparency = 0.7f;
            orangeMat.SpecularColor = System.Drawing.Color.Orange;
            orangeMat.CommitChanges();
            orangeAttribute = new ObjectAttributes();
            //orangeAttribute.LayerIndex = 3;
            orangeAttribute.MaterialIndex = orangeIndex;
            orangeAttribute.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
            orangeAttribute.ObjectColor = Color.Orange;
            orangeAttribute.ColorSource = ObjectColorSource.ColorFromObject;

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
            pManager.AddBooleanParameter("SaveBtnClicked", "BC", "Save all the parts for print", GH_ParamAccess.item);
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
            bool toSave = false;

            #region read the button click and decide whether execute the saving command or not
            if (!DA.GetData(0, ref btnClick))
                return;

            if (!btnClick && testBtnClick)
            {
                toSave = true;
                testBtnClick = false;
            }
            else if (btnClick)
            {
                testBtnClick = true;
            }
            #endregion

            if (toSave)
            {
                #region generate the conductive layer
                int newLayerIdx = -1;
                if (myDoc.Layers.FindName("Conductive") == null)
                {
                    // create a new layer named "Conductive"
                    string layer_name = "Conductive";
                    Layer p_ly = myDoc.Layers.CurrentLayer;

                    Layer new_ly = new Layer();
                    new_ly.Name = layer_name;
                    new_ly.ParentLayerId = p_ly.ParentLayerId;
                    new_ly.Color = Color.FromArgb(0, 0, 0);

                    newLayerIdx = myDoc.Layers.Add(new_ly);
                    daAttribute.LayerIndex = newLayerIdx;
                    myDoc.Layers.ElementAt(newLayerIdx).IsVisible = true;
                    myDoc.Layers.SetCurrentLayerIndex(newLayerIdx, true);

                    if (newLayerIdx < 0) return;
                }
                else
                {
                    newLayerIdx = myDoc.Layers.FindName("Conductive").Index;
                    myDoc.Layers.ElementAt(newLayerIdx).IsVisible = true;
                    myDoc.Layers.SetCurrentLayerIndex(newLayerIdx, true);
                }

                // dupblicate and move all the traces to layer "Conductive"
                Rhino.DocObjects.RhinoObject[] rhobjs = myDoc.Objects.FindByLayer("Traces");
                List<Brep> traceBreps_forDA = new List<Brep>();
                List<Brep> traceBreps_forPlastic = new List<Brep>();

                if (rhobjs == null || rhobjs.Length < 1) return;

                for (int i = 0; i < rhobjs.Length; i++)
                {
                    Brep b = (Brep)rhobjs[i].Geometry;
                    Brep bDup = b.DuplicateBrep();
                    Brep bDupDup = b.DuplicateBrep();
                    Brep bDupDupDup = b.DuplicateBrep();

                    if(dataset.deployedTraces.Find(x=>x.TrID.Equals(rhobjs[i].Id)) == null)
                        traceBreps_forDA.Add(bDupDup);

                    traceBreps_forPlastic.Add(bDupDupDup);

                    Guid tempID = myDoc.Objects.AddBrep(bDup);
                    myDoc.Objects.Find(tempID).Attributes.LayerIndex = newLayerIdx;
                    myDoc.Objects.Find(tempID).CommitChanges();
                    
                }
                myDoc.Views.Redraw();

                #endregion



                #region generate all the DAs
                Brep conBody = (Brep)myDoc.Objects.Find(currBody.convertedObjID).Geometry;
                Brep conBodyDup = conBody.DuplicateBrep();
                List<Brep> daBreps = new List<Brep>();

                #region create a new layer called "DA"

                if (myDoc.Layers.FindName("DA") == null)
                {
                    // create a new layer named "DA"
                    string layer_name = "DA";
                    Layer p_ly = myDoc.Layers.CurrentLayer;

                    Layer new_ly = new Layer();
                    new_ly.Name = layer_name;
                    new_ly.ParentLayerId = p_ly.ParentLayerId;
                    new_ly.Color = Color.FromArgb(0, 0, 0);

                    int index = myDoc.Layers.Add(new_ly);
                    daAttribute.LayerIndex = index;
                    myDoc.Layers.ElementAt(index).IsVisible = true;
                    myDoc.Layers.SetCurrentLayerIndex(index, true);

                    if (index < 0) return;
                }
                else
                {
                    int index = myDoc.Layers.FindName("DA").Index;
                    myDoc.Layers.ElementAt(index).IsVisible = true;
                    myDoc.Layers.SetCurrentLayerIndex(index, true);
                }

                #endregion

                foreach (Trace tr in dataset.deployedTraces)
                {
                    if (tr.PrtName != "")
                    {
                        // the endpoint is not the intermediate point inside the body

                        if (dataset.part3DPList.Find(x => x.PartName.Equals(tr.PrtName)) != null)
                        {
                            Part3DP tempPrt3DP = dataset.part3DPList.Find(x => x.PartName.Equals(tr.PrtName));
                            Sphere da_sphere = new Sphere(tr.PrtAPos, tempPrt3DP.Length + 1.5);
                            Brep da_brep = da_sphere.ToBrep();
                            daBreps.Add(da_brep);
                        }
                        else
                        {
                            Sphere da_sphere = new Sphere(tr.PrtAPos, 1.5);
                            Brep da_brep = da_sphere.ToBrep();
                            daBreps.Add(da_brep);
                        }

                    }

                    if (tr.DesPrtName != "")
                    {
                        // the endpoint is not the intermediate point inside the body
                        if (dataset.part3DPList.Find(x => x.PartName.Equals(tr.DesPrtName)) != null)
                        {
                            Part3DP tempPrt3DP = dataset.part3DPList.Find(x => x.PartName.Equals(tr.DesPrtName));
                            Sphere da_sphere = new Sphere(tr.PrtBPos, tempPrt3DP.Length + 1.5);
                            Brep da_brep = da_sphere.ToBrep();
                            daBreps.Add(da_brep);
                        }
                        else
                        {

                            Sphere da_sphere = new Sphere(tr.PrtBPos, 1.5);
                            Brep da_brep = da_sphere.ToBrep();
                            daBreps.Add(da_brep);
                        }
                    }
                }

                List<Brep> conBodyBreps = new List<Brep>();
                conBodyBreps.Add(conBodyDup);
                foreach (Brep b in traceBreps_forDA)
                {
                    conBodyBreps.Add(b);
                }

                var combody = Brep.CreateBooleanUnion(conBodyBreps, myDoc.ModelAbsoluteTolerance);

                var DAs = Brep.CreateBooleanDifference(daBreps, combody, myDoc.ModelAbsoluteTolerance);

                if (DAs.Length > 0)
                {
                    //var DAs_final = Brep.CreateBooleanDifference(DAs, traceBreps_forDA, myDoc.ModelAbsoluteTolerance);

                    //if (DAs_final.Length > 0)
                    //{
                    //    foreach (Brep da in DAs_final)
                    //    {
                    //        myDoc.Objects.AddBrep(da, daAttribute);
                    //    }
                    //    myDoc.Views.Redraw();
                    //}

                    foreach (Brep da in DAs)
                    {
                        myDoc.Objects.AddBrep(da, daAttribute);
                    }
                    myDoc.Views.Redraw();
                }


                //foreach (Brep b in daBreps)
                //{
                //    myDoc.Objects.AddBrep(b, daAttribute);
                //    myDoc.Views.Redraw();
                //}
                #endregion

                #region generate the plastic layer

                if (myDoc.Layers.FindName("Plastic") == null)
                {
                    // create a new layer named "Plastic"
                    string layer_name = "Plastic";
                    Layer p_ly = myDoc.Layers.CurrentLayer;

                    Layer new_ly = new Layer();
                    new_ly.Name = layer_name;
                    new_ly.ParentLayerId = p_ly.ParentLayerId;
                    new_ly.Color = Color.FromArgb(0, 0, 0);

                    int index = myDoc.Layers.Add(new_ly);
                    daAttribute.LayerIndex = index;
                    myDoc.Layers.ElementAt(index).IsVisible = true;
                    myDoc.Layers.SetCurrentLayerIndex(index, true);

                    if (index < 0) return;
                }
                else
                {
                    int index = myDoc.Layers.FindName("Plastic").Index;
                    myDoc.Layers.ElementAt(index).IsVisible = true;
                    myDoc.Layers.SetCurrentLayerIndex(index, true);
                }


                Brep conBodyDup2 = conBody.DuplicateBrep();
                List<Brep> conBodyDup2List = new List<Brep>();
                conBodyDup2List.Add(conBodyDup2);

                var plastics = Brep.CreateBooleanDifference(conBodyDup2List, traceBreps_forPlastic, myDoc.ModelAbsoluteTolerance);

                if (plastics.Length > 0)
                {
                    foreach (Brep b in plastics)
                    {
                        myDoc.Objects.AddBrep(b);
                    }
                    myDoc.Views.Redraw();
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
            get { return new Guid("9595e891-655a-4f0c-a3c1-c81b74069c72"); }
        }
    }
}