// *****************************************************************
// PSPEditLib (c) 2017 by Roger Zander
// *****************************************************************

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PSPEdit
{
    public class AdmxParse
    {
        internal XmlDocument xAdml = new XmlDocument();
        internal XmlDocument xAdmx = new XmlDocument();
        internal XmlDocument xPathMapping = new XmlDocument();

        public List<category> categories { get; set; }
        public List<policy> policies { get; set; }

        /// <summary>
        /// Parse ADMX Files
        /// </summary>
        /// <param name="FilePath">Admx File Path</param>
        /// <param name="Language">Subfolder of the Admx File with the corresponding Adml File</param>
        public AdmxParse(string FilePath, string Language)
        {
            categories = new List<category>();
            policies = new List<policy>();
            xPathMapping.LoadXml(Properties.Settings.Default.DevicePathMapping);

            FileInfo fiAdmx = new FileInfo(FilePath);
            if (fiAdmx.Exists)
            {
                FileInfo fiAdml = new FileInfo(Path.Combine(fiAdmx.DirectoryName, Language, fiAdmx.Name.Replace(".admx", ".adml")));
                if (fiAdml.Exists)
                {
                    xAdml.Load(fiAdml.FullName);
                }

                xAdmx.Load(FilePath);
                XmlNamespaceManager ns = new XmlNamespaceManager(xAdmx.NameTable);
                ns.AddNamespace("pd", xAdmx.DocumentElement.NamespaceURI);

                //Get categories
                foreach (XmlNode xCat in xAdmx.SelectNodes("/pd:policyDefinitions/pd:categories/pd:category", ns))
                {
                    try
                    {
                        var oRes = new category();
                        oRes.displayName = sResourceStringLookup(xCat.Attributes["displayName"].InnerText.Replace("$(string.", "").TrimEnd(')'));
                        oRes.name = xCat.Attributes["name"].InnerText;
                        if (xCat["parentCategory"] == null)
                        {

                            //oRes.parent = oRes.name; ???
                            oRes.parent = "";

                            categories.Add(oRes);

                            continue;
                        }

                        oRes.parent = xCat["parentCategory"].Attributes["ref"].InnerText;
                        if (categories.FirstOrDefault(t => t.name == oRes.parent) == null)
                        {
                            var xParent = xPathMapping.SelectSingleNode("//*[@name='" + oRes.parent + "']");
                            if (xParent != null)
                            {
                                XmlNode xPar = xParent;
                                while (xPar != null)
                                {
                                    if (xPar.Name != "#document")
                                    {
                                        if (xPar.Attributes["name"] != null)
                                        {
                                            string sPar = "";
                                            if (xPar.ParentNode.Attributes["name"] != null)
                                            {
                                                sPar = xPar.ParentNode.Attributes["name"].Value;
                                            }
                                            categories.Add(new category() { name = xPar.Attributes["name"].Value, displayName = xPar.Attributes["displayname"].Value, parent = sPar });
                                        }
                                    }
                                    xPar = xPar.ParentNode;
                                }
                            }
                            else
                            {
                                string sDispName = oRes.parent;
                                categories.Add(new category() { name = sDispName, displayName = sDispName, parent = "" });

                            }
                        }

                        categories.Add(oRes);
                    }
                    catch (Exception ex)
                    {
                        ex.Message.ToString();
                    }

                }

                //Get Policies
                foreach (XmlNode xPol in xAdmx.SelectNodes("/pd:policyDefinitions/pd:policies/pd:policy", ns))
                {
                    var oRes = new policy();
                    oRes.elements = new List<element>();
                    oRes.displayName = sResourceStringLookup(xPol.Attributes["displayName"].InnerText.Replace("$(string.", "").TrimEnd(')'));
                    oRes.name = xPol.Attributes["name"].InnerText;
                    oRes.state = policyState.NotConfigured;

                    if (string.IsNullOrEmpty(oRes.displayName))
                        oRes.displayName = oRes.name;

                    if(xPol.Attributes["explainText"] != null)
                        oRes.explainText = sResourceStringLookup(xPol.Attributes["explainText"].InnerText.Replace("$(string.", "").TrimEnd(')'));

                    oRes.parentCategory = categories.FirstOrDefault(t => t.name == xPol["parentCategory"].Attributes["ref"].InnerText);
                    catLookup(xPol["parentCategory"].Attributes["ref"].InnerText);
                    oRes.path = GetPath(xPol["parentCategory"].Attributes["ref"].InnerText);
                    var pCat = categories.FirstOrDefault(t => t.name == xPol["parentCategory"].Attributes["ref"].InnerText);
                    if (pCat != null)
                    {
                        oRes.displaypath = GetDisplayPath(pCat.displayName);
                    }
                    else
                    {
                        var xParent = xPathMapping.SelectSingleNode("//*[@name='" + xPol["parentCategory"].Attributes["ref"].InnerText + "']");
                        if (xParent != null)
                        {
                            string sDispName = xParent.Attributes["displayname"].Value;
                            string sName = xParent.Attributes["name"].Value;
                            string sParent = "";
                            if (xParent.ParentNode.Attributes["name"] != null)
                                sParent = xParent.ParentNode.Attributes["name"].Value;
                            categories.Add(new category() { name = sName, displayName = sDispName, parent = sParent });
                            oRes.displaypath = GetDisplayPath(sDispName);
                        }

                    }

                    oRes.key = xPol.Attributes["key"].InnerText;
                    if (xPol.Attributes["presentation"] != null)
                        oRes.presentation = sPresentationStringLookup(xPol.Attributes["presentation"].InnerText.Replace("$(presentation.", "").TrimEnd(')'));
                    switch (xPol.Attributes["class"].InnerText)
                    {
                        case "Machine":
                            oRes.policyType = classType.Machine;
                            break;
                        case "User":
                            oRes.policyType = classType.User;
                            break;
                    }

                    //oRes.innerXML = xPol.InnerXml;

                    if (xPol.Attributes["valueName"] != null)
                    {
                        if (xPol["enabledValue"] != null)
                        {
                            polEnableElement oElem = new polEnableElement();
                            oElem.ValueName = xPol.Attributes["valueName"].InnerText;
                            oElem.ValueType = valueType.PolicyEnable;

                            if (xPol["enabledValue"]["decimal"] != null)
                            {
                                try
                                {
                                    oElem.enabledValue = uint.Parse(xPol["enabledValue"]["decimal"].Attributes["value"].Value);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }
                            }

                            if (xPol["disabledValue"] != null)
                            {
                                if (xPol["disabledValue"]["decimal"] != null)
                                {
                                    try
                                    {
                                        oElem.disabledValue = uint.Parse(xPol["disabledValue"]["decimal"].Attributes["value"].Value);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex.Message);
                                    }
                                }
                            }

                            oRes.elements.Add(oElem);
                        }
                        else
                        {
                            polEnableElement oElem = new polEnableElement();
                            oElem.ValueName = xPol.Attributes["valueName"].InnerText;
                            oElem.ValueType = valueType.PolicyEnable;
                            oElem.enabledValue = 1;
                            oElem.disabledValue = 0;

                            oRes.elements.Add(oElem);
                        }
                    }

                    if (xPol["elements"] != null)
                    {
                        foreach (XmlNode xElem in xPol["elements"].ChildNodes)
                        {
                            if (xElem.Name == "#comment")
                                continue;
                            if (xElem.Name == "#text")
                                continue;

                            element oElem = new element();

                            switch (xElem.Name.ToLower())
                            {
                                case "decimal":
                                    oElem = new decimalElement();
                                    oElem.ValueType = valueType.Decimal;
                                    if (xElem.Attributes["minValue"] != null)
                                    {
                                        uint iRes = 0;
                                        if (uint.TryParse(xElem.Attributes["minValue"].InnerText, out iRes))
                                        {
                                            ((decimalElement)oElem).minValue = iRes;
                                        }
                                    }
                                    if (xElem.Attributes["maxValue"] != null)
                                    {
                                        uint iRes = 0;
                                        if (uint.TryParse(xElem.Attributes["maxValue"].InnerText, out iRes))
                                        {
                                            ((decimalElement)oElem).maxValue = iRes;
                                        }
                                    }
                                    break;
                                case "enum":
                                    oElem = new enumElement();
                                    oElem.ValueType = valueType.Enum;
                                    ((enumElement)oElem).valueList = new Dictionary<string, string>();
                                    foreach (XmlNode xElemItem in xElem.SelectNodes("pd:item", ns))
                                    {
                                        try
                                        {
                                            string sDisplayName = sResourceStringLookup(xElemItem.Attributes["displayName"].Value.Replace("$(string.", "").TrimEnd(')'));
                                            if (string.IsNullOrEmpty(sDisplayName))
                                                sDisplayName = xElemItem.Attributes["displayName"].Value.Replace("$(string.", "").TrimEnd(')');
                                            if (xElemItem["value"].FirstChild.Name == "decimal")
                                                ((enumElement)oElem).valueList.Add(sDisplayName, xElemItem["value"].FirstChild.Attributes["value"].Value);
                                            if (xElemItem["value"].FirstChild.Name == "string")
                                                ((enumElement)oElem).valueList.Add(sDisplayName, xElemItem["value"].FirstChild.InnerText);
                                            string sDefault = oRes.sPresentationdefaultValue(xElem.Attributes["id"].Value);
                                            if (!string.IsNullOrEmpty(sDefault))
                                            {
                                                int iDef = 0;
                                                int.TryParse(sDefault, out iDef);

                                                ((enumElement)oElem).defaultItem = iDef;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine(ex.Message);
                                        }
                                    }
                                    break;
                                case "boolean":
                                    oElem = new decimalElement();
                                    oElem.ValueType = valueType.Boolean;
                                    ((decimalElement)oElem).minValue = 0;
                                    ((decimalElement)oElem).maxValue = 1;
                                    oElem.value = oRes.sPresentationdefaultValue(xElem.Attributes["id"].Value);
                                    break;
                                case "list":
                                    oElem = new listElement();
                                    oElem.ValueType = valueType.List;
                                    ((listElement)oElem).valueList = new Dictionary<string, string>();
                                    if (xElem.Attributes["additive"] != null)
                                    {
                                        bool bRes = false;
                                        if (bool.TryParse(xElem.Attributes["additive"].Value, out bRes))
                                        {
                                            ((listElement)oElem).additive = bRes;
                                        }
                                        else
                                            ((listElement)oElem).additive = null;
                                    }
                                    if (xElem.Attributes["explicitValue"] != null)
                                    {
                                        bool bRes = false;
                                        if (bool.TryParse(xElem.Attributes["explicitValue"].Value, out bRes))
                                        {
                                            ((listElement)oElem).explicitValue = bRes;
                                        }
                                        else
                                            ((listElement)oElem).explicitValue = null;
                                    }
                                    break;
                                case "text":
                                    oElem = new textElement();
                                    oElem.ValueType = valueType.Text;
                                    break;
                                default:
                                    xElem.Name.ToString();
                                    break;
                            }

                            oElem.value = oRes.sPresentationdefaultValue(xElem.Attributes["id"].InnerText);
                            //oElem.innerXML = xElem.OuterXml;

                            //List do not have a Value, they have List of Valuenames and Values
                            if (oElem.ValueType != valueType.List)
                                oElem.ValueName = xElem.Attributes["valueName"].InnerText;

                            if (xElem.Attributes["required"] != null)
                            {
                                bool bReq = false;
                                if (bool.TryParse(xElem.Attributes["required"].InnerText, out bReq))
                                    oElem.required = bReq;
                            }

                            if (xElem.Attributes["key"] != null)
                            {
                                oElem.key = xElem.Attributes["key"].Value;
                            }


                            oRes.elements.Add(oElem);
                        }
                    }

                    if (xPol["enabledList"] != null)
                    {
                        EnableListElement oElem = new EnableListElement();

                        oElem.ValueType = valueType.Enum;
                        oElem.enabledValueList = new List<enabledList>();

                        foreach (XmlNode xElem in xPol["enabledList"].SelectNodes("pd:item", ns))
                        {
                            try
                            {
                                enabledList oResList = new enabledList();
                                
                                if (xElem["value"].FirstChild.Name == "decimal")
                                {
                                    oResList.type = valueType.Decimal;
                                    oResList.key = xElem.Attributes["key"].Value;
                                    oResList.valueName = xElem.Attributes["valueName"].Value;
                                    oResList.value = xElem["value"].FirstChild.Attributes["value"].Value;
                                }
                                if (xElem["value"].FirstChild.Name == "string")
                                {
                                    oResList.type = valueType.Text;
                                    oResList.key = xElem.Attributes["key"].Value;
                                    oResList.valueName = xElem.Attributes["valueName"].Value;
                                    oResList.value = xElem["value"].FirstChild.Attributes["value"].Value;
                                }

                                oElem.enabledValueList.Add(oResList);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }


                        }


                        oRes.elements.Add(oElem);
                    }

                    if (xPol["disabledList"] != null)
                    {
                        EnableListElement oElem = new EnableListElement();

                        oElem.ValueType = valueType.Enum;
                        oElem.disabledValueList = new List<enabledList>();

                        foreach (XmlNode xElem in xPol["disabledList"].SelectNodes("pd:item", ns))
                        {
                            try
                            {
                                enabledList oResList = new enabledList();

                                if (xElem["value"].FirstChild.Name == "decimal")
                                {
                                    oResList.type = valueType.Decimal;
                                    oResList.key = xElem.Attributes["key"].Value;
                                    oResList.valueName = xElem.Attributes["valueName"].Value;
                                    oResList.value = xElem["value"].FirstChild.Attributes["value"].Value;
                                }
                                if (xElem["value"].FirstChild.Name == "string")
                                {
                                    oResList.type = valueType.Text;
                                    oResList.key = xElem.Attributes["key"].Value;
                                    oResList.valueName = xElem.Attributes["valueName"].Value;
                                    oResList.value = xElem["value"].FirstChild.Attributes["value"].Value;
                                }

                                oElem.disabledValueList.Add(oResList);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }


                        }


                        oRes.elements.Add(oElem);
                    }

                    policies.Add(oRes);

                }
            }
        }

        /// <summary>
        /// Category Lookup
        /// </summary>
        /// <param name="CategoryName"></param>
        public void catLookup(string CategoryName)
        {
            var xParent = xPathMapping.SelectSingleNode("//*[@name='" + CategoryName + "']");
            if (xParent != null)
            {
                XmlNode xPar = xParent;
                while (xPar != null)
                {
                    if (xPar.Name != "#document")
                    {
                        if (xPar.Attributes["name"] != null)
                        {
                            string sPar = "";
                            if (xPar.ParentNode.Attributes["name"] != null)
                            {
                                sPar = xPar.ParentNode.Attributes["name"].Value;
                            }
                            if (categories.FirstOrDefault(t => t.name == xPar.Attributes["name"].Value) == null)
                            {
                                categories.Add(new category() { name = xPar.Attributes["name"].Value, displayName = xPar.Attributes["displayname"].Value, parent = sPar });
                            }
                        }
                    }
                    xPar = xPar.ParentNode;
                }
            }
        }

        /// <summary>
        /// Build Path
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        public string GetPath(string Path)
        {
            string sResult = Path;

            var oParent = categories.FirstOrDefault(t => t.name == Path.Split('\\')[0]);
            if (oParent != null)
            {
                sResult = GetPath(oParent.parent + "\\" + sResult);
            }

            return sResult.TrimStart('\\');
        }

        /// <summary>
        /// Build DisplayPath
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        public string GetDisplayPath(string Path)
        {
            string sResult = Path;

            var oThis = categories.FirstOrDefault(t => t.displayName == Path.Split('\\')[0]);
            if (oThis != null)
            {
                var oParent = categories.FirstOrDefault(t => t.name == oThis.parent);
                if (oParent != null)
                    sResult = GetDisplayPath(oParent.displayName + "\\" + sResult);
            }

            return sResult.TrimStart('\\');
        }

        public class category
        {
            public category()
            {
            }
            public string parent { get; set; }
            public string name { get; set; }
            public string displayName { get; set; }
            public string innerXML { get; set; }
        }

        public static policy PolicyLoad(string JSONFile)
        {
            //serializer 
            string json = File.ReadAllText(JSONFile);
            //PSPEdit.AdmxParse.policy oPol = new PSPEdit.AdmxParse.policy();


            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            return JsonConvert.DeserializeObject<AdmxParse.policy>(json, settings);
        }
        public class policy
        {
            public policy()
            {
            }
            public policyState state { get; set; }
            public string name { get; set; }
            public classType policyType { get; set; }
            public string displayName { get; set; }
            public string explainText { get; set; }
            public string presentation { get; set; }
            public string key { get; set; }

            /// <summary>
            /// PowerShell RegKey
            /// </summary>
            public string pskey
            {
                get
                {
                    if (policyType == classType.Machine)
                        return "HKLM:\\" + key;
                    else
                        return "HKCU:\\" + key;
                }
            }
            public category parentCategory { get; set; }

            public string path { get; set; }

            /// <summary>
            /// Path based on display names
            /// </summary>
            public string displaypath { get; set; }

            public string innerXML { get; set; }

            public List<element> elements { get; set; }

            internal string sPresentationdefaultValue(string refId)
            {
                string sResult = "";
                try
                {
                    if (!string.IsNullOrEmpty(presentation))
                    {
                        XmlDocument xPres = new XmlDocument();
                        xPres.LoadXml("<root>" + presentation + "</root>");
                        XmlNamespaceManager ns = new XmlNamespaceManager(xPres.NameTable);
                        ns.AddNamespace("pd", "http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions");
                        var vDefVal = xPres.SelectSingleNode("/root/*[contains(@refId,'" + refId + "')]", ns);
                        if (vDefVal != null)
                        {
                            if (vDefVal.Attributes["defaultValue"] != null)
                                sResult = vDefVal.Attributes["defaultValue"].InnerText;
                            if (vDefVal.Attributes["defaultChecked"] != null)
                            {
                                if (vDefVal.Attributes["defaultChecked"].InnerText == "true")
                                    sResult = "1";
                                else
                                    sResult = "0";
                            }
                            if (vDefVal.Attributes["defaultItem"] != null)
                                sResult = vDefVal.Attributes["defaultItem"].InnerText;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Message.ToString();
                }

                return sResult;
            }

            public string PSCode
            {
                get
                {
                    string sResult = "";

                    if (elements == null)
                        return sResult;

                    sResult += "#region " + this.displayName +"\n\n";

                    if (this.state == policyState.NotConfigured)
                    {
                        foreach (var oElem in elements)
                        {
                            string sKey = this.pskey;
                            if (oElem.key != null)
                            {
                                if (policyType == classType.Machine)
                                    sKey = "HKLM:\\" + oElem.key;
                                else
                                    sKey = "HKCU:\\" + oElem.key;
                            }
                            if (oElem.ValueType != valueType.List)
                            {
                                if (!string.IsNullOrEmpty(oElem.ValueName))
                                {
                                    sResult += "Remove-ItemProperty -Path '" + sKey + "' -Name '" + oElem.ValueName + "' -ea SilentlyContinue \n";
                                }
                            }
                            else
                            {
                                sResult += "New-Item -Path '" + pskey + "' -force -ea SilentlyContinue \n";
                            }
                        }

                        foreach (EnableListElement oElem in elements.Where(t => t.GetType() == typeof(EnableListElement)))
                        {
                            if (oElem.disabledValueList != null)
                            {
                                foreach (var odislis in oElem.disabledValueList)
                                {
                                    string sKey = "HKLM:\\";
                                    if (this.policyType == classType.User)
                                        sKey = "HKCU:\\";

                                    sResult += "Remove-ItemProperty -Path '" + sKey + odislis.key + "' -Name '" + odislis.valueName + "' -ea SilentlyContinue \n";

                                }
                            }
                        }

                        sResult += "\n";
                        sResult += "#Cleanup \n";
                        sResult += "if(((Get-Item -Path '" + pskey + "').SubKeyCount -eq 0) -and ((Get-Item -Path '" + pskey + "').ValueCount -eq 0)) { Remove-Item -Path '" + pskey + "' -ea SilentlyContinue } \n";

                    }

                    if (this.state == policyState.Disabled)
                    {
                        foreach (var oElem in elements.Where(t => t.ValueType == valueType.PolicyEnable))
                        {
                            string sKey = this.pskey;
                            if (oElem.key != null)
                            {
                                if (policyType == classType.Machine)
                                    sKey = "HKLM:\\" + oElem.key;
                                else
                                    sKey = "HKCU:\\" + oElem.key;
                            }
                            sResult += "#Create the key if missing \n";
                            sResult += "If((Test-Path '" + pskey + "') -eq $false ) { New-Item -Path '" + pskey + "' -force -ea SilentlyContinue } \n\n";
                            sResult += "#Disable the Policy \n";

                            if (((polEnableElement)oElem).disabledValue != null)
                                sResult += "Set-ItemProperty -Path '" + sKey + "' -Name '" + oElem.ValueName + "' -Value " + ((polEnableElement)oElem).disabledValue as string + " -ea SilentlyContinue \n";
                            else
                            {
                                if (!string.IsNullOrEmpty(oElem.ValueName))
                                {
                                    sResult += "Remove-ItemProperty -Path '" + sKey + "' -Name '" + oElem.ValueName + "' -ea SilentlyContinue \n";
                                }
                            }
                        }
                        

                        bool Settings = true;
                        foreach (var oElem in elements.Where(t => t.ValueType != valueType.PolicyEnable))
                        {
                            if (Settings)
                            {
                                sResult += "#Disable Settings \n";
                                Settings = false;
                            }
                            string sKey = this.pskey;
                            if (oElem.key != null)
                            {
                                if (policyType == classType.Machine)
                                    sKey = "HKLM:\\" + oElem.key;
                                else
                                    sKey = "HKCU:\\" + oElem.key;
                            }
                            if (oElem.ValueType != valueType.List)
                            {
                                if (!string.IsNullOrEmpty(oElem.ValueName))
                                {
                                    sResult += "Remove-ItemProperty -Path '" + sKey + "' -Name '" + oElem.ValueName + "' -ea SilentlyContinue \n";
                                }
                            }
                            else
                            {
                                sResult += "New-Item -Path '" + pskey + "' -force -ea SilentlyContinue \n";
                            }

                        }

                        foreach (EnableListElement oElem in elements.Where(t => t.GetType() == typeof(EnableListElement)))
                        {
                            if (oElem.disabledValueList != null)
                            {
                                foreach (var odislis in oElem.disabledValueList)
                                {
                                    string sKey = "HKLM:\\";
                                    if (this.policyType == classType.User)
                                        sKey = "HKCU:\\";
                                    string sValue = odislis.value;

                                    if (odislis.type == valueType.Text)
                                        sValue = "'" + odislis.value + "'";

                                    sResult += "Set-ItemProperty -Path '" + sKey + odislis.key + "' -Name '" + odislis.valueName + "' -Value " + sValue + " -ea SilentlyContinue \n";
                                }
                            }
                        }
                    }

                    if (this.state == policyState.Enabled)
                    {
                        sResult += "#Create the key if missing \n";
                        sResult += "If((Test-Path '" + pskey + "') -eq $false ) { New-Item -Path '" + pskey + "' -force -ea SilentlyContinue } \n";

                        foreach (var oElem in elements.Where(t => t.ValueType == valueType.PolicyEnable))
                        {
                            string sKey = this.pskey;
                            if (oElem.key != null)
                            {
                                if (policyType == classType.Machine)
                                    sKey = "HKLM:\\" + oElem.key;
                                else
                                    sKey = "HKCU:\\" + oElem.key;
                            }
                            sResult += "\n#Enable the Policy\n";
                            sResult += "Set-ItemProperty -Path '" + sKey + "' -Name '" + oElem.ValueName + "' -Value " + ((polEnableElement)oElem).enabledValue as string + " -ea SilentlyContinue \n";
                        }

                        bool Settings = true;
                        foreach (var oElem in elements.Where(t => t.ValueType != valueType.PolicyEnable))
                        {
                            if (Settings)
                            {
                                sResult += "\n#Enable Settings \n";
                                Settings = false;
                            }
                            string sKey = this.pskey;


                            if (oElem.value == null)
                                continue;

                            if (oElem.ValueType == valueType.Text)
                                oElem.value = "'" + oElem.value + "'";
                            else
                            {
                                if (oElem.value.Contains(' '))
                                    oElem.value = "'" + oElem.value.TrimStart(new char[] { '\'', '"' }).TrimEnd(new char[] { '\'', '"' }) + "'";
                            }

                            if (oElem.key != null)
                            {
                                if (policyType == classType.Machine)
                                    sKey = "HKLM:\\" + oElem.key;
                                else
                                    sKey = "HKCU:\\" + oElem.key;
                            }

                            if(oElem.ValueType == valueType.List)
                            {
                                foreach(var oIt in ((AdmxParse.listElement)oElem).valueList)
                                {
                                    sResult += "Set-ItemProperty -Path '" + sKey + "' -Name '" + oIt.Key + "' -Value " + oIt.Value + " -ea SilentlyContinue \n";
                                }
                            }

                            if (oElem.ValueType != valueType.List)
                            {
                                sResult += "Set-ItemProperty -Path '" + sKey + "' -Name '" + oElem.ValueName + "' -Value " + oElem.value + " -ea SilentlyContinue \n";
                            }
                        }

                        foreach (EnableListElement oElem in elements.Where(t => t.GetType() == typeof(EnableListElement)))
                        {
                            if (oElem.enabledValueList != null)
                            {
                                foreach (var oenlis in oElem.enabledValueList)
                                {
                                    string sKey = "HKLM:\\";
                                    if (this.policyType == classType.User)
                                        sKey = "HKCU:\\";
                                    string sValue = oenlis.value;

                                    if (oenlis.type == valueType.Text)
                                        sValue = "'" + oenlis.value + "'";

                                    sResult += "Set-ItemProperty -Path '" + sKey + oenlis.key + "' -Name '" + oenlis.valueName + "' -Value " + sValue + " -ea SilentlyContinue \n";
                                }
                            }
                        }
                    }
                    sResult += "\n#endregion";
                    return sResult;
                }
            }

        }

        public class element
        {
            public string ValueName { get; set; }
            public valueType ValueType { get; set; }
            public string value { get; set; }
            public bool required { get; set; } = false;
            public string innerXML { get; set; }

            public string key { get; set; }

        }

        public class decimalElement : element
        {
            public uint? minValue { get; set; }
            public uint? maxValue { get; set; }
        }

        public class enumElement : element
        {
            public Dictionary<string, string> valueList { get; set; }

            public int defaultItem { get; set; }
        }

        public class listElement : element
        {
            public Dictionary<string, string> valueList { get; set; }

            public Boolean? additive { get; set; }
            public Boolean? explicitValue { get; set; }
        }

        public class textElement : element
        {
        }

        public class polEnableElement : element
        {
            public object enabledValue { get; set; }
            public object disabledValue { get; set; }
        }

        public class EnableListElement : element
        {
            public List<enabledList> enabledValueList { get; set; }
            public List<enabledList> disabledValueList { get; set; }
        }

        public enum classType { Machine, User }

        public enum policyState { Enabled, Disabled, NotConfigured }

        public enum valueType { PolicyEnable, Decimal, Boolean, Text, Enum, List }

        /// <summary>
        /// Lookup for Resource-Strings in ADML
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        internal string sResourceStringLookup(string ID)
        {
            string sResult = "";
            try
            {
                if (!string.IsNullOrEmpty(xAdml.OuterXml))
                {
                    if (string.IsNullOrEmpty(xAdml.DocumentElement.NamespaceURI))
                    {
                        var oRes = xAdml.SelectSingleNode("/policyDefinitionResources/resources/stringTable/string[@id=\"" + ID + "\"]");
                        if (oRes != null)
                            return oRes.InnerText;
                        else
                            return ID;
                    }
                    else
                    {
                        XmlNamespaceManager ns = new XmlNamespaceManager(xAdml.NameTable);
                        //ns.AddNamespace("pd", "http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions");
                        ns.AddNamespace("pd", xAdml.DocumentElement.NamespaceURI);


                        var oRes = xAdml.SelectSingleNode("/pd:policyDefinitionResources/pd:resources/pd:stringTable/pd:string[@id=\"" + ID + "\"]", ns);

                        if (oRes != null)
                        {
                            if (string.IsNullOrEmpty(oRes.InnerText))
                            {
                                oRes.InnerText.ToString();
                                return ID;
                            }
                            return oRes.InnerText;
                        }
                        else
                            return ID;
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return sResult;
        }

        /// <summary>
        /// Lookup for Presentation-String in ADML
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        internal string sPresentationStringLookup(string ID)
        {
            string sResult = "";
            try
            {
                if (!string.IsNullOrEmpty(xAdml.OuterXml))
                {
                    XmlNamespaceManager ns = new XmlNamespaceManager(xAdml.NameTable);
                    ns.AddNamespace("pd", "http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions");
                    var oRes = xAdml.SelectSingleNode("/pd:policyDefinitionResources/pd:resources/pd:presentationTable/pd:presentation[@id=\"" + ID + "\"]", ns);

                    if (oRes != null)
                        return oRes.InnerXml;
                    else return "";
                }
            }
            catch { }

            return sResult;
        }

        public class enabledList
        {
            public string key  { get; set; }
            public string valueName { get; set; }
            public valueType type { get; set; }
            public string value { get; set; }
        }

    }
}
