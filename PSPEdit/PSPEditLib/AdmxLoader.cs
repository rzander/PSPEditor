using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace PSPEdit
{
    public class AdmxLoader
    {
        //JavaScriptSerializer jSerializer = new JavaScriptSerializer();
        public void load(string jsonpath)
        {
            string TemplateDir = Properties.Settings.Default.ScriptPath;
            if (string.IsNullOrEmpty(TemplateDir))
            {
                TemplateDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Templates");
            }

            //var xNew = xTemplate.CreateNode(XmlNodeType.Element, "name", "");

            foreach (var admxFile in Directory.GetFiles(TemplateDir, "*.admx", SearchOption.TopDirectoryOnly))
            {
                AdmxParse oAdmx = new AdmxParse(admxFile, "en-US");
                oAdmx.ToString();

                foreach (var oPol in oAdmx.policies)
                {
                    polSettings.Add(oPol);
                }
            }

            XmlDocument xTemplate = new XmlDocument();
            xTemplate.LoadXml(Properties.Settings.Default.DevicePathMapping);

            /*XmlNode xAdminTemplates = xTemplate.SelectSingleNode("/configuration/administrativeTemplates");

            if (xAdminTemplates == null)
                return;*/

            var ooUt = Sort(XDocument.Parse(xTemplate.OuterXml));
            ooUt.ToString();

            foreach (var oPS in polSettings)
            {
                try
                {
                    if (oPS.policyType == AdmxParse.classType.Machine)
                        TemplateDir = System.IO.Path.Combine(jsonpath, "Computer Configuration");
                    else
                        TemplateDir = System.IO.Path.Combine(jsonpath, "User Configuration");

                    string sPath = oPS.displaypath;
                    sPath = sPath.Replace("\"", "'");
                    sPath = sPath.Replace(">", "-");
                    sPath = sPath.Replace("|", "¦");
                    //sPath = sPath.Replace("\\", "&");

                    var oDir = Directory.CreateDirectory(Path.Combine(TemplateDir, sPath));
                    JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

                    var json = JsonConvert.SerializeObject(oPS, settings);
                    //var json = jSerializer.Serialize(oPS);
                    json.ToString();

                    string sFile = oPS.displayName;
                    sFile = sFile.Replace("\"", "'");
                    sFile = sFile.Replace("/", "¦");
                    sFile = sFile.Replace("|", "¦");
                    sFile = sFile.Replace(":", ";");
                    sFile = sFile.Replace("?", "");
                    sFile = sFile.Replace("*", "");
                    if (sFile.Length > 64)
                        sFile = sFile.Substring(0, 64);


                    File.WriteAllText(oDir.FullName + "\\" + sFile + ".json", json);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

            }
        }

        public List<AdmxParse.policy> polSettings = new List<AdmxParse.policy>();

        public class setting
        {
            public string path { get; set; }
            public string name { get; set; }
            public string displayname { get; set; }
        }

        public XmlNode SearchItem(XmlNode Node, string name)
        {
            XmlNode oResult = Node.SelectSingleNode("//*[@name='" + name + "']");

            return oResult;
        }

        private static XElement Sort(XElement element)
        {
            return new XElement(element.Name,
                    element.Attributes(),
                    from child in element.Nodes()
                    where child.NodeType != XmlNodeType.Element
                    select child,
                    from child in element.Elements()
                    orderby (string)child.Attribute("displayname").Value
                    select Sort(child));
        }

        private static XDocument Sort(XDocument file)
        {
            return new XDocument(
                    file.Declaration,
                    from child in file.Nodes()
                    where child.NodeType != XmlNodeType.Element
                    select child,
                    Sort(file.Root));
        }

        private static void CreateDir(XElement element, string Path)
        {
            try
            {
                string sDir = System.IO.Path.Combine(Path, element.Attribute("displayname").Value.Replace(":", "_"));
                if (!Directory.Exists(sDir))
                    Directory.CreateDirectory(sDir);
                foreach (XElement xe in element.Nodes())
                {
                    CreateDir(xe, sDir);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
