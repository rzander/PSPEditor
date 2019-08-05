using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;
using System.Diagnostics;
using PSPEdit;

namespace PSPEditWeb
{
    public partial class admxViewer : System.Web.UI.UserControl
    {
        public string json;
        private AdmxParse.policy Policy;

        protected void Page_Init()
        {
            try
            {
                Policy = this.Session["pol"] as AdmxParse.policy;
                if(Policy != null)
                    return;
            }
            catch
            {
               
            }

            Policy = new AdmxParse.policy();
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Policy.elements != null)
            {
                bool bElements = false;
                foreach (var oElem in Policy.elements.Where(t => t.ValueType != AdmxParse.valueType.PolicyEnable))
                {
                    bElements = true;
                    try
                    {
                        switch(oElem.GetType().ToString())
                        {
                            case "PSPEdit.AdmxParse+enumElement":
                                pOptions.Controls.Add(new Label() { Text = oElem.ValueName, CssClass = "col-md-6" });
                                DropDownList dl = new DropDownList()
                                {
                                    DataSource = ((AdmxParse.enumElement)oElem).valueList,
                                    DataTextField = "key",
                                    DataValueField = "value",
                                    CssClass = "col-md-6"
                                };
                                dl.SelectedIndexChanged += Dl_SelectedIndexChanged;
                                dl.ToolTip = oElem.ValueName;
                                dl.AutoPostBack = true;
                                dl.DataBind();
                                dl.Style["margin-top"] = "2px";
                                pOptions.Controls.Add(dl);

                                oElem.value = dl.SelectedValue;
                                break;
                            case "PSPEdit.AdmxParse+polEnableElement":
                                break;
                            case "PSPEdit.AdmxParse+listElement":
                                pOptions.Controls.Add(new Label() { Text = "Key1", CssClass = "col-md-6" });
                                pOptions.Controls.Add(new Label() { Text = "Val1", CssClass = "col-md-6" });
                                ((AdmxParse.listElement)oElem).valueList = new System.Collections.Generic.Dictionary<string, string>();
                                ((AdmxParse.listElement)oElem).valueList.Add("Key1", "Val1");
                                break;
                            case "PSPEdit.AdmxParse+decimalElement":
                                if(oElem.ValueType == AdmxParse.valueType.Boolean)
                                {
                                    pOptions.Controls.Add(new Label() { Text = oElem.ValueName, CssClass = "col-md-6" });
                                    var cb = new CheckBox() { Text = oElem.ValueName, CssClass = "col-md-6" };
                                    cb.CheckedChanged += Cb_CheckedChanged;
                                    if (oElem.value == "1")
                                        cb.Checked = true;
                                    else
                                        cb.Checked = false;
                                    
                                    cb.AutoPostBack = true;
                                    cb.ToolTip = oElem.ValueName;
                                    pOptions.Controls.Add(cb);
                                }
                                else
                                {
                                    pOptions.Controls.Add(new Label() { Text = oElem.ValueName, CssClass = "col-md-6" });
                                    var tb1 = new TextBox() { Text = oElem.value, CssClass = "col-md-6" };
                                    tb1.TextChanged += Tb_TextChanged;
                                    tb1.AutoPostBack = true;
                                    tb1.ToolTip = oElem.ValueName;
                                    tb1.Style["margin-top"] = "2px";
                                    oElem.value = tb1.Text;
                                    pOptions.Controls.Add(tb1);
                                }
                                break;
                            case "PSPEdit.AdmxParse+EnableListElement":
                                break;
                            default:
                                pOptions.Controls.Add(new Label() { Text = oElem.ValueName, CssClass = "col-md-6" });
                                var tb = new TextBox() { Text = oElem.value, CssClass = "col-md-6" };
                                tb.TextChanged += Tb_TextChanged;
                                tb.AutoPostBack = true;
                                tb.ToolTip = oElem.ValueName;
                                tb.ToolTip = oElem.ValueName;
                                tb.Style["margin-top"] = "2px";
                                oElem.value = tb.Text;
                                pOptions.Controls.Add(tb);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }

                if(!bElements)
                {
                    pOptions.Visible = false;
                }
                else
                {
                    pOptions.Visible = true;
                }

                
            }

            if (!IsPostBack)
            {
                tbPOSH.Text = Policy.PSCode;
            }

            this.Session["pol"] = Policy;
        }

        private void Cb_CheckedChanged(object sender, EventArgs e)
        {
            var oElem = Policy.elements.FirstOrDefault(t => t.ValueName == ((CheckBox)sender).ToolTip);
            oElem.value = ((CheckBox)sender).Checked ? "1" : "0";
            tbPOSH.Text = Policy.PSCode;
        }

        private void Dl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var oElem = Policy.elements.FirstOrDefault(t => t.ValueName == ((DropDownList)sender).ToolTip);
            oElem.value = ((DropDownList)sender).SelectedValue;
            tbPOSH.Text = Policy.PSCode;
        }

        public void LoadPolicy(string Filepath)
        {
            statusRadios.SelectedValue = "2";
            tbDescription.Text = "";

            Policy = AdmxParse.PolicyLoad(Filepath);

            lName.Text = Policy.displayName;
            tbDescription.Text = Policy.explainText;
            pOptions.Controls.Clear();
            tbPOSH.Text = "";

            this.Session["pol"] = Policy;
        }

        private void Tb_TextChanged(object sender, EventArgs e)
        {
            var oElem = Policy.elements.FirstOrDefault(t => t.ValueName == ((TextBox)sender).ToolTip);
            oElem.value = ((TextBox)sender).Text;
            tbPOSH.Text = Policy.PSCode;
        }

        protected void statusRadios_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (statusRadios.SelectedValue)
            {
                case "2":
                    Policy.state = AdmxParse.policyState.NotConfigured;
                    break;
                case "1":
                    Policy.state = AdmxParse.policyState.Enabled;
                    break;
                case "0":
                    Policy.state = AdmxParse.policyState.Disabled;
                    break;
            }
            tbPOSH.Text = Policy.PSCode;
        }
    }

}