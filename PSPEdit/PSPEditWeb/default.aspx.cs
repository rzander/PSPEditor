using PSPEdit;
using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using System.IO;
using System.Diagnostics;

namespace PSPEditWeb
{
    public partial class _default1 : System.Web.UI.Page
    {

        List<AdmxParse.policy> lPolicies = new List<AdmxParse.policy>();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                //LoadDir(tvADMXFiles.Nodes, new DirectoryInfo(@"C:\Temp\pol"));
                LoadDir(tvADMXFiles.Nodes, new DirectoryInfo(Server.MapPath("~/Data/en-us")));
            }
        }

        protected void LoadDir(TreeNodeCollection tNode, DirectoryInfo oDirs)
        {
            foreach (var oDir in oDirs.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                tNode.Add(new TreeNode()
                {
                    Text = oDir.Name,
                    ImageUrl = "~/Images/folder_closed.png",
                    Value = oDir.FullName
                });
            }
            foreach (var oFile in oDirs.GetFiles("*.json", SearchOption.TopDirectoryOnly))
            {
                tNode.Add(new TreeNode()
                {
                    Text = oFile.Name.Replace(".json", ""),
                    ImageUrl = "~/Images/Process.ico",
                    Value = oFile.FullName,
                });
            }
        }

        protected void tvADMXFiles_TreeNodeExpanded(object sender, TreeNodeEventArgs e)
        {
            try
            {
                if (e.Node.ChildNodes.Count == 0)
                {
                    if (!File.Exists(e.Node.Value))
                    {
                        try
                        {
                            LoadDir(e.Node.ChildNodes, new DirectoryInfo(e.Node.Value));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }

                e.Node.ImageUrl = "~/Images/folder.png";
            }
            catch { }
        }

        protected void tvADMXFiles_SelectedNodeChanged(object sender, EventArgs e)
        {
            try
            {
                if (File.Exists(tvADMXFiles.SelectedNode.Value))
                {
                    admxViewer.LoadPolicy(tvADMXFiles.SelectedNode.Value);

                }
                else
                {
                    tvADMXFiles.SelectedNode.Expand();
                    //tvADMXFiles.SelectedNode.ImageUrl = "~/Images/folder.png";
                }
            }
            catch { }
        }

        protected void tvADMXFiles_TreeNodeCollapsed(object sender, TreeNodeEventArgs e)
        {
            tvADMXFiles.SelectedNode.ImageUrl = "~/Images/folder_closed.png";
        }
    }
}