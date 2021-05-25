using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace ModelecComponents
{
    public class PartParserComponent : GH_Component
    {
        RhinoDoc myDoc;

        string oldPartName;
        /// <summary>
        /// Initializes a new instance of the PartParser class.
        /// </summary>
        public PartParserComponent()
          : base("PartParser", "PP",
              "Get the 3D part model and related information",
              "Modelec", "Utility")
        {
            this.oldPartName = "";
            myDoc = RhinoDoc.ActiveDoc;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("SelectedPart", "SP", "Select one part from the list", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Dataset", "Dataset", "The dataset shared with all the components", GH_ParamAccess.item);
            pManager.AddTextParameter("CurrentDirectory", "CDir", "The directory the Grassshopper components is at", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Part3D", "P3D", "The ModElec part unit", GH_ParamAccess.item);
            pManager.AddGenericParameter("Data", "Dataset", "The dataset shared with all the components", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string readPartName = "";
            bool isOn = false;
            SharedData dataset = SharedData.Instance;
            Part3D result = new Part3D();
            string curr_dir = "";

            #region read the button click and decide whether execute the circuit load or not
            if (!DA.GetData(0, ref readPartName))
                return;

            //if (!DA.GetData(1, ref dataset))
            //    return;

            if (!DA.GetData(1, ref curr_dir))
                return;

            if (!readPartName.Equals(oldPartName) && dataset.part2DList.Count > 0)
            {
                oldPartName = readPartName;
                isOn = true;
            }
            else 
            {
                isOn = false;
            }

            #endregion

            if (isOn)
            {
                string part_type = "";
                
                result.PartName = readPartName;

                switch (dataset.schematicType)
                {
                    case 1:
                        {
                            // Eagle

                        }break;
                    case 2:
                        {
                            // Fritzing
                            #region register the pins for the part
                            foreach(Part3D prt in dataset.part3DList)
                            {
                                if (prt.PartName.Equals(readPartName))
                                {
                                    result.Iscustom = prt.Iscustom;
                                    result.IsDeployed = prt.IsDeployed;
                                    result.IsFlipped = prt.IsFlipped;
                                    result.ModelPath = prt.ModelPath;
                                    result.Normal = prt.Normal;
                                    result.PartName = prt.PartName;
                                    result.PartPos = prt.PartPos;
                                    foreach(Pin p in prt.Pins)
                                    {
                                        result.Pins.Add(p);
                                    }
                                    
                                    result.PrtID = prt.PrtID;
                                    result.RotationAngle = prt.RotationAngle;

                                    foreach(Transform tr in prt.TransformReverseSets)
                                    {
                                        result.TransformReverseSets.Add(tr);
                                    }

                                    foreach(Transform trr in prt.TransformSets)
                                    {
                                        result.TransformSets.Add(trr);
                                    }
                                    break;
                                }
                            }
                            #endregion

                        }
                        break;
                    case 3:
                        {
                            // Cadence
                            part_type = readPartName.Substring(0, readPartName.IndexOf('-'));
                            #region register the pins for the part
                            foreach (Part3D prt in dataset.part3DList)
                            {
                                if (prt.PartName.Equals(readPartName))
                                {
                                    result.Iscustom = prt.Iscustom;
                                    result.IsDeployed = prt.IsDeployed;
                                    result.IsFlipped = prt.IsFlipped;
                                    result.ModelPath = prt.ModelPath;
                                    result.Normal = prt.Normal;
                                    result.PartName = prt.PartName;
                                    result.PartPos = prt.PartPos;
                                    foreach (Pin p in prt.Pins)
                                    {
                                        result.Pins.Add(p);
                                    }

                                    result.PrtID = prt.PrtID;
                                    result.RotationAngle = prt.RotationAngle;

                                    foreach (Transform tr in prt.TransformReverseSets)
                                    {
                                        result.TransformReverseSets.Add(tr);
                                    }

                                    foreach (Transform trr in prt.TransformSets)
                                    {
                                        result.TransformSets.Add(trr);
                                    }
                                    break;
                                }
                            }
                            #endregion

                            #region old code (commented)
                            //#region register the pins for the part

                            //// Find the chips.prt file and obtain the pin name and pin number
                            //string chip_path = dataset.circuitPath + @"flatlib\model_sym\" + part_type + @"\";
                            //string[] subdirectoryEntries = Directory.GetDirectories(chip_path);
                            //chip_path = subdirectoryEntries[0] + @"\chips\chips.prt";

                            //bool flag = false;
                            //string line;
                            //System.IO.StreamReader file = new System.IO.StreamReader(chip_path);
                            //while ((line = file.ReadLine()) != null)
                            //{
                            //    if (!flag)
                            //    {
                            //        if (line.Length == 0) continue;

                            //        if (line.Trim().Substring(0, 3).Equals("pin"))
                            //        {
                            //            flag = true;
                            //        }

                            //    }
                            //    else
                            //    {
                            //        string trimmedString = line.Trim();
                            //        if (trimmedString.IndexOf(':') != -1)
                            //        {
                            //            // find the pin name in the current line
                            //            string pin_name = trimmedString.Substring(1, trimmedString.LastIndexOf('\'') - 1);
                            //            int pin_num = -1;

                            //            // find the pin number in the following line
                            //            line = file.ReadLine().Trim();
                            //            if (line.IndexOf("PIN_NUMBER") != -1)
                            //            {
                            //                pin_num = Int32.Parse(line.Substring(line.IndexOf('(') + 1, line.IndexOf(')') - line.IndexOf('(')- 1));
                            //            }

                            //            Pin part_pin = new Pin();
                            //            part_pin.PinName = pin_name;
                            //            part_pin.PinNum = pin_num;

                            //            result.Pins.Add(part_pin);
                            //        }

                            //        if (trimmedString.IndexOf("end_pin") != -1)
                            //        {
                            //            // find all the pin information
                            //            flag = false;
                            //            break;
                            //        }
                            //    }
                            //}

                            //#endregion

                            //#region obtain the 3D model path

                            //// step 1: get part number
                            //string part_num = "";
                            //string csv_path = dataset.circuitPath + dataset.circuitFileName + @".csv";
                            //string part_ID = readPartName.Substring(readPartName.IndexOf('-') + 1, readPartName.Length - readPartName.IndexOf('-') - 1);

                            //bool flag1 = false;
                            //System.IO.StreamReader file1 = new System.IO.StreamReader(csv_path);
                            //while ((line = file1.ReadLine()) != null)
                            //{
                            //    if (!flag1)
                            //    {
                            //        if (line.Length == 0) continue;

                            //        if (line.Trim().Substring(0, 1).Equals("%"))
                            //        {
                            //            flag1 = true;
                            //        }
                            //    }
                            //    else
                            //    {
                            //        string[] segs = line.Trim().Split(',');
                            //        string temp_ID = segs[segs.Length - 1].Substring(1, segs[segs.Length - 1].Length - 3);

                            //        if (part_ID.Equals(temp_ID))
                            //        {
                            //            // find the part number 
                            //            do
                            //            {
                            //                line = file1.ReadLine();
                            //            } while ((line.Trim().Length < 11) || (line.Trim().Length >= 11 && !line.Trim().Substring(0, 11).Equals("PART_NUMBER")));

                            //            // line should be PART_NUMBER"XXXX"
                            //            part_num = line.Substring(line.IndexOf('\"') + 1, line.LastIndexOf('\"') - line.IndexOf('\"') - 1);
                                        
                            //            flag1 = false;
                            //            break;
                            //        }
                            //        else
                            //        {
                            //            flag1 = false;
                            //        }
                            //    }
                            //}

                            //// step 2: get JEDEC number
                            //string JEDEC_num = "";
                            //string prt_table_path = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) + @"ElectronicPartModels\HP-Lib\part_table.ptf";

                            //bool flag2 = false;
                            //int number_idx = 0; // used to store the order of the JEDEC number
                            //System.IO.StreamReader file2 = new System.IO.StreamReader(prt_table_path);
                            //while ((line = file2.ReadLine()) != null)
                            //{
                            //    if (!flag2)
                            //    {
                            //        if (line.Length == 0) continue;

                            //        if (line.Trim().Substring(0, 1).Equals(":"))
                            //        {
                            //            string[] headers = line.Split('|');
                            //            for(number_idx = 0; number_idx < headers.Length; ++number_idx)
                            //            {
                            //                if (headers[number_idx].Contains("JEDEC"))
                            //                {
                            //                    break;
                            //                }
                            //            }
                            //            flag2 = true;
                            //        }
                            //    }
                            //    else
                            //    {
                            //        if (line.Trim().Contains(part_num))
                            //        {
                            //            // find the record that has the part number
                            //            string[] content = line.Trim().Split('|');

                            //            string JEDEC_string = content[number_idx];
                            //            if(JEDEC_string.IndexOf('=') != -1)
                            //            {
                            //                string[] JEDEC_content = JEDEC_string.Split('=');
                            //                JEDEC_num = JEDEC_content[1].Substring(1, JEDEC_content[1].Length - 2);
                            //                break;
                            //            }
                            //            else
                            //            {
                            //                JEDEC_num = JEDEC_string.Substring(1, JEDEC_string.Length - 2);
                            //                break;
                            //            }
                            //        }
                            //        else
                            //        {
                            //            if (line.Trim().Equals("END_PART"))
                            //            {
                            //                // reset the order of the JEDEC number and the flag
                            //                number_idx = 0;
                            //                flag2 = false;
                            //            }
                            //        }
                            //    }
                            //}


                            //// step 3: find the STL file of the part using the JEDEC number
                            //string model_path = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) + @"ElectronicPartModels\HP-Lib\";
                            //string model_package_path = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) + @"ElectronicPartModels\HP-Lib\part-packages\";
                            //string[] subdirectoryEntries1 = Directory.GetDirectories(model_path);
                            //foreach (string subdirectory in subdirectoryEntries1)
                            //{
                            //    if (!subdirectory.Equals(model_package_path))
                            //    {
                            //        string[] fileEntries = Directory.GetFiles(subdirectory);
                            //        bool isfind = false;
                            //        foreach (string s in fileEntries)
                            //        {
                            //            if (s.Contains(JEDEC_num) && s.Substring(s.Length - 3, 3).Equals("stl"))
                            //            {
                            //                isfind = true;
                            //                result.ModelPath = s;
                            //                break;
                            //            }
                            //        }

                            //        if (isfind)
                            //        {
                            //            break;
                            //        }
                            //    }
                            //}
                            //#endregion

                            //#region get the pin postions based on the XML file
                            //string package_path1 = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) + @"ElectronicPartModels\HP-Lib\part-packages\packages\";
                            //string package_path2 = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) + @"ElectronicPartModels\HP-Lib\part-packages\pr_packages\";

                            //string[] packagefileEntries1 = Directory.GetFiles(package_path1);
                            //string[] packagefileEntries2 = Directory.GetFiles(package_path2);

                            //bool isxmlfind = false;

                            //foreach(string entry in packagefileEntries1)
                            //{
                            //    string obtained_name = entry.Substring(entry.LastIndexOf('\\') + 1, entry.Length - entry.LastIndexOf('\\') - 1 - 8);  //.dra.xml so it minus 8
                            //    obtained_name = obtained_name.ToUpper();

                            //    if (JEDEC_num.Equals(obtained_name))
                            //    {
                            //        // find the model's package xml file
                            //        XElement pinList = XElement.Load($"{entry}");
                            //        IEnumerable<XElement> pindataElements = from item in pinList.Descendants("PIN-DEF")
                            //                                                         select item;
                            //        foreach (XElement e in pindataElements)
                            //        {
                            //            if(e.Attribute("NUMBER").Value != "")
                            //            {
                            //                int pinnum = Int32.Parse(e.Attribute("NUMBER").Value);
                            //                foreach(Pin p in result.Pins)
                            //                {
                            //                    if(p.PinNum == pinnum)
                            //                    {
                            //                        double x_offset = Double.Parse(e.Descendants("LOCATION").ElementAt(0).Attribute("X").Value);
                            //                        double y_offset = Double.Parse(e.Descendants("LOCATION").ElementAt(0).Attribute("Y").Value);
                            //                        double z_offset = 0;        // z is always 0 and the center of the 3D model should be on the XY plane

                            //                        p.Pos = new Point3d(x_offset, y_offset, z_offset);

                            //                        int idx = result.Pins.IndexOf(p);
                            //                        result.Pins.ElementAt(idx).Pos = p.Pos;
                            //                    }
                            //                }
                            //            }
                            //        }

                            //        isxmlfind = true;
                            //        break;
                            //    }
                            //}

                            //if (!isxmlfind)
                            //{
                            //    // the file should be in pr_packages

                            //    foreach (string entry in packagefileEntries2)
                            //    {
                            //        string obtained_name = entry.Substring(entry.LastIndexOf('\\') + 1, entry.Length - entry.LastIndexOf('\\') - 1 - 8);  // .dra.xml so it minus 8
                            //        obtained_name = obtained_name.ToUpper();

                            //        if (JEDEC_num.Equals(obtained_name))
                            //        {
                            //            // find the model's package xml file

                            //            XElement pinList = XElement.Load($"{entry}");
                            //            IEnumerable<XElement> pindataElements = from item in pinList.Descendants("PIN-DEF")
                            //                                                    select item;
                            //            foreach (XElement e in pindataElements)
                            //            {
                            //                if (e.Attribute("NUMBER").Value != "")
                            //                {
                            //                    int pinnum = Int32.Parse(e.Attribute("NUMBER").Value);
                            //                    foreach (Pin p in result.Pins)
                            //                    {
                            //                        if (p.PinNum == pinnum)
                            //                        {
                            //                            double x_offset = Double.Parse(e.Descendants("LOCATION").ElementAt(0).Attribute("X").Value);
                            //                            double y_offset = Double.Parse(e.Descendants("LOCATION").ElementAt(0).Attribute("Y").Value);
                            //                            double z_offset = 0;        // z is always 0 and the center of the 3D model should be on the XY plane

                            //                            p.Pos = new Point3d(x_offset, y_offset, z_offset);

                            //                            int idx = result.Pins.IndexOf(p);
                            //                            result.Pins.ElementAt(idx).Pos = p.Pos;

                            //                            break;  // find the pin so we can stop here
                            //                        }
                            //                    }
                            //                }
                            //            }

                            //            break;
                            //        }
                            //    }
                            //}

                            //#endregion

                            #endregion
                        }
                        break;
                    default:break;
                }

                #region highlight the corresponding part in the Rhino scene

                int prt_idx = -1;
                foreach (Part3D prt3D in dataset.part3DList)
                {
                    if (prt3D.PartName.Equals(result.PartName))
                    {
                        prt_idx = dataset.part3DList.IndexOf(prt3D);
                    }
                }

                Guid sel_id = dataset.part3DList.ElementAt(prt_idx).PrtID;

                myDoc.Objects.UnselectAll();
                myDoc.Objects.Select(sel_id);
                myDoc.Views.Redraw();

                #endregion

            }

            dataset.currPart3D = result;

            DA.SetData(0, result);
            DA.SetData(1, dataset);
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
            get { return new Guid("ffd80d14-a058-4c58-9933-cfcaf193a516"); }
        }
    }
}