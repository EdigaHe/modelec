using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.GUI.Gradient;
using Grasshopper.Kernel.Types;
using System.Linq;
using Grasshopper.GUI;
using Grasshopper.Kernel.Special;

namespace HumanUI.Components
{
    public class CreateGradientEditor_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreateGradientEditor_Component class.
        /// </summary>
        public CreateGradientEditor_Component()
          : base("Create Gradient Editor", "Gradient",
              "Creates an editable gradient in the UI",
              "Human UI", "UI Elements")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Gradient(s)", "G", "The Gradient or collection of gradient presets to include. Multiples will be ignored if the preset menu is not showing.", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager.AddGenericParameter("Default Gradient", "D", "The default gradient to show - only used if the preset menu is enabled.", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager.AddBooleanParameter("Show Editor", "E", "Set to true to show an interactive gradient editor", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Show Presets", "P", "Set to true to show a pulldown menu of gradient presets", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Gradient Editor", "GE", "The Gradient Editor Element", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_Gradient> gradients = new List<GH_Gradient>();
            List<GH_Gradient> defaultGradient = new List<GH_Gradient>();
            List<object> gradientObjects = new List<object>();
            List<object> defaultGradientObjects = new List<object>();
            bool showPresets = false;
            bool showEditor = true;
            DA.GetData("Show Presets", ref showPresets);
            DA.GetData("Show Editor", ref showEditor);

            DA.GetDataList("Gradient(s)", gradientObjects);
            DA.GetDataList("Default Gradient", defaultGradientObjects);
            var hasDefault = Params.Input[1].SourceCount > 0;
            ExtractGradients(gradients, gradientObjects);

            List<HUI_Gradient> presets = new List<HUI_Gradient>();

            if (hasDefault)
            {
                ExtractGradients(defaultGradient, defaultGradientObjects,1);
                presets.Add(HUI_Gradient.FromGHGradient(defaultGradient.First()));
            }

            foreach (GH_Gradient g in gradients)
            {
                //w/ addition of the default value, have to make sure we don't double add a gradient
                var gradientToAdd = HUI_Gradient.FromGHGradient(g);
                if (presets.Select(p => p.ToString()).Any(p => p.Contains(gradientToAdd.ToString()))) continue;
                presets.Add(gradientToAdd);
            }
            HUI_GradientEditor hge = new HUI_GradientEditor(showPresets, showEditor, presets);
            DA.SetData("Gradient Editor", new UIElement_Goo(hge, "Gradient Editor", InstanceGuid, DA.Iteration));

        }

        private void ExtractGradients(List<GH_Gradient> gradients, List<object> gradientObjects, int paramIndex = 0)
        {
            if (gradientObjects.Count > 0 && gradientObjects[0] is GH_ObjectWrapper)
            {
                foreach (GH_ObjectWrapper wrapper in gradientObjects.OfType<GH_ObjectWrapper>())
                {
                    GH_GradientControl editor = wrapper.Value as GH_GradientControl;
                    gradients.Add(editor.Gradient);
                }
            }
            else
            {
                var doc = OnPingDocument();
                doc.Objects.OfType<GH_GradientControl>().Where(ao => Params.Input[paramIndex].DependsOn(ao)).ToList().ForEach(g => gradients.Add(g.Gradient));
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.GradientEditor;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{DA8DD6A3-AD57-471E-B891-94902C8647ED}");
    }
}