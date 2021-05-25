using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ModelecComponents.Helpers
{
    public class JSONHelpers
    {
        public JSONHelpers() { }

        /// <summary>
        /// Load and parse the printer profile file in JSON format
        /// </summary>
        /// <param name="filepath">The directory of the JSON file</param>
        /// <returns>System recognizable printer profile data structure</returns>
        public void loadPrinterProfile(string filepath)
        {
            PrinterProfile result = PrinterProfile.Instance;

            using (StreamReader file = File.OpenText(filepath))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject o = (JObject)JToken.ReadFrom(reader);
                foreach(JProperty prop in o.Properties())
                {
                    switch (prop.Name)
                    {
                        case "bed X": result.BedX = (double)prop.Value; break;
                        case "bed Y": result.BedY = (double)prop.Value; break;
                        case "bed Z": result.BedZ = (double)prop.Value; break;
                        case "resolution": result.Resolution = (double)prop.Value; break;
                        case "trace cross section X":
                            {

                                foreach(JProperty pair in prop.Value)
                                {
                                    printingConditionPair temp = new printingConditionPair();
                                    temp.Cond = pair.Name;
                                    temp.Value = (string)pair.Value;

                                    result.Trace_min_x.Add(temp);
                                }
                            }break;
                        case "trace cross section Y":
                            {

                                foreach (JProperty pair in prop.Value)
                                {
                                    printingConditionPair temp = new printingConditionPair();
                                    temp.Cond = pair.Name;
                                    temp.Value = (string)pair.Value;

                                    result.Trace_min_y.Add(temp);
                                }
                            }
                            break;
                        case "trace spacing":
                            {
                                foreach (JProperty pair in prop.Value)
                                {
                                    printingConditionPair temp = new printingConditionPair();
                                    temp.Cond = pair.Name;
                                    temp.Value = (string)pair.Value;

                                    result.Trace_spacing.Add(temp);
                                }
                            }
                            break;
                        case "trace resistivity": result.Trace_resistivity = (double)prop.Value; break;
                        case "minimum pad spacing": result.Trace_resistivity = (double)prop.Value; break;
                        case "conductor area":
                            {
                                foreach (JProperty pair in prop.Value)
                                {
                                    printingConditionPair temp = new printingConditionPair();
                                    temp.Cond = pair.Name;
                                    temp.Value = (string)pair.Value;

                                    result.Conductor_conditions.Add(temp);
                                }
                            }
                            break;
                        case "thickness around conductor":
                            {
                                foreach (JProperty pair in prop.Value)
                                {
                                    if (pair.Name.Equals("internal above"))
                                    {
                                        result.Thickness_inter_above = (double)pair.Value;
                                    }
                                    else if (pair.Name.Equals("internal below"))
                                    {
                                        result.Thickness_inter_below = (double)pair.Value;
                                    }
                                    else if (pair.Name.Equals("external above"))
                                    {
                                        result.Thickness_exter_pad = (double)pair.Value;
                                    }
                                    else if (pair.Name.Equals("external below"))
                                    {
                                        result.Thickness_inter_pad = (double)pair.Value;
                                    }
                                }
                            }
                            break;
                        case "printed electronics":
                            {
                                foreach (JProperty pair in prop.Value)
                                {
                                    if (pair.Name.Equals("resistor"))
                                    {
                                        result.Resistor_printed = (bool)pair.Value;
                                    }
                                    else if (pair.Name.Equals("capacitor"))
                                    {
                                        result.Capacitor_printed = (bool)pair.Value;
                                    }
                                    else if (pair.Name.Equals("inductor")){
                                        result.Inductor_printed = (bool)pair.Value;
                                    }
                                }
                            }
                            break;
                        case "shrinkage": result.Shrinkage = (double)prop.Value; break;
                        case "cross-section": result.TrShape = (int)prop.Value; break;
                        default: break;
                    }

                }
            }
        }
    }
}
