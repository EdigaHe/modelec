using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ModelecComponents.Helpers
{
    public class XmlHelpers
    {
        public XmlHelpers() { }

        /// <summary>
        /// Crop the image to export the correct region to show the entire schematic
        /// </summary>
        /// <param name="filepath">the xcon file path</param>
        /// <param name="imagepath">the converted image path</param>
        /// <param name="x">the region origin X</param>
        /// <param name="y">the region origin Y</param>
        /// <param name="w">the width of the region</param>
        /// <param name="h">the height of the region</param>
        /// <param name="imgW">the height of the image</param>
        /// <param name="imgH">the height of the image</param>
        public void CropImage(string filepath, string imagepath, out int x, out int y, out int w, out int h, int imgW, int imgH)
        {
            x = 0;
            y = 0;
            w = 0;
            h = 0;

            bool flag = false;
            string line;
            string csvFilePath = filepath.Substring(0, filepath.IndexOf('.')) + ".csv";

            int x_min = Int32.MaxValue, x_max = Int32.MinValue;
            int y_min = Int32.MaxValue, y_max = Int32.MinValue;

            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(csvFilePath);
            while ((line = file.ReadLine()) != null)
            {
                if (!flag)
                {
                    if (line.Length == 0) continue;

                    if (line.Substring(0, 1).Equals("%"))
                    {
                        flag = true;
                    }
                }
                else
                {
                    // this line has the coordinates
                    int idx_left = line.IndexOf('(');
                    int idx_right = line.IndexOf(')');
                    string coordinatesString = line.Substring(idx_left + 1, idx_right - idx_left - 1);

                    string[] coordinates = coordinatesString.Split(',');
                    int coorX = 0, coorY = 0;
                    try
                    {
                        coorX = (int)(Int32.Parse(coordinates[0]) * 0.65);
                        coorY = (int)(Int32.Parse(coordinates[1]) * 0.65);

                        if(coorX <= x_min)
                        {
                            x_min = coorX;
                        }

                        if (coorX >= x_max)
                        {
                            x_max = coorX;
                        }

                        if(coorY <= y_min)
                        {
                            y_min = coorY;
                        }

                        if(coorY >= y_max)
                        {
                            y_max = coorY;
                        }

                    }
                    catch(Exception e)
                    {
                        
                    }
                    flag = false;
                }
            }

            int range = 400;

            x = ((x_min - range) < 0) ? 0 : (x_min - range);
            w = (((x_max + range) > imgW) ? imgW : (x_max + range)) - (((x_min - range) < 0) ? 0 : (x_min - range));
            y = imgH - (((y_max + range) > imgH) ? imgH : (y_max + range));
            h = (((y_max + range) > imgH) ? imgH : (y_max + range)) - (((y_min - range) < 0) ? 0 : (y_min - range));
        }

        /// <summary>
        /// Count the number of traces in a circuit
        /// </summary>
        /// <param name="xmlFileName">the path of the imported circuit</param>
        /// <param name="type">the file format of the circuit schematic</param>
        /// <returns></returns>
        public int GetNumberOfTraces(string xmlFileName, int type)
        {
            int numTrace = 0;
            //var currentDirectory = Directory.GetCurrentDirectory();
            //string filename = @"circuit-lib\" + xmlFileName;
            //var circuitFilepath = Path.Combine(currentDirectory, filename);

            string circuitFilepath = xmlFileName;
            XElement circuitTraceList = XElement.Load($"{circuitFilepath}");

            List<KeyValuePair<string,int>> connections = new List<KeyValuePair<string, int>>();

            switch (type)
            {
                case 1:
                    {
                        // Eagle file
                        IEnumerable<XElement> circuitComponentElements = from item in circuitTraceList.Descendants("net")
                                                                         select item;
                        foreach (XElement e in circuitComponentElements)
                        {
                            numTrace += e.Descendants("segment").ElementAt(0).Descendants("pinref").Count() - 1;
                        }
                    }
                    break;
                case 2:
                    {
                        // Fritzing file
                        IEnumerable<XElement> circuitComponentElements = from item in circuitTraceList.Descendants("instance")
                                                                         select item;

                        foreach (XElement e in circuitComponentElements)
                        {
                            if (e.Attribute("moduleIdRef").Value.Equals("TwoLayerRectanglePCBModuleID") || e.Attribute("moduleIdRef").Value.Equals("WireModuleID"))
                            {
                                continue;
                            }
                            else
                            {
                                // meaningful part found
                                var connectors = e.Descendants("views").ElementAt(0).
                                    Descendants("schematicView").ElementAt(0).
                                    Descendants("connectors").ElementAt(0);

                                IEnumerable<XElement> connects = from item in connectors.Descendants("connector")
                                                                 select item;

                                foreach (XElement ele in connects)
                                {
                                    numTrace += ele.Descendants("connects").ElementAt(0).Descendants("connect").Count();
                                }
                            }
                        }

                        numTrace = numTrace / 2;
                    }
                    break;
                case 3:
                    {
                        // parse the circuit schematic created with Cadence

                        IEnumerable<XElement> circuitComponentElements = from item in circuitTraceList.Descendants("instance")
                                                                         select item;
                        foreach (XElement e in circuitComponentElements)
                        {
                            var pins = e.Descendants("pin");
                            if(pins != null)
                            {
                                foreach (var p in pins)
                                {
                                    var connects = p.Descendants("connection");
                                    foreach (var c in connects)
                                    {
                                        string netName = c.Attribute("net").Value;
                                        int idx = -1;
                                        int currCount = -1;
                                        foreach (KeyValuePair<string, int> kv in connections)
                                        {
                                            if (kv.Key.Equals(netName))
                                            {
                                                idx = connections.IndexOf(kv);
                                                currCount = kv.Value;
                                                currCount++;
                                            }
                                        }

                                        if (idx != -1)
                                        {
                                            // find exisitng net
                                            connections.RemoveAt(idx);
                                            KeyValuePair<string, int> temp = new KeyValuePair<string, int>(netName, currCount);
                                            connections.Insert(idx, temp);
                                        }
                                        else
                                        {
                                            KeyValuePair<string, int> temp = new KeyValuePair<string, int>(netName, 1);
                                            connections.Add(temp);
                                        }
                                    }
                                }
                            }
                            
                        }

                        foreach(var kv in connections)
                        {
                            numTrace += kv.Value - 1;
                        }
                    }
                    break;
                default: break;
            }

            return numTrace;
        }

        /// <summary>
        /// Count the number of electronic parts in the imported circuit schematic
        /// </summary>
        /// <param name="xmlFileName">the circuit schematic path</param>
        /// <param name="type">the circuit schematic format</param>
        /// <returns></returns>
        public int GetNumberOfParts(string xmlFileName, int type)
        {
            int numComp = 0;
            //var currentDirectory = Directory.GetCurrentDirectory();
            //string filename = @"circuit-lib\" + xmlFileName;
            //var circuitFilepath = Path.Combine(currentDirectory, filename);

            string circuitFilepath = xmlFileName;
            XElement circuitComponentList = XElement.Load($"{circuitFilepath}");

            switch (type)
            {
                case 1:
                    {
                        // parse the circuit schematic created with Eagle
                        IEnumerable<XElement> circuitComponentElements = from item in circuitComponentList.Descendants("parts")
                                                                         select item;
                        foreach (XElement e in circuitComponentElements)
                        {
                            numComp += e.Descendants("part").Count();
                        }
                    }
                    break;
                case 2:
                    {
                        // parse the circuit schematic created with Fritzing
                        IEnumerable<XElement> circuitComponentElements = from item in circuitComponentList.Descendants("instance")
                                                                         select item;

                        foreach (XElement e in circuitComponentElements)
                        {
                            if (e.Attribute("moduleIdRef").Value.Equals("TwoLayerRectanglePCBModuleID") || e.Attribute("moduleIdRef").Value.Equals("WireModuleID"))
                            {
                                continue;
                            }
                            else
                            {
                                // meaningful part found
                                numComp++;
                            }
                        }
                    }
                    break;
                case 3:
                    {
                        // parse the circuit schematic created with Cadence
                        
                        IEnumerable<XElement> circuitComponentElements = from item in circuitComponentList.Descendants("pages")
                                                                         select item;
                        foreach (XElement e in circuitComponentElements)
                        {
                            numComp += e.Descendants("instance").Count();
                        }
                    }
                    break;
                default: break;
            }

            return numComp;
        }

        /// <summary>
        /// Find the longest common substring from two strings
        /// </summary>
        /// <param name="X">the first string</param>
        /// <param name="Y">the second string</param>
        /// <param name="m">the length of the first string</param>
        /// <param name="n">the length of the second string</param>
        string printLCSubStr(String X, String Y, int m, int n)
        {
            // Create a table to store lengths of longest common
            // suffixes of substrings. Note that LCSuff[i][j]
            // contains length of longest common suffix of X[0..i-1]
            // and Y[0..j-1]. The first row and first column entries
            // have no logical meaning, they are used only for
            // simplicity of program
            int[,] LCSuff = new int[m + 1, n + 1];

            // To store length of the longest common substring
            int len = 0;

            // To store the index of the cell which contains the
            // maximum value. This cell's index helps in building
            // up the longest common substring from right to left.
            int row = 0, col = 0;

            /* Following steps build LCSuff[m+1][n+1] in bottom
            up fashion. */
            for (int i = 0; i <= m; i++)
            {
                for (int j = 0; j <= n; j++)
                {
                    if (i == 0 || j == 0)
                        LCSuff[i, j] = 0;

                    else if (X[i - 1] == Y[j - 1])
                    {
                        LCSuff[i, j] = LCSuff[i - 1, j - 1] + 1;
                        if (len < LCSuff[i, j])
                        {
                            len = LCSuff[i, j];
                            row = i;
                            col = j;
                        }
                    }
                    else
                        LCSuff[i, j] = 0;
                }
            }

            // if true, then no common substring exists
            if (len == 0)
            {
                return "";
            }

            // allocate space for the longest common substring
            String resultStr = "";

            // traverse up diagonally form the (row, col) cell
            // until LCSuff[row][col] != 0
            while (LCSuff[row, col] != 0)
            {
                resultStr = X[row - 1] + resultStr; // or Y[col-1]
                --len;

                // move diagonally up to previous cell
                row--;
                col--;
            }

            // required longest common substring
            return resultStr;
        }

        /// <summary>
        /// Get the list of 3D parts that are included in the 2D schematic
        /// </summary>
        /// <param name="filename">the schematic file path</param>
        /// <param name="type">the file format of the schematic</param>
        /// <param name="scalar">the scalar of the schematic image</param>
        /// <param name="dataset">the SharedData instance</param>
        /// <param name="curr_dir">the current directory path where the Grasshopper file sits</param>
        /// <returns></returns>
        public List<Part3D> GetParts3DFromSchematic(string filename, int type, double scalar, SharedData dataset, string curr_dir)
        {
            List<Part3D> result = new List<Part3D>();

            XElement circuitComponentList = XElement.Load($"{filename}");

            switch (type)
            {
                case 1:
                    {
                        // parse the circuit schematic created with Eagle
                        IEnumerable<XElement> circuitComponentElements = from item in circuitComponentList.Descendants("instance")
                                                                         select item;
                        foreach (XElement e in circuitComponentElements)
                        {
                            Part3D temp = new Part3D();

                            temp.PartName = e.Attribute("part").Value;
                            // To-Do: add the other information for the 3D model based on the Eagle structre

                            temp.ModelPath = "";
                            temp.IsDeployed = true;
                            temp.IsFlipped = false;
                            temp.PrtID = Guid.Empty;
                            temp.RotationAngle = 0;
                            temp.TransformSets = new List<Transform>();
                            temp.TransformReverseSets = new List<Transform>();

                            #region get all the pins
                            int n = 1;
                            IEnumerable<XElement> partElements = from item in circuitComponentList.Descendants("pinref")
                                                                         select item;

                            foreach(XElement prt in partElements)
                            {
                                String partName = prt.Attribute("part").Value;
                                if (partName.Equals(temp.PartName))
                                {
                                    String pinName = prt.Attribute("pin").Value;
                                    if (temp.Pins.Find(x => x.PinName.Equals(pinName)) == null)
                                    {
                                        Pin tempPin = new Pin();
                                        tempPin.PinName = pinName;
                                        tempPin.PinNum = n;
                                        tempPin.Pos = new Point3d(0, 0, 0);
                                        n++;

                                        temp.Pins.Add(tempPin);
                                    }
                                }
                            }

                            #endregion

                            result.Add(temp);
                        }
                    }
                    break;
                case 2:
                    {
                        // parse the circuit schematic created with Fritzing

                        bool isUserPart = false;

                        IEnumerable<XElement> circuitComponentElements = from item in circuitComponentList.Descendants("instance")
                                                                         select item;

                        foreach (XElement e in circuitComponentElements)
                        {
                            if (e.Attribute("moduleIdRef").Value.Equals("TwoLayerRectanglePCBModuleID") || e.Attribute("moduleIdRef").Value.Equals("WireModuleID"))
                            {
                                continue;
                            }
                            else
                            {
                                // meaningful part found
                                Part3D temp = new Part3D();

                                temp.PartName = e.Descendants("title").ElementAt(0).Value;
                                

                                #region Get the model name
                                String partFZPSysPath = e.Attribute("path").Value;
                                int fzp_idx = partFZPSysPath.IndexOf("fritzing-parts");
                                String tempPartFZPPath = "";

                                if (fzp_idx == -1)
                                {
                                    // user defined components
                                    isUserPart = true;
                                    fzp_idx = partFZPSysPath.IndexOf("parts");
                                    tempPartFZPPath = partFZPSysPath.Substring(fzp_idx, partFZPSysPath.Length - fzp_idx);
                                }
                                else
                                {
                                    // make sure this path starts with "\parts\"
                                    isUserPart = false;
                                    tempPartFZPPath = partFZPSysPath.Substring(fzp_idx + 9, partFZPSysPath.Length - fzp_idx - 9);
                                }

                                
                                tempPartFZPPath = tempPartFZPPath.Replace('/', '\\');
                                String partFZPPath = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) + 
                                                        @"ElectronicPartModels\Fritzing-Lib\" + tempPartFZPPath;

                                XElement partInfo = XElement.Load($"{partFZPPath}");

                                IEnumerable<XElement> partInfoElements = from item in partInfo.Descendants("property")
                                                                                 select item;
                                String modelName = "";
                                foreach (XElement pinfo in partInfoElements)
                                {
                                    if (pinfo.Attribute("name").Value.Equals("package"))
                                    {
                                        modelName = pinfo.Value.ToUpper();
                                        break;
                                    }
                                }

                                #endregion

                                #region find the 3D model (STL) file in the open-sourced Sparkfun repository

                                try
                                {
                                    String modelPath = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) + @"ElectronicPartModels\sparkfun-models\";
                                    string[] STLfileEntries = Directory.GetFiles(modelPath);

                                    string longest_com_string = "";
                                    string longest_com_path = "";
                                    foreach (string s in STLfileEntries)
                                    {
                                        String targetFileName = s.Substring(s.IndexOf("sparkfun-models") + 16, s.IndexOf('.') - s.IndexOf("sparkfun-models") - 16);

                                        string common_substring = printLCSubStr(modelName, targetFileName, modelName.Length, targetFileName.Length);

                                        if (common_substring == "")
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            if (common_substring.Equals(targetFileName))
                                            {
                                                longest_com_string = common_substring;
                                                longest_com_path = s;
                                                break;
                                            }
                                            else
                                            {
                                                if (common_substring.Length >= 7)
                                                {
                                                    if (common_substring.Length >= longest_com_string.Length)
                                                    {
                                                        longest_com_string = common_substring;
                                                        longest_com_path = s;
                                                    }
                                                }
                                            }
                                        }

                                    }

                                    
                                    temp.ModelPath = longest_com_path;

                                    if (longest_com_path == "")
                                        temp.Iscustom = true;
                                    else
                                        temp.Iscustom = false;
                                }
                                catch
                                {
                                    temp.ModelPath = "";
                                    temp.Iscustom = true;
                                }

                                #endregion

                                temp.IsDeployed = true;
                                temp.IsFlipped = false;
                                temp.PrtID = Guid.Empty;
                                temp.RotationAngle = 0;
                                temp.TransformSets = new List<Transform>();
                                temp.TransformReverseSets = new List<Transform>();

                                if (!temp.Iscustom)
                                {
                                    #region find the pins 

                                    // find the connectors (pads/pins)

                                    int n = 1;
                                    IEnumerable<XElement> partConectorElements = from item in partInfo.Descendants("connector")
                                                                                 select item;

                                    foreach (XElement connector in partConectorElements)
                                    {
                                        Pin tempPin = new Pin();
                                        tempPin.PinName = connector.Attribute("id").Value + " (" + connector.Attribute("name").Value + ")";
                                        tempPin.PinNum = n;
                                        n++;

                                        temp.Pins.Add(tempPin);
                                    }

                                    // find the pin positions
                                    string cirPCBPath = "";
                                    IEnumerable<XElement> pcbElements = from item in partInfo.Descendants("pcbView")
                                                                        select item;

                                    foreach (XElement pcbview in pcbElements)
                                    {
                                        if (pcbview.Descendants("layers") != null)
                                        {
                                            string pcbp = pcbview.Descendants("layers").ElementAt(0).Attribute("image").Value;
                                            pcbp = pcbp.Replace('/', '\\');

                                            
                                            cirPCBPath = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) +
                                                        @"ElectronicPartModels\Fritzing-Lib\parts\svg\core\" + pcbp;
                                            
                                            break;
                                        }

                                    }

                                    XElement partPCBImage = XElement.Load($"{cirPCBPath}");


                                    double part_width = 0, part_height = 0;

                                    string partBox = partPCBImage.Attribute("viewBox").Value;
                                    string[] part_dim = partBox.Split(' ');
                                    part_width = double.Parse(part_dim[2]);
                                    part_height = double.Parse(part_dim[3]);


                                    string pcb_line;
                                    System.IO.StreamReader file_pcb = new System.IO.StreamReader(cirPCBPath);
                                    while ((pcb_line = file_pcb.ReadLine()) != null)
                                    {
                                        if (pcb_line.Length == 0 || pcb_line.Length < 13) continue;

                                        if (pcb_line.IndexOf("connectorname") != -1)
                                        {
                                            // find the contact pads
                                            int con_idx = pcb_line.IndexOf("id");

                                            // connectorname exists
                                            // the valid pads
                                            int con_name_first = pcb_line.IndexOf('\'', con_idx);
                                            int con_name_last = pcb_line.IndexOf('\'', con_name_first + 1);
                                            string con_name = pcb_line.Substring(con_name_first + 1, con_name_last - con_name_first - 1);
                                            con_name = con_name.Substring(0, con_name.Length - 3);

                                            if (pcb_line.IndexOf("rect") != -1)
                                            {
                                                // the pads are in rect (for SMD)
                                                foreach (Pin p in temp.Pins)
                                                {
                                                    string p_id = p.PinName.Substring(0, p.PinName.IndexOf(' '));

                                                    if (p_id.Equals(con_name))
                                                    {
                                                        Point3d pPos = new Point3d();
                                                        pPos.Z = 0;

                                                        int x_idx = pcb_line.IndexOf("x=\'");
                                                        int x_idx_first = pcb_line.IndexOf('\'', x_idx);
                                                        int x_idx_last = pcb_line.IndexOf('\'', x_idx_first + 1);
                                                        double pos_x = double.Parse(pcb_line.Substring(x_idx_first + 1, x_idx_last - x_idx_first - 1));

                                                        int y_idx = pcb_line.IndexOf("y=\'");
                                                        int y_idx_first = pcb_line.IndexOf('\'', y_idx);
                                                        int y_idx_last = pcb_line.IndexOf('\'', y_idx_first + 1);
                                                        double pos_y = double.Parse(pcb_line.Substring(y_idx_first + 1, y_idx_last - y_idx_first - 1));

                                                        int w_idx = pcb_line.IndexOf("width=\'");
                                                        int w_idx_first = pcb_line.IndexOf('\'', w_idx);
                                                        int w_idx_last = pcb_line.IndexOf('\'', w_idx_first + 1);
                                                        double width = double.Parse(pcb_line.Substring(w_idx_first + 1, w_idx_last - w_idx_first - 1));

                                                        int h_idx = pcb_line.IndexOf("width=\'");
                                                        int h_idx_first = pcb_line.IndexOf('\'', h_idx);
                                                        int h_idx_last = pcb_line.IndexOf('\'', h_idx_first + 1);
                                                        double height = double.Parse(pcb_line.Substring(h_idx_first + 1, h_idx_last - h_idx_first - 1));

                                                        pPos.X = (pos_x + width / 2) - part_width / 2;
                                                        pPos.Y = (pos_y + height / 2) - part_height / 2;

                                                        p.Pos = pPos;
                                                    }
                                                }
                                            }
                                            else if (pcb_line.IndexOf("circle") != -1)
                                            {
                                                // the pads are in circle (for through-hole pin device)
                                                foreach (Pin p in temp.Pins)
                                                {
                                                    string p_id = p.PinName.Substring(0, p.PinName.IndexOf(' '));

                                                    if (p_id.Equals(con_name))
                                                    {
                                                        Point3d pPos = new Point3d();
                                                        pPos.Z = 0;

                                                        int x_idx = pcb_line.IndexOf("cx=\'");
                                                        int x_idx_first = pcb_line.IndexOf('\'', x_idx);
                                                        int x_idx_last = pcb_line.IndexOf('\'', x_idx_first + 1);
                                                        double pos_x = double.Parse(pcb_line.Substring(x_idx_first + 1, x_idx_last - x_idx_first - 1));

                                                        int y_idx = pcb_line.IndexOf("cy=\'");
                                                        int y_idx_first = pcb_line.IndexOf('\'', y_idx);
                                                        int y_idx_last = pcb_line.IndexOf('\'', y_idx_first + 1);
                                                        double pos_y = double.Parse(pcb_line.Substring(y_idx_first + 1, y_idx_last - y_idx_first - 1));

                                                        pPos.X = pos_x;
                                                        pPos.Y = pos_y;

                                                        p.Pos = pPos;
                                                    }
                                                }
                                            }


                                        }


                                        //if(pcb_line.Substring(0, 5).Equals("<rect"))
                                        //{
                                        //    // find the contact pads
                                        //    int con_idx = pcb_line.IndexOf("connectorname");
                                        //    if (con_idx != -1)
                                        //    {
                                        //        // connectorname exists
                                        //        // the valid pads
                                        //        int con_name_first = pcb_line.IndexOf('\'', con_idx);
                                        //        int con_name_last = pcb_line.IndexOf('\'', con_name_first + 1);
                                        //        string con_name = pcb_line.Substring(con_name_first + 1, con_name_last - con_name_first - 1);

                                        //        foreach (Pin p in temp.Pins)
                                        //        {
                                        //            if (p.PinName.Equals(con_name))
                                        //            {
                                        //                Point3d pPos = new Point3d();
                                        //                pPos.Z = 0;

                                        //                int x_idx = pcb_line.IndexOf("x=\'");
                                        //                int x_idx_first = pcb_line.IndexOf('\'', x_idx);
                                        //                int x_idx_last = pcb_line.IndexOf('\'', x_idx_first + 1);
                                        //                double pos_x= double.Parse(pcb_line.Substring(x_idx_first + 1, x_idx_last - x_idx_first - 1));

                                        //                int y_idx = pcb_line.IndexOf("y=\'");
                                        //                int y_idx_first = pcb_line.IndexOf('\'', y_idx);
                                        //                int y_idx_last = pcb_line.IndexOf('\'', y_idx_first + 1);
                                        //                double pos_y = double.Parse(pcb_line.Substring(y_idx_first + 1, y_idx_last - y_idx_first - 1));

                                        //                int w_idx = pcb_line.IndexOf("width=\'");
                                        //                int w_idx_first = pcb_line.IndexOf('\'', w_idx);
                                        //                int w_idx_last = pcb_line.IndexOf('\'', w_idx_first + 1);
                                        //                double width = double.Parse(pcb_line.Substring(w_idx_first + 1, w_idx_last - w_idx_first - 1));

                                        //                int h_idx = pcb_line.IndexOf("width=\'");
                                        //                int h_idx_first = pcb_line.IndexOf('\'', h_idx);
                                        //                int h_idx_last = pcb_line.IndexOf('\'', h_idx_first + 1);
                                        //                double height = double.Parse(pcb_line.Substring(h_idx_first + 1, h_idx_last - h_idx_first - 1));

                                        //                pPos.X = (pos_x + width / 2) - part_width / 2;
                                        //                pPos.Y = (pos_y + height / 2) - part_height / 2;

                                        //                p.Pos = pPos;
                                        //            }
                                        //        }

                                        //    }

                                        //}
                                    }

                                    file_pcb.Close();

                                    #endregion
                                }
                                else
                                {
                                    // find the connectors (pads/pins)

                                    int n = 1;
                                    IEnumerable<XElement> partConectorElements = from item in partInfo.Descendants("connector")
                                                                                 select item;

                                    foreach (XElement connector in partConectorElements)
                                    {
                                        Pin tempPin = new Pin();
                                        tempPin.PinName = connector.Attribute("id").Value + " (" + connector.Attribute("name").Value + ")";
                                        tempPin.PinNum = n;
                                        tempPin.Pos = new Point3d(0, 0, 0);
                                        n++;

                                        temp.Pins.Add(tempPin);
                                    }

                                }

                                result.Add(temp);
                            }
                        }
                    }
                    break;
                case 3:
                    {
                        // parse the circuit schematic created with Cadence
                        string csvFileName = filename.Substring(0, filename.IndexOf('.')) + ".csv";

                        bool flag = false;
                        string line;

                        int x_min = Int32.MaxValue, x_max = Int32.MinValue;
                        int y_min = Int32.MaxValue, y_max = Int32.MinValue;

                        #region get the origin, width, and height of the region

                        // Read the file and display it line by line.  
                        System.IO.StreamReader file = new System.IO.StreamReader(csvFileName);
                        while ((line = file.ReadLine()) != null)
                        {
                            if (!flag)
                            {
                                if (line.Length == 0) continue;

                                if (line.Substring(0, 1).Equals("%"))
                                {
                                    flag = true;
                                }
                            }
                            else
                            {
                                // get the positions
                                int idx_left = line.IndexOf('(');
                                int idx_right = line.IndexOf(')');
                                string coordinatesString = line.Substring(idx_left + 1, idx_right - idx_left - 1);

                                string[] coordinates = coordinatesString.Split(',');
                                int coorX = 0, coorY = 0;
                                try
                                {
                                    coorX = (int)(Int32.Parse(coordinates[0]) * 0.65);
                                    coorY = (int)(Int32.Parse(coordinates[1]) * 0.65);

                                    if (coorX <= x_min)
                                    {
                                        x_min = coorX;
                                    }

                                    if (coorX >= x_max)
                                    {
                                        x_max = coorX;
                                    }

                                    if (coorY <= y_min)
                                    {
                                        y_min = coorY;
                                    }

                                    if (coorY >= y_max)
                                    {
                                        y_max = coorY;
                                    }

                                }
                                catch (Exception e) { }
                                flag = false;
                            }
                        }

                        int range = 400;

                        string pngFile = filename.Substring(0, filename.IndexOf('.')) + ".png";
                        Image img = Image.FromFile(pngFile);
                        int imgW = img.Width;
                        int imgH = img.Height;

                        int orig_x = ((x_min - range) < 0) ? 0 : (x_min - range);
                        int w = (((x_max + range) > imgW) ? imgW : (x_max + range)) - (((x_min - range) < 0) ? 0 : (x_min - range));
                        int orig_y = imgH - (((y_max + range) > imgH) ? imgH : (y_max + range));
                        int h = (((y_max + range) > imgH) ? imgH : (y_max + range)) - (((y_min - range) < 0) ? 0 : (y_min - range));

                        #endregion

                        #region get the parts
                        System.IO.StreamReader file1 = new System.IO.StreamReader(csvFileName);
                        while ((line = file1.ReadLine()) != null)
                        {
                            if (!flag)
                            {
                                if (line.Length == 0) continue;

                                if (line.Substring(0, 1).Equals("%"))
                                {
                                    flag = true;
                                }
                            }
                            else
                            {
                                string[] segs = line.Split(',');
                                string partName = segs[4].Substring(1, segs[4].Length - 2);
                                string suffix = segs[5].Substring(1, segs[5].Length - 3);
                                partName = partName + "-" + suffix;

                                // get the positions
                                double posX = 0, posY = 0;
                                int idx_left = line.IndexOf('(');
                                int idx_right = line.IndexOf(')');
                                string coordinatesString = line.Substring(idx_left + 1, idx_right - idx_left - 1);

                                string[] coordinates = coordinatesString.Split(',');
                                int coorX = 0, coorY = 0;
                                try
                                {
                                    coorX = (int)(Int32.Parse(coordinates[0]) * 0.65);
                                    coorY = (int)(Int32.Parse(coordinates[1]) * 0.65);

                                    posX = (coorX - orig_x) * scalar;
                                    posY = (imgH - coorY - orig_y) * scalar;

                                }
                                catch (Exception e) { }

                                Part3D temp = new Part3D();
                                temp.PartName = partName;

                                temp.Iscustom = false;
                                temp.IsDeployed = true;
                                temp.PrtID = Guid.Empty;
                                temp.RotationAngle = 0;
                                temp.TransformSets = new List<Rhino.Geometry.Transform>();

                                result.Add(temp);

                                flag = false;
                            }
                        }
                        file1.Close();

                        foreach(Part3D p3D in result)
                        {
                            #region register the pins for the part

                            // Find the chips.prt file and obtain the pin name and pin number
                            string part_type = p3D.PartName.Substring(0, p3D.PartName.IndexOf('-'));

                            string chip_path = dataset.circuitPath + @"flatlib\model_sym\" + part_type + @"\";
                            string[] subdirectoryEntries = Directory.GetDirectories(chip_path);
                            chip_path = subdirectoryEntries[0] + @"\chips\chips.prt";

                            bool flag2 = false;
                            string line2;
                            System.IO.StreamReader file2 = new System.IO.StreamReader(chip_path);
                            while ((line2 = file2.ReadLine()) != null)
                            {
                                if (!flag2)
                                {
                                    if (line2.Length == 0) continue;

                                    if (line2.Trim().Substring(0, 3).Equals("pin"))
                                    {
                                        flag2 = true;
                                    }

                                }
                                else
                                {
                                    string trimmedString = line2.Trim();
                                    if (trimmedString.IndexOf(':') != -1)
                                    {
                                        // find the pin name in the current line
                                        string pin_name = trimmedString.Substring(1, trimmedString.LastIndexOf('\'') - 1);
                                        int pin_num = -1;

                                        // find the pin number in the following line
                                        line2 = file2.ReadLine().Trim();
                                        if (line2.IndexOf("PIN_NUMBER") != -1)
                                        {
                                            pin_num = Int32.Parse(line2.Substring(line2.IndexOf('(') + 1, line2.IndexOf(')') - line2.IndexOf('(') - 1));
                                        }

                                        Pin part_pin = new Pin();
                                        part_pin.PinName = pin_name;
                                        part_pin.PinNum = pin_num;

                                        p3D.Pins.Add(part_pin);
                                    }

                                    if (trimmedString.IndexOf("end_pin") != -1)
                                    {
                                        // find all the pin information
                                        flag2 = false;
                                        break;
                                    }
                                }
                            }

                            #endregion

                            #region obtain the 3D model path

                            // step 1: get part number
                            string part_num = "";
                            string csv_path = dataset.circuitPath + dataset.circuitFileName + @".csv";
                            string part_ID = p3D.PartName.Substring(p3D.PartName.IndexOf('-') + 1, p3D.PartName.Length - p3D.PartName.IndexOf('-') - 1);

                            bool flag3 = false;
                            System.IO.StreamReader file3 = new System.IO.StreamReader(csv_path);
                            while ((line2 = file3.ReadLine()) != null)
                            {
                                if (!flag3)
                                {
                                    if (line2.Length == 0) continue;

                                    if (line2.Trim().Substring(0, 1).Equals("%"))
                                    {
                                        flag3 = true;
                                    }
                                }
                                else
                                {
                                    string[] segs1 = line2.Trim().Split(',');
                                    string temp_ID = segs1[segs1.Length - 1].Substring(1, segs1[segs1.Length - 1].Length - 3);

                                    if (part_ID.Equals(temp_ID))
                                    {
                                        // find the part number 
                                        do
                                        {
                                            line2 = file3.ReadLine();
                                        } while ((line2.Trim().Length < 11) || (line2.Trim().Length >= 11 && !line2.Trim().Substring(0, 11).Equals("PART_NUMBER")));
                                        

                                        // line should be PART_NUMBER"XXXX"
                                        part_num = line2.Substring(line2.IndexOf('\"') + 1, line2.LastIndexOf('\"') - line2.IndexOf('\"') - 1);

                                        flag3 = false;
                                        break;
                                    }
                                    else
                                    {
                                        flag3 = false;
                                    }
                                }
                            }
                            file3.Close();

                            // step 2: get JEDEC number
                            string JEDEC_num = "";
                            string prt_table_path = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) + @"ElectronicPartModels\HP-Lib\part_table.ptf";

                            bool flag4 = false;
                            int number_idx = 0; // used to store the order of the JEDEC number
                            System.IO.StreamReader file4 = new System.IO.StreamReader(prt_table_path);
                            while ((line2 = file4.ReadLine()) != null)
                            {
                                if (!flag4)
                                {
                                    if (line2.Length == 0) continue;

                                    if (line2.Trim().Substring(0, 1).Equals(":"))
                                    {
                                        string[] headers = line2.Split('|');
                                        for (number_idx = 0; number_idx < headers.Length; ++number_idx)
                                        {
                                            if (headers[number_idx].Contains("JEDEC"))
                                            {
                                                break;
                                            }
                                        }
                                        flag4 = true;
                                    }
                                }
                                else
                                {
                                    if (line2.Trim().Contains(part_num))
                                    {
                                        // find the record that has the part number
                                        string[] content = line2.Trim().Split('|');

                                        string JEDEC_string = content[number_idx];
                                        if (JEDEC_string.IndexOf('=') != -1)
                                        {
                                            string[] JEDEC_content = JEDEC_string.Split('=');
                                            JEDEC_num = JEDEC_content[1].Substring(1, JEDEC_content[1].Length - 2);
                                            break;
                                        }
                                        else
                                        {
                                            JEDEC_num = JEDEC_string.Substring(1, JEDEC_string.Length - 2);
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (line2.Trim().Equals("END_PART"))
                                        {
                                            // reset the order of the JEDEC number and the flag
                                            number_idx = 0;
                                            flag4 = false;
                                        }
                                    }
                                }
                            }


                            // step 3: find the STL file of the part using the JEDEC number

                            try
                            {
                                string model_path = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) + @"ElectronicPartModels\HP-Lib\";
                                string model_package_path = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) + @"ElectronicPartModels\HP-Lib\part-packages\";
                                string[] subdirectoryEntries1 = Directory.GetDirectories(model_path);
                                foreach (string subdirectory in subdirectoryEntries1)
                                {
                                    if (!subdirectory.Equals(model_package_path))
                                    {
                                        string[] fileEntries = Directory.GetFiles(subdirectory);
                                        bool isfind = false;
                                        foreach (string s in fileEntries)
                                        {
                                            if (s.Contains(JEDEC_num) && s.Substring(s.Length - 3, 3).Equals("stl"))
                                            {
                                                isfind = true;
                                                p3D.ModelPath = s;
                                                break;
                                            }
                                        }

                                        if (isfind)
                                        {
                                            break;
                                        }
                                    }
                                }

                                p3D.Iscustom = false;
                            }
                            catch
                            {
                                p3D.ModelPath = "";
                                p3D.Iscustom = true;
                            }

                            #endregion

                            #region get the pin postions based on the XML file

                            if (!p3D.Iscustom)
                            {
                                string package_path1 = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) + @"ElectronicPartModels\HP-Lib\part-packages\packages\";
                                string package_path2 = curr_dir.Substring(0, curr_dir.IndexOf("ModelecMainInterface.gh")) + @"ElectronicPartModels\HP-Lib\part-packages\pr_packages\";

                                string[] packagefileEntries1 = Directory.GetFiles(package_path1);
                                string[] packagefileEntries2 = Directory.GetFiles(package_path2);

                                bool isxmlfind = false;

                                foreach (string entry in packagefileEntries1)
                                {
                                    string obtained_name = entry.Substring(entry.LastIndexOf('\\') + 1, entry.Length - entry.LastIndexOf('\\') - 1 - 8);  //.dra.xml so it minus 8
                                    obtained_name = obtained_name.ToUpper();

                                    if (JEDEC_num.Equals(obtained_name))
                                    {
                                        // find the model's package xml file
                                        XElement pinList = XElement.Load($"{entry}");
                                        IEnumerable<XElement> pindataElements = from item in pinList.Descendants("PIN-DEF")
                                                                                select item;
                                        foreach (XElement e in pindataElements)
                                        {
                                            if (e.Attribute("NUMBER").Value != "")
                                            {
                                                int pinnum = Int32.Parse(e.Attribute("NUMBER").Value);
                                                foreach (Pin p in p3D.Pins)
                                                {
                                                    if (p.PinNum == pinnum)
                                                    {
                                                        double x_offset = Double.Parse(e.Descendants("LOCATION").ElementAt(0).Attribute("X").Value);
                                                        double y_offset = Double.Parse(e.Descendants("LOCATION").ElementAt(0).Attribute("Y").Value);
                                                        double z_offset = 0;        // z is always 0 and the center of the 3D model should be on the XY plane

                                                        p.Pos = new Point3d(x_offset, y_offset, z_offset);
                                                    }
                                                }
                                            }
                                        }

                                        isxmlfind = true;
                                        break;
                                    }
                                }

                                if (!isxmlfind)
                                {
                                    // the file should be in pr_packages

                                    foreach (string entry in packagefileEntries2)
                                    {
                                        string obtained_name = entry.Substring(entry.LastIndexOf('\\') + 1, entry.Length - entry.LastIndexOf('\\') - 1 - 8);  // .dra.xml so it minus 8
                                        obtained_name = obtained_name.ToUpper();

                                        if (JEDEC_num.Equals(obtained_name))
                                        {
                                            // find the model's package xml file

                                            XElement pinList = XElement.Load($"{entry}");
                                            IEnumerable<XElement> pindataElements = from item in pinList.Descendants("PIN-DEF")
                                                                                    select item;
                                            foreach (XElement e in pindataElements)
                                            {
                                                if (e.Attribute("NUMBER").Value != "")
                                                {
                                                    int pinnum = Int32.Parse(e.Attribute("NUMBER").Value);
                                                    foreach (Pin p in p3D.Pins)
                                                    {
                                                        if (p.PinNum == pinnum)
                                                        {
                                                            double x_offset = Double.Parse(e.Descendants("LOCATION").ElementAt(0).Attribute("X").Value);
                                                            double y_offset = Double.Parse(e.Descendants("LOCATION").ElementAt(0).Attribute("Y").Value);
                                                            double z_offset = 0;        // z is always 0 and the center of the 3D model should be on the XY plane

                                                            p.Pos = new Point3d(x_offset, y_offset, z_offset);

                                                            break;  // find the pin so we can stop here
                                                        }
                                                    }
                                                }
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                p3D.Pins.Clear();
                            }
                            

                            #endregion
                        }
                        #endregion
                    }
                    break;
                default: break;
            }

            return result;
        }
        /// <summary>
        /// Get the list of parts that are included in the 2D schematic
        /// </summary>
        /// <param name="filename">the schematic file path</param>
        /// <param name="type">the file format of the schematic</param>
        /// <param name="scalar">the scalar of the schematic image</param>
        /// <returns></returns>
        public List<Part2D> GetPartsFromSchematic(string filename, int type, double scalar)
        {
            List<Part2D> result = new List<Part2D>();
            XElement circuitComponentList = XElement.Load($"{filename}");

            switch (type)
            {
                case 1:
                    {
                        // parse the circuit schematic created with Eagle
                        IEnumerable<XElement> circuitComponentElements = from item in circuitComponentList.Descendants("instance")
                                                                         select item;
                        foreach (XElement e in circuitComponentElements)
                        {
                            Part2D temp = new Part2D();

                            temp.PartName = e.Attribute("part").Value;
                            temp.PosX = double.Parse(e.Attribute("x").Value) * scalar;
                            temp.PosY = double.Parse(e.Attribute("y").Value) * scalar;

                            result.Add(temp); 
                        }
                    }
                    break;
                case 2:
                    {
                        // parse the circuit schematic created with Fritzing
                        IEnumerable<XElement> circuitComponentElements = from item in circuitComponentList.Descendants("instance")
                                                                         select item;

                        double min_x = double.MaxValue, min_y = double.MaxValue, max_x = double.MinValue, max_y = double.MinValue;

                        foreach (XElement e in circuitComponentElements)
                        {
                            if (e.Attribute("moduleIdRef").Value.Equals("TwoLayerRectanglePCBModuleID") || e.Attribute("moduleIdRef").Value.Equals("WireModuleID"))
                            {
                                if (e.Attribute("moduleIdRef").Value.Equals("WireModuleID"))
                                {
                                    if(e.Descendants("title") != null && e.Descendants("title").Count() != 0)
                                    {
                                        // get wires for calculating the boundaries of the circuit
                                        XElement geo = e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).Descendants("geometry").ElementAt(0);
                                        double ori_x = double.Parse(geo.Attribute("x").Value);
                                        double ori_y = double.Parse(geo.Attribute("y").Value);

                                        double ori_x_offset = double.Parse(geo.Attribute("x1").Value);
                                        double ori_y_offset = double.Parse(geo.Attribute("y1").Value);

                                        double ori_x_final = ori_x + ori_x_offset;
                                        double ori_y_final = ori_y + ori_y_offset;

                                        double end_x_ini = double.Parse(geo.Attribute("x2").Value.IndexOf("e-") != -1 ? "0" : geo.Attribute("x2").Value);
                                        double end_x = ori_x + ori_x_offset + end_x_ini;
                                        double end_y_ini = double.Parse(geo.Attribute("y2").Value.IndexOf("e-") != -1 ? "0" : geo.Attribute("y2").Value);
                                        double end_y = ori_y + ori_y_offset + end_y_ini;

                                        if (ori_x_final >= max_x)
                                            max_x = ori_x_final;

                                        if (ori_x_final <= min_x)
                                            min_x = ori_x_final;

                                        if (end_x >= max_x)
                                            max_x = end_x;

                                        if (end_x <= min_x)
                                            min_x = end_x;

                                        if (ori_y_final >= max_y)
                                            max_y = ori_y_final;

                                        if (ori_y_final <= min_y)
                                            min_y = ori_y_final;

                                        if (end_y >= max_y)
                                            max_y = end_y;

                                        if (end_y <= min_y)
                                            min_y = end_y;

                                    }
                                }
                            }
                            else
                            {
                                // meaningful part found
                                Part2D temp = new Part2D();

                                temp.PartName = e.Descendants("title").ElementAt(0).Value;
                                //temp.PosX = double.Parse(e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).
                                //    Descendants("geometry").ElementAt(0).Attribute("x").Value) * scalar;
                                //temp.PosY = double.Parse(e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).
                                //    Descendants("geometry").ElementAt(0).Attribute("y").Value) * scalar;

                                XElement trans = e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).
                                    Descendants("geometry").ElementAt(0);
                                bool isRotate = false;

                                if(trans.Descendants("transform") != null && trans.Descendants("transform").Count() != 0)
                                {
                                    isRotate = true;
                                }
                                else
                                {
                                    isRotate = false;
                                }

                                if (isRotate)
                                {
                                    temp.PosX = double.Parse(e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).
                                            Descendants("geometry").ElementAt(0).Attribute("x").Value) + 20;

                                    temp.PosY = double.Parse(e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).
                                                Descendants("geometry").ElementAt(0).Attribute("y").Value) - 5;
                                }
                                else
                                {
                                    temp.PosX = double.Parse(e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).
                                            Descendants("geometry").ElementAt(0).Attribute("x").Value);

                                    temp.PosY = double.Parse(e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).
                                                Descendants("geometry").ElementAt(0).Attribute("y").Value);
                                }


                                result.Add(temp);

                                // get the boundary of the part to determine if the boundary of the circuit

                                // might need to get the width and height of the part

                                if (temp.PosX >= max_x)
                                    max_x = temp.PosX;

                                if (temp.PosX <= min_x)
                                    min_x = temp.PosX;

                                if (temp.PosY >= max_y)
                                    max_y = temp.PosY;

                                if (temp.PosY <= min_y)
                                    min_y = temp.PosY;


                                #region count the part title as well for deciding the boundary of the circuit
                                double t_x = 0, t_y = 0;

                                XElement t_trans = e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).
                                    Descendants("titleGeometry").ElementAt(0);

                                bool isRotate_t = false;

                                if (t_trans.Descendants("transform") != null && t_trans.Descendants("transform").Count() != 0)
                                {
                                    isRotate_t = true;
                                }
                                else
                                {
                                    isRotate_t = false;
                                }

                                if (isRotate_t)
                                {
                                    t_x = double.Parse(e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).
                                        Descendants("titleGeometry").ElementAt(0).Attribute("x").Value) + 20;
                                    t_y = double.Parse(e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).
                                        Descendants("titleGeometry").ElementAt(0).Attribute("y").Value) - 5;
                                }
                                else
                                {
                                    t_x = double.Parse(e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).
                                        Descendants("titleGeometry").ElementAt(0).Attribute("x").Value);
                                    t_y = double.Parse(e.Descendants("views").ElementAt(0).Descendants("schematicView").ElementAt(0).
                                        Descendants("titleGeometry").ElementAt(0).Attribute("y").Value);
                                }

                                if (t_x >= max_x)
                                    max_x = t_x;

                                if (t_x <= min_x)
                                    min_x = t_x;

                                if (t_y >= max_y)
                                    max_y = t_y;

                                if (t_y <= min_y)
                                    min_y = t_y;
                                #endregion

                            }
                        }


                        #region update all the part's position in the interafce

                        foreach(Part2D prt2D in result)
                        {
                            double x_real = (prt2D.PosX - min_x);
                            double y_real = (prt2D.PosY - min_y);

                            prt2D.PosX = x_real;
                            prt2D.PosY = y_real;
                        }
                        #endregion
                    }
                    break;
                case 3:
                    {
                        string csvFileName = filename.Substring(0, filename.IndexOf('.')) + ".csv";

                        bool flag = false;
                        string line;

                        int x_min = Int32.MaxValue, x_max = Int32.MinValue;
                        int y_min = Int32.MaxValue, y_max = Int32.MinValue;

                        #region get the origin, width, and height of the region

                        // Read the file and display it line by line.  
                        System.IO.StreamReader file = new System.IO.StreamReader(csvFileName);
                        while ((line = file.ReadLine()) != null)
                        {
                            if (!flag)
                            {
                                if (line.Length == 0) continue;

                                if (line.Substring(0, 1).Equals("%"))
                                {
                                    flag = true;
                                }
                            }
                            else
                            {
                                // get the positions
                                int idx_left = line.IndexOf('(');
                                int idx_right = line.IndexOf(')');
                                string coordinatesString = line.Substring(idx_left + 1, idx_right - idx_left - 1);

                                string[] coordinates = coordinatesString.Split(',');
                                int coorX = 0, coorY = 0;
                                try
                                {
                                    coorX = (int)(Int32.Parse(coordinates[0]) * 0.70);
                                    coorY = (int)(Int32.Parse(coordinates[1]) * 0.61);

                                    if (coorX <= x_min)
                                    {
                                        x_min = coorX;
                                    }

                                    if (coorX >= x_max)
                                    {
                                        x_max = coorX;
                                    }

                                    if (coorY <= y_min)
                                    {
                                        y_min = coorY;
                                    }

                                    if (coorY >= y_max)
                                    {
                                        y_max = coorY;
                                    }

                                }
                                catch(Exception e) { }
                                flag = false;
                            }
                        }

                        int range = 400;

                        string pngFile = filename.Substring(0, filename.IndexOf('.')) + ".png";
                        Image img = Image.FromFile(pngFile);
                        int imgW = img.Width;
                        int imgH = img.Height;

                        int orig_x = ((x_min - range) < 0) ? 0 : (x_min - range);
                        int w = (((x_max + range) > imgW) ? imgW : (x_max + range)) - (((x_min - range) < 0) ? 0 : (x_min - range));
                        int orig_y = imgH - (((y_max + range) > imgH) ? imgH : (y_max + range));
                        int h = (((y_max + range) > imgH) ? imgH : (y_max + range)) - (((y_min - range) < 0) ? 0 : (y_min - range));

                        #endregion

                        #region get the parts
                        System.IO.StreamReader file1 = new System.IO.StreamReader(csvFileName);
                        while ((line = file1.ReadLine()) != null)
                        {
                            if (!flag)
                            {
                                if (line.Length == 0) continue;

                                if (line.Substring(0, 1).Equals("%"))
                                {
                                    flag = true;
                                }
                            }
                            else
                            {
                                string[] segs = line.Split(',');
                                string partName = segs[4].Substring(1, segs[4].Length - 2);
                                string suffix = segs[5].Substring(1, segs[5].Length - 3);
                                partName = partName + "-" + suffix;

                                // get the positions
                                double posX = 0, posY = 0;
                                int idx_left = line.IndexOf('(');
                                int idx_right = line.IndexOf(')');
                                string coordinatesString = line.Substring(idx_left + 1, idx_right - idx_left - 1);

                                string[] coordinates = coordinatesString.Split(',');
                                int coorX = 0, coorY = 0;
                                try
                                {
                                    coorX = (int)(Int32.Parse(coordinates[0]) * 0.65);
                                    coorY = (int)(Int32.Parse(coordinates[1]) * 0.65);

                                    posX = (coorX - orig_x) * scalar;
                                    posY = (imgH - coorY - orig_y) * scalar;

                                }
                                catch (Exception e) { }

                                Part2D temp = new Part2D(partName, posX, posY);
                                result.Add(temp);

                                flag = false;
                            }
                        }
                        #endregion
                    }
                    break;
                default: break;
            }
               
            return result;
        }

        /// <summary>
        /// Get the list of connected parts based on the connection net
        /// </summary>
        /// <param name="filename">the schematic file path</param>
        /// <param name="type">the file format of the schematic</param>
        /// <returns></returns>
        public List<ConnectionNet> GetConnectionNetFromSchematic(string filename, int type)
        {
            List<ConnectionNet> result = new List<ConnectionNet>();

            switch (type)
            {
                case 1:
                    {
                        // parse the circuit schematic created with Eagle
                        bool flag = false;
                        string line;
                        ConnectionNet temp = new ConnectionNet();
                        string filedir = filename.Substring(0, filename.LastIndexOf('.')) + ".netlist";

                        // Read the file and display it line by line.  
                        System.IO.StreamReader file = new System.IO.StreamReader(filedir);
                        while ((line = file.ReadLine()) != null)
                        {
                            if (!flag)
                            {
                                if (line.Length == 0) continue;

                                if (line.Substring(0, 4).Equals("Net "))
                                {
                                    flag = true;
                                }
                            }
                            else
                            {
                                if (line.Length == 0)
                                {
                                    if (temp.NetName != "")
                                    {
                                        // add the net to the final result
                                        result.Add(temp);
                                        temp = new ConnectionNet();
                                    }
                                }
                                else
                                {
                                    if (line.Substring(0, 1).Equals("N"))
                                    {
                                        // the first line for a new net
                                        // create a new net
                                        string netName = "";
                                        int idx = 0;
                                        while (line.ElementAt(idx) != ' ')
                                        {
                                            idx++;
                                        }
                                        netName = line.Substring(0, idx);
                                        temp.NetName = netName;

                                        PartPinPair tempPP = new PartPinPair();
                                        while (line.ElementAt(idx) == ' ')
                                        {
                                            idx++;
                                        }
                                        int part_fir_pos = idx;
                                        while (line.ElementAt(idx) != ' ')
                                        {
                                            idx++;
                                        }
                                        tempPP.PartName = line.Substring(part_fir_pos, idx - part_fir_pos);

                                        while (line.ElementAt(idx) == ' ')
                                        {
                                            idx++;
                                        }
                                        int pin_fir_pos = idx;
                                        while (line.ElementAt(idx) != ' ')
                                        {
                                            idx++;
                                        }
                                        tempPP.PinName = line.Substring(pin_fir_pos, idx - pin_fir_pos);

                                        temp.PartPinPairs.Add(tempPP);
                                    }
                                    else
                                    {
                                        // record the part and pin for the same net
                                        int idx = 0;
                                        PartPinPair tempPP = new PartPinPair();

                                        while (line.ElementAt(idx) == ' ')
                                        {
                                            idx++;
                                        }
                                        int part_fir_pos = idx;
                                        while (line.ElementAt(idx) != ' ')
                                        {
                                            idx++;
                                        }
                                        tempPP.PartName = line.Substring(part_fir_pos, idx - part_fir_pos);

                                        while (line.ElementAt(idx) == ' ')
                                        {
                                            idx++;
                                        }
                                        int pin_fir_pos = idx;
                                        while (line.ElementAt(idx) != ' ')
                                        {
                                            idx++;
                                        }
                                        tempPP.PinName = line.Substring(pin_fir_pos, idx - pin_fir_pos);

                                        temp.PartPinPairs.Add(tempPP);
                                    }
                                }
                            }
                        }

                        file.Close();
                    }
                    break;
                case 2:
                    {
                        // parse the circuit schematic created with Fritzing
                        string filedir = filename.Substring(0, filename.LastIndexOf('.')) + ".xml";
                        XElement netList = XElement.Load($"{filedir}");
                        IEnumerable<XElement> connectionElements = from item in netList.Descendants("net")
                                                                         select item;

                        int netID = 1;
                        foreach (XElement e in connectionElements)
                        {
                            ConnectionNet temp = new ConnectionNet();

                            string netName = "N$" + netID.ToString();
                            temp.NetName = netName;
                            
                            foreach(var con in e.Descendants("connector"))
                            {
                                PartPinPair tempPP = new PartPinPair();

                                tempPP.PinName = con.Attribute("id").Value;
                                tempPP.PartName = con.Descendants("part").ElementAt(0).Attribute("label").Value;
                                temp.PartPinPairs.Add(tempPP);
                            }

                            result.Add(temp);
                            netID++;
                        }
                    }
                    break;
                case 3:
                    {
                        XElement netList = XElement.Load($"{filename}");
                        IEnumerable<XElement> connectionElements = from item in netList.Descendants("nets")
                                                                   select item;
                        foreach (XElement e in connectionElements.ElementAt(0).Descendants("net"))
                        {
                            ConnectionNet temp = new ConnectionNet();

                            string netName = e.Descendants("id").ElementAt(0).Value;
                            temp.NetName = netName;

                            // get the parts and pins
                            List<PartPinPair> part_pin_pairs = GetPartNameByNetName(filename, netName);
                            temp.PartPinPairs = part_pin_pairs;

                            result.Add(temp);
                        }

                    }
                    break;
                default: break;
            }

            return result;
        }

        private List<PartPinPair> GetPartNameByNetName(string filename, string netname)
        {
            List<PartPinPair> result = new List<PartPinPair>();

            XElement netList = XElement.Load($"{filename}");
            IEnumerable<XElement> connectionElements = from item in netList.Descendants("instance")
                                                       select item;

            foreach(XElement e in connectionElements)
            {
                IEnumerable<XElement> pins = e.Descendants("pin");
                foreach(XElement p in pins)
                {
                    string netNameTest = p.Descendants("connection").Attributes("net").ElementAt(0).Value;
                    if (netname.Equals(netNameTest)){
                        string partname = e.Descendants("id").ElementAt(0).Value;
                        string pinname = "";

                        string termID = p.Descendants("termid").ElementAt(0).Value;
                        string cellID = e.Descendants("cellid").ElementAt(0).Value;

                        IEnumerable<XElement> cellElements = from item in netList.Descendants("cell")
                                                                   select item;

                        foreach(XElement c in cellElements)
                        {
                            string cellIDTest = c.Descendants("id").ElementAt(0).Value;

                            if (cellID.Equals(cellIDTest))
                            {
                                // correct cell
                                IEnumerable<XElement> terms = c.Descendants("term");

                                foreach(XElement t in terms)
                                {
                                    string termIDTest = t.Descendants("id").ElementAt(0).Value;

                                    if (termID.Equals(termIDTest))
                                    {
                                        pinname = t.Descendants("name").ElementAt(0).Value.ToUpper();
                                    }
                                }
                            }
                        }

                        PartPinPair temp = new PartPinPair(partname, pinname);
                        result.Add(temp);
                    }
                }
            }

            return result;
        }
        #region old code
        //public void CreateSchematicFromFritzingFiles(List<string> fzFileName)
        //{
        //    XmlDocument xmlDoc = new XmlDocument();
        //    XmlNode rootNode = xmlDoc.CreateElement("schematics");
        //    xmlDoc.AppendChild(rootNode);

        //    foreach (string filename in fzFileName)
        //    {
        //        XmlNode schematicNode = xmlDoc.CreateElement("schematic");
        //        XmlAttribute schematicAttribute = xmlDoc.CreateAttribute("schematicID");
        //        schematicAttribute.Value = filename;
        //        schematicNode.Attributes.Append(schematicAttribute);

        //        #region Find each part and record them in the xml
        //        var currentDirectory = Directory.GetCurrentDirectory();
        //        string fritzingFile = @"circuit-lib\" + filename + ".fz";

        //        var circuitFilepath = Path.Combine(currentDirectory, fritzingFile);

        //        XElement circuitPartList = XElement.Load($"{circuitFilepath}");
        //        IEnumerable<XElement> circuitComponentElements = from item in circuitPartList.Descendants("instances").Descendants("instance")
        //                                                         where (item.Element("title") != null && item.Element("title").Value.IndexOf("Wire") == -1 && item.Element("title").Value.IndexOf("PCB") == -1)
        //                                                         select item;

        //        #region Calibrate the coordinates of the left-corner in the Fritzing schematic
        //        double min_x = 1000000000000000000, min_y = 1000000000000000000;
        //        double max_x = -1, max_y = -1;

        //        XElement circuitTraceList = XElement.Load($"{circuitFilepath}");
        //        IEnumerable<XElement> circuitTraceElements = from item in circuitTraceList.Descendants("instances").Descendants("instance")
        //                                                     where (item.Element("title") != null && item.Element("title").Value.IndexOf("Wire") != -1)
        //                                                     select item;

        //        foreach (XElement cirComp in circuitComponentElements)
        //        {
        //            double temp_x = 0, temp_y = 0;

        //            temp_x = Double.Parse(cirComp.Element("views").Element("schematicView").Element("geometry").Attribute("x").Value);
        //            temp_y = Double.Parse(cirComp.Element("views").Element("schematicView").Element("geometry").Attribute("y").Value);

        //            if (temp_x <= min_x)
        //                min_x = temp_x;

        //            if (temp_y <= min_y)
        //                min_y = temp_y;

        //            if (temp_x >= max_x)
        //                max_x = temp_x;

        //            if (temp_y >= max_y)
        //                max_y = temp_y;
        //        }

        //        foreach (XElement cirPos in circuitTraceElements)
        //        {
        //            double temp_x1 = 0, temp_y1 = 0, temp_x2 = 0, temp_y2 = 0;

        //            temp_x1 = Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("x").Value) +
        //                        Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("x1").Value);
        //            temp_y1 = Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("y").Value) +
        //                        Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("y1").Value);

        //            temp_x2 = Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("x").Value) +
        //                        Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("x2").Value);
        //            temp_y2 = Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("y").Value) +
        //                        Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("y2").Value);

        //            if (temp_x1 <= min_x)
        //                min_x = temp_x1;

        //            if (temp_y1 <= min_y)
        //                min_y = temp_y1;

        //            if (temp_x2 <= min_x)
        //                min_x = temp_x2;

        //            if (temp_y2 <= min_y)
        //                min_y = temp_y2;

        //            if (temp_x1 >= max_x)
        //                max_x = temp_x1;

        //            if (temp_y1 >= max_y)
        //                max_y = temp_y1;

        //            if (temp_x2 >= max_x)
        //                max_x = temp_x2;

        //            if (temp_y2 >= max_y)
        //                max_y = temp_y2;
        //        }
        //        #endregion

        //        Dictionary<string, string> partIDtoid = new Dictionary<string, string>();

        //        #region add parts to the schematic
        //        int p_id = 0;
        //        foreach (XElement circomp in circuitComponentElements)
        //        {
        //            XmlNode partNode = xmlDoc.CreateElement("part");
        //            XmlAttribute partAttribute = xmlDoc.CreateAttribute("partID");
        //            XmlAttribute pAttribute = xmlDoc.CreateAttribute("id");

        //            // Part's properties
        //            XmlAttribute pnameAttribute = xmlDoc.CreateAttribute("partName");
        //            XmlAttribute packageAttribute = xmlDoc.CreateAttribute("Package");
        //            XmlAttribute familyAttribute = xmlDoc.CreateAttribute("Family");
        //            XmlAttribute variantAttribute = xmlDoc.CreateAttribute("Variant");
        //            XmlAttribute descriptionAttribute = xmlDoc.CreateAttribute("Description");

        //            p_id++;
        //            partAttribute.Value = circomp.Element("title").Value;
        //            pAttribute.Value = p_id.ToString();

        //            partIDtoid.Add(partAttribute.Value, pAttribute.Value);

        //            string url = circomp.Attribute("path").Value;
        //            XElement partInfoList = XElement.Load($"{url}");

        //            pnameAttribute.Value = partInfoList.Element("title").Value;
        //            descriptionAttribute.Value = partInfoList.Element("description").Value;

        //            IEnumerable<XElement> partPropertiesElements = from item in partInfoList.Descendants("properties").Descendants("property")
        //                                                           select item;

        //            foreach (XElement pp in partPropertiesElements)
        //            {
        //                if (pp.Attribute("name").Value.Equals("package"))
        //                    packageAttribute.Value = pp.Value;

        //                if (pp.Attribute("name").Value.Equals("family"))
        //                    familyAttribute.Value = pp.Value;

        //                if (pp.Attribute("name").Value.Equals("variant"))
        //                    variantAttribute.Value = pp.Value;
        //            }

        //            partNode.Attributes.Append(partAttribute);
        //            partNode.Attributes.Append(pAttribute);
        //            partNode.Attributes.Append(pnameAttribute);
        //            partNode.Attributes.Append(packageAttribute);
        //            partNode.Attributes.Append(familyAttribute);
        //            partNode.Attributes.Append(variantAttribute);
        //            partNode.Attributes.Append(descriptionAttribute);

        //            // convert and add the position values 
        //            XmlNode positionNode = xmlDoc.CreateElement("position");
        //            double raw_x = 0, raw_y = 0, real_x = 0, real_y = 0;
        //            raw_x = Double.Parse(circomp.Element("views").Element("schematicView").Element("geometry").Attribute("x").Value);
        //            raw_y = Double.Parse(circomp.Element("views").Element("schematicView").Element("geometry").Attribute("y").Value);

        //            double offset_center_x = 235 - (max_x + min_x) / 2;
        //            double offset_center_y = 138 - (max_y + min_y) / 2;
        //            real_x = raw_x + offset_center_x;
        //            real_y = raw_y + offset_center_y;

        //            //Point2d circenter = new Point2d((max_x - min_x) / 2 + offset_center_x, (max_y - min_y) / 2 + offset_center_y);
        //            //Point2d oldPos = new Point2d(real_x, real_y);
        //            //Vector2d fromVector = oldPos - circenter;
        //            //double scalefactor = 1;
        //            //Point2d newPos = scalefactor * fromVector + circenter;

        //            //positionNode.InnerText = (Convert.ToInt32(newPos.X)).ToString() + "," + (Convert.ToInt32(newPos.Y)).ToString();

        //            positionNode.InnerText = (Convert.ToInt32(real_x)).ToString() + "," + (Convert.ToInt32(real_y)).ToString();

        //            // convert and add the rotation values
        //            XmlNode rotationNode = xmlDoc.CreateElement("rotation");
        //            if (circomp.Element("views").Element("schematicView").Element("geometry").Element("transform") != null)
        //            {
        //                double cosvalue = 0, sinvalue = 0;
        //                cosvalue = Double.Parse(circomp.Element("views").Element("schematicView").Element("geometry").Element("transform").Attribute("m11").Value);
        //                sinvalue = Double.Parse(circomp.Element("views").Element("schematicView").Element("geometry").Element("transform").Attribute("m12").Value);

        //                if (cosvalue == 0 && sinvalue == -1)
        //                    rotationNode.InnerText = "270";
        //                else if (cosvalue == -1 && sinvalue == 0)
        //                    rotationNode.InnerText = "180";
        //                else if (cosvalue == 0 && sinvalue == 1)
        //                    rotationNode.InnerText = "90";
        //                else
        //                    rotationNode.InnerText = "0";
        //            }
        //            else
        //            {
        //                // no rotation
        //                rotationNode.InnerText = "0";
        //            }

        //            partNode.AppendChild(positionNode);
        //            partNode.AppendChild(rotationNode);
        //            schematicNode.AppendChild(partNode);
        //        }
        //        #endregion

        //        #region add traces to the schematic for 2D schematic preview
        //        int t_id = 0;
        //        XmlNode tracesNode = xmlDoc.CreateElement("traces");
        //        foreach (XElement cirPos in circuitTraceElements)
        //        {
        //            XmlNode traceNode = xmlDoc.CreateElement("trace");
        //            t_id++;
        //            XmlAttribute tidAttribute = xmlDoc.CreateAttribute("id");
        //            tidAttribute.Value = t_id.ToString();
        //            traceNode.Attributes.Append(tidAttribute);

        //            double from_x = 0, from_y = 0, to_x = 0, to_y = 0;
        //            double offset_center_x = 235 - (max_x + min_x) / 2;
        //            double offset_center_y = 138 - (max_y + min_y) / 2;

        //            from_x = Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("x").Value) +
        //                        Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("x1").Value) + offset_center_x;
        //            from_y = Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("y").Value) +
        //                        Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("y1").Value) + offset_center_y;

        //            to_x = Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("x").Value) +
        //                        Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("x2").Value) + offset_center_x;
        //            to_y = Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("y").Value) +
        //                        Double.Parse(cirPos.Element("views").Element("schematicView").Element("geometry").Attribute("y2").Value) + offset_center_y;

        //            //Point2d circenter = new Point2d((max_x-min_x)/2+offset_center_x, (max_y-min_y)/2+offset_center_y);
        //            //Point2d oldFromPoint = new Point2d(from_x, from_y);
        //            //Point2d oldToPoint = new Point2d(to_x, to_y);
        //            //Vector2d fromVector = oldFromPoint - circenter;
        //            //Vector2d toVector = oldToPoint - circenter;
        //            //double scalefactor = 1;
        //            //Point2d newFromPoint = scalefactor* fromVector + circenter;
        //            //Point2d newToPoint = scalefactor * toVector + circenter;

        //            //traceNode.InnerText = (Convert.ToInt32(newFromPoint.X)).ToString() + "," + (Convert.ToInt32(newFromPoint.Y)).ToString() + "," +
        //            //                    (Convert.ToInt32(newToPoint.X)).ToString() + "," + (Convert.ToInt32(newToPoint.Y)).ToString();
        //            traceNode.InnerText = (Convert.ToInt32(from_x)).ToString() + "," + (Convert.ToInt32(from_y)).ToString() + "," +
        //                                (Convert.ToInt32(to_x)).ToString() + "," + (Convert.ToInt32(to_y)).ToString();


        //            tracesNode.AppendChild(traceNode);
        //        }
        //        schematicNode.AppendChild(tracesNode);
        //        #endregion

        //        #region add connections to the schematic for 3D tracing
        //        XmlNode connectionsNode = xmlDoc.CreateElement("connections");

        //        string netlistFile = @"circuit-lib\" + filename + ".xml";
        //        var netlistFilepath = Path.Combine(currentDirectory, netlistFile);
        //        XElement connectionList = XElement.Load($"{netlistFilepath}");
        //        IEnumerable<XElement> connectionNetElements = from item in connectionList.Descendants("net")
        //                                                      select item;

        //        int net_id = 0;
        //        foreach (XElement connection in connectionNetElements)
        //        {
        //            if (connection.Descendants("connector").Count() > 1)
        //            {
        //                net_id++;
        //                XmlNode connectionNode = xmlDoc.CreateElement("connection");
        //                XmlAttribute conidAttribute = xmlDoc.CreateAttribute("id");
        //                conidAttribute.Value = net_id.ToString();
        //                connectionNode.Attributes.Append(conidAttribute);

        //                foreach (XElement connector in connection.Descendants("connector"))
        //                {
        //                    XmlNode connectorNode = xmlDoc.CreateElement("connector");
        //                    XmlAttribute partidAttribute = xmlDoc.CreateAttribute("part");
        //                    XmlAttribute pinidAttribute = xmlDoc.CreateAttribute("pin");
        //                    string keyPartLabel = connector.Element("part").Attribute("label").Value;
        //                    string partID = partIDtoid[keyPartLabel];
        //                    partidAttribute.Value = partID;
        //                    pinidAttribute.Value = connector.Attribute("id").Value;

        //                    connectorNode.Attributes.Append(partidAttribute);
        //                    connectorNode.Attributes.Append(pinidAttribute);

        //                    connectionNode.AppendChild(connectorNode);
        //                }

        //                connectionsNode.AppendChild(connectionNode);
        //            }
        //        }
        //        schematicNode.AppendChild(connectionsNode);
        //        #endregion

        //        #endregion

        //        rootNode.AppendChild(schematicNode);
        //    }

        //    xmlDoc.Save(@"database\schematics.xml");
        //}




        //public UserIntent GetUserIntentByID(string ui_id)
        //{
        //    UserIntent result = new UserIntent();
        //    var filename = "database\\user_intents.xml";
        //    var currentDirectory = Directory.GetCurrentDirectory();
        //    var userIntentFilepath = Path.Combine(currentDirectory, filename);

        //    XElement userIntentList = XElement.Load($"{userIntentFilepath}");

        //    IEnumerable<XElement> userIntentElements = from item in userIntentList.Descendants("function")
        //                                               where ((string)item.Attribute("userintentID")).Equals(ui_id)
        //                                               select item;

        //    if (userIntentElements.Count() != 0)
        //    {
        //        result.Descriptor = (string)userIntentElements.ElementAt(0).Element("description").Value;
        //        result.UIID = ui_id;
        //        result.SchematicID = userIntentElements.ElementAt(0).Element("schematicID").Value;
        //    }
        //    return result;
        //}

        //public void AddItemsToSchematic(List<Part> partList, List<Trace> traceList, string sid)
        //{
        //    var filename = "database\\schematics.xml";
        //    var currentDirectory = Directory.GetCurrentDirectory();
        //    var schematicFilepath = Path.Combine(currentDirectory, filename);

        //    XmlDocument doc = new XmlDocument();
        //    doc.Load($"{schematicFilepath}");
        //    XmlNodeList schematicElements = doc.SelectNodes("descendant::schematic[@schematicID=sid]");

        //    foreach (XmlElement node in schematicElements)
        //    {
        //        foreach (Part p in partList)
        //        {
        //            XmlElement el = doc.CreateElement("part");
        //            XmlElement pos = doc.CreateElement("position");

        //            // To-Do: finish the implementation of adding new nodes to the xml
        //            node.AppendChild(el);

        //        }
        //    }
        //}
        //public Schematic GetSchematicByID(string s_id)
        //{
        //    Schematic result = new Schematic();
        //    result.UIID = s_id;
        //    var filename = "database\\schematics.xml";
        //    var currentDirectory = Directory.GetCurrentDirectory();
        //    var schematicFilepath = Path.Combine(currentDirectory, filename);
        //    XElement schematicList = XElement.Load($"{schematicFilepath}");
        //    IEnumerable<XElement> schematicElements = from item in schematicList.Descendants("schematic")
        //                                              where ((string)item.Attribute("schematicID")).Equals(s_id.ToString())
        //                                              select item;

        //    if (schematicElements.Count() != 0)
        //    {

        //        #region get all the parts for the particular schematic
        //        result.S_ID = s_id;
        //        IEnumerable<XElement> partGroup = from item in schematicElements.ElementAt(0).Descendants("part")
        //                                          select item;
        //        foreach (XElement p in partGroup)
        //        {
        //            var pID = (string)(p.Attribute("partName"));
        //            var id = Int32.Parse((string)p.Attribute("id"));
        //            IEnumerable<XElement> positions = from item in p.Descendants("position")
        //                                              select item;
        //            IEnumerable<XElement> rotations = from item in p.Descendants("rotation")
        //                                              select item;
        //            string pos_string = "";
        //            string rotation_string = "";
        //            if (positions.Count() != 0 && rotations.Count() != 0)
        //            {

        //                // these are position and rotation of the 2D symbol
        //                pos_string = positions.ElementAt(0).Value;
        //                rotation_string = rotations.ElementAt(0).Value;

        //                Part pRep = GetPart(pID);
        //                pRep.ID = id;
        //                pRep.S_ID = s_id;
        //                pRep.Rotation = Convert.ToDouble(rotation_string);
        //                string imgpath = "database/" + pRep.DirSymbol;
        //                System.Drawing.Image img = System.Drawing.Image.FromFile(imgpath);
        //                pRep.Width2D = img.Width;
        //                pRep.Height2D = img.Height;

        //                string[] poslist = pos_string.Split(',');
        //                pRep.Center2D = new Point2d(Convert.ToDouble(poslist[0]) + pRep.Width2D / 2, Convert.ToDouble(poslist[1]) + pRep.Height2D / 2);

        //                result.Parts.Add(pRep);
        //            }
        //        }
        //        #endregion

        //        #region get all the 2D traces for the particular schematic
        //        IEnumerable<XElement> traceGroup = from item in schematicElements.ElementAt(0).Descendants("trace")
        //                                           select item;
        //        foreach (XElement t in traceGroup)
        //        {
        //            List<Point2d> singleTrace = new List<Point2d>();

        //            string points_string = t.Value;
        //            string[] ptPosList = points_string.Split(',');
        //            for (int idx = 0; idx < ptPosList.Length; idx++)
        //            {
        //                if (idx % 2 == 1)
        //                {
        //                    double x = Convert.ToDouble(ptPosList[idx - 1]);
        //                    double y = Convert.ToDouble(ptPosList[idx]);
        //                    Point2d pt = new Point2d(x, y);
        //                    singleTrace.Add(pt);
        //                }
        //            }

        //            result.SchematicTracing2D.Add(singleTrace);
        //        }
        //        #endregion

        //        #region get all the 3D traces for the particular schematic
        //        IEnumerable<XElement> connectionGroup = from item in schematicElements.ElementAt(0).Descendants("connection")
        //                                                select item;
        //        foreach (XElement con in connectionGroup)
        //        {
        //            Trace tRep = new Trace();
        //            tRep.ID = Int32.Parse(con.Attribute("id").Value);
        //            tRep.SID = s_id;

        //            foreach (XElement c in con.Descendants("connector"))
        //            {
        //                int idx_part = Int32.Parse(c.Attribute("part").Value);
        //                string idx_pin = c.Attribute("pin").Value;

        //                string partContent = idx_part.ToString() + '-' + idx_pin;

        //                tRep.Partnpin.Add(partContent);
        //            }
        //            tRep.TracePoints3D = new List<Point3d>();

        //            result.Traces.Add(tRep);
        //        }
        //        #endregion

        //        #region [commented] get all the labels for connections
        //        //IEnumerable<XElement> connectionLablesGroup = from item in schematicElements.ElementAt(0).Descendants("label")
        //        //                                   select item;
        //        //foreach (XElement cl in connectionLablesGroup)
        //        //{
        //        //    var posx = Int32.Parse((cl.Attribute("x").Value));
        //        //    var posy = Int32.Parse((cl.Attribute("y").Value));
        //        //    string label = cl.Value;

        //        //    ConnectionLabel connection = new ConnectionLabel();

        //        //    connection.LabelName = label;
        //        //    connection.Position = new Point2d(posx, posy);

        //        //    result.ConnectionLabels.Add(connection);
        //        //}
        //        #endregion
        //    }

        //    return result;
        //}

        //public string PartIDfromSchematicToXML(string partID_schematic)
        //{
        //    if (partID_schematic.IndexOf("esistor") != -1)
        //    {
        //        return "p1";
        //    }
        //    else if (partID_schematic.IndexOf("LEDs") != -1)
        //    {
        //        return "p5";
        //    }
        //    else if (partID_schematic.IndexOf("attery") != -1)
        //    {
        //        return "p3";
        //    }
        //    else if (partID_schematic.IndexOf("LIPO") != -1)
        //    {
        //        return "p11";
        //    }
        //    else if (partID_schematic.IndexOf("Pulse") != -1)
        //    {
        //        return "p4";
        //    }
        //    else if (partID_schematic.IndexOf("LED") != -1)
        //    {
        //        return "p2";
        //    }
        //    else if (partID_schematic.IndexOf("Pro Trinket") != -1)
        //    {
        //        return "p23";
        //    }
        //    else if (partID_schematic.IndexOf("Trinket") != -1)
        //    {
        //        return "p6";
        //    }
        //    else if (partID_schematic.IndexOf("atmega328") != -1)
        //    {
        //        return "p7";
        //    }
        //    else if (partID_schematic.IndexOf("apacitor") != -1)
        //    {
        //        return "p24";
        //    }
        //    else if (partID_schematic.IndexOf("esonator") != -1)
        //    {
        //        return "p25";
        //    }
        //    else if (partID_schematic.IndexOf("otor") != -1)
        //    {
        //        return "p12";
        //    }
        //    else if (partID_schematic.IndexOf("iode") != -1)
        //    {
        //        return "p14";
        //    }
        //    else if (partID_schematic.IndexOf("MOSFET") != -1)
        //    {
        //        return "p15";
        //    }
        //    else if (partID_schematic.IndexOf("iezo") != -1)
        //    {
        //        return "p16";
        //    }
        //    else if (partID_schematic.IndexOf("PIR") != -1)
        //    {
        //        return "p17";
        //    }
        //    else if (partID_schematic.IndexOf("Bitsy") != -1)
        //    {
        //        return "p18";
        //    }
        //    else if (partID_schematic.IndexOf("LBT") != -1)
        //    {
        //        return "p19";
        //    }
        //    else if (partID_schematic.IndexOf("MAX7219") != -1)
        //    {
        //        return "p20";
        //    }
        //    else if (partID_schematic.IndexOf("witch") != -1)
        //    {
        //        return "p21";
        //    }
        //    else if (partID_schematic.IndexOf("TOUCH") != -1)
        //    {
        //        return "p22";
        //    }
        //    else
        //    {
        //        return "unknown";
        //    }

        //}
        //public Part GetPart(string pid)
        //{
        //    // replace all the parts with the part file from Fritzing 
        //    Part result = new Part();
        //    var filename = "database\\parts.xml";
        //    var currentDirectory = Directory.GetCurrentDirectory();
        //    var partFilepath = Path.Combine(currentDirectory, filename);

        //    XElement partList = XElement.Load($"{partFilepath}");

        //    IEnumerable<XElement> partElements = from item in partList.Descendants("part")
        //                                         where ((string)item.Attribute("partID")).Equals(PartIDfromSchematicToXML(pid))
        //                                         select item;


        //    if (partElements.Count() != 0)
        //    {
        //        result.P_ID = partElements.ElementAt(0).Attribute("partID").Value;
        //        result.Name = partElements.ElementAt(0).Element("name").Value;
        //        result.Category = partElements.ElementAt(0).Element("category").Value;

        //        result.Center3D = new Point3d();


        //        result.DirPackageModel = partElements.ElementAt(0).Element("package").Value;
        //        result.DirSocketModel = partElements.ElementAt(0).Element("socket").Value;
        //        result.SocketType = partElements.ElementAt(0).Element("socket").Attribute("type").Value;
        //        result.DirSymbol = partElements.ElementAt(0).Element("schematics").Value;
        //        result.DirPartImage = partElements.ElementAt(0).Element("image").Value;

        //        result.Height3D = Convert.ToDouble(partElements.ElementAt(0).Element("height").Value);
        //        result.Width3D = Convert.ToDouble(partElements.ElementAt(0).Element("width").Value);
        //        result.Length3D = Convert.ToDouble(partElements.ElementAt(0).Element("length").Value);

        //        result.IsAttachable = (partElements.ElementAt(0).Element("attachable").Value == "No") ? false : true;

        //        // add the embedment options that the part has
        //        string embed_string = partElements.ElementAt(0).Element("embedment").Value;
        //        if (embed_string.IndexOf(',') != 0)
        //        {
        //            string[] embedVals = embed_string.Split(',');
        //            foreach (string v in embedVals)
        //            {
        //                result.Embedment.Add(Int32.Parse(v));
        //            }
        //        }
        //        else
        //        {
        //            result.Embedment.Add(Int32.Parse(embed_string));
        //        }

        //        // add all properties to the list of parameters of the part
        //        IEnumerable<XElement> paraGroup = from item in partElements.ElementAt(0).Descendants("property")
        //                                          select item;
        //        foreach (var para in paraGroup)
        //        {
        //            result.Parameters.Add((string)para.Attribute("type"), (string)para.Value);
        //        }

        //        // add all pins that the part has
        //        IEnumerable<XElement> pinGroup = from item in partElements.ElementAt(0).Descendants("pin")
        //                                         select item;
        //        foreach (XElement pin in pinGroup)
        //        {
        //            var ptype = (string)(pin.Attribute("type"));
        //            var pname = (string)(pin.Attribute("name"));
        //            var pinid = (string)(pin.Attribute("ID"));
        //            double d = Convert.ToDouble((string)(pin.Descendants("diameter").ElementAt(0).Value));
        //            double len = Convert.ToDouble((string)(pin.Descendants("length").ElementAt(0).Value));
        //            string os_2D_string = (string)(pin.Descendants("offsetVector2D").ElementAt(0).Value);
        //            string os_3D_string = (string)(pin.Descendants("offsetVector3D").ElementAt(0).Value);

        //            string[] os_2D = os_2D_string.Split(',');
        //            string[] os_3D = os_3D_string.Split(',');
        //            double os2D_X = Convert.ToDouble(os_2D[0]);
        //            double os2D_Y = Convert.ToDouble(os_2D[1]);
        //            double os3D_X = Convert.ToDouble(os_3D[0]);
        //            double os3D_Y = Convert.ToDouble(os_3D[1]);
        //            double os3D_Z = Convert.ToDouble(os_3D[2]);

        //            Vector2d os2D = new Vector2d(os2D_X, os2D_Y);
        //            Vector3d os3D = new Vector3d(os3D_X, os3D_Y, os3D_Z);

        //            Pin pinFound = new Pin(pinid, partElements.ElementAt(0).Attribute("partID").Value, ptype, pname, os2D, os3D);
        //            result.PinList.Add(pinFound);
        //        }

        //        result.RotationVectors = new List<Vector3d>();

        //    }

        //    return result;
        //}

        //public Schematic GetSchematicByUserIntent(string ui_id)
        //{
        //    UserIntent currUI = GetUserIntentByID(ui_id);
        //    string s_id = currUI.SchematicID;
        //    return GetSchematicByID(s_id);
        //}

        //public string GetDescription(string uiid)
        //{
        //    string result = "";

        //    var filename = "database/user_intents.xml";
        //    var currentDirectory = Directory.GetCurrentDirectory();
        //    var userintentFilepath = Path.Combine(currentDirectory, filename);

        //    XElement userintentList = XElement.Load($"{userintentFilepath}");

        //    IEnumerable<string> descriptions = from item in userintentList.Descendants("function")
        //                                       where ((string)item.Attribute("userintentID")).Equals(uiid)
        //                                       select (string)item.Element("description").Value;
        //    if (descriptions.Count() != 0)
        //    {
        //        result = descriptions.ElementAt(0).ToString();
        //    }

        //    return result;
        //}

        //public List<string> GetAllCategory()
        //{
        //    List<string> result = new List<string>();
        //    var filename = "database/parts.xml";
        //    var currentDirectory = Directory.GetCurrentDirectory();
        //    var partFilepath = Path.Combine(currentDirectory, filename);

        //    XElement partList = XElement.Load($"{partFilepath}");

        //    IEnumerable<XElement> cateElements = from item in partList.Descendants("category")
        //                                         select item;

        //    if (cateElements.Count() != 0)
        //    {
        //        foreach (var c in cateElements)
        //        {
        //            if (!result.Contains((string)c.Value))
        //            {
        //                result.Add((string)c.Value);
        //            }
        //        }
        //    }

        //    return result;
        //}

        //public Dictionary<string, string> GetPartsByCategory(string cate)
        //{
        //    Dictionary<string, string> result = new Dictionary<string, string>();

        //    var filename = "database/parts.xml";
        //    var currentDirectory = Directory.GetCurrentDirectory();
        //    var partFilepath = Path.Combine(currentDirectory, filename);

        //    XElement partList = XElement.Load($"{partFilepath}");

        //    IEnumerable<XElement> partElements = from item in partList.Descendants("part")
        //                                         where item.Element("category").Value.Equals(cate)
        //                                         select item;

        //    if (partElements.Count() != 0)
        //    {
        //        foreach (var p in partElements)
        //        {

        //            result.Add((string)p.Element("name").Value, (string)p.Attribute("partID"));

        //        }
        //    }

        //    return result;
        //}

        //public IEnumerable<string> GetAllUserIntents()
        //{
        //    var filename = "database\\user_intents.xml";
        //    var currentDirectory = Directory.GetCurrentDirectory();
        //    var userIntentFilepath = Path.Combine(currentDirectory, filename);

        //    XElement userIntentList = XElement.Load($"{userIntentFilepath}");

        //    IEnumerable<string> result = from item in userIntentList.Descendants("function")
        //                                 select (string)item.Element("description").Value;

        //    return result;
        //}

        #endregion
    }
}
