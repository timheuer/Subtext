#region Disclaimer/Info
///////////////////////////////////////////////////////////////////////////////////////////////////
// Subtext WebLog
// 
// Subtext is an open source weblog system that is a fork of the .TEXT
// weblog system.
//
// For updated news and information please visit http://subtextproject.com/
// Subtext is hosted at SourceForge at http://sourceforge.net/projects/subtext
// The development mailing list is at subtext-devs@lists.sourceforge.net 
//
// This project is licensed under the BSD license.  See the License.txt file for more information.
///////////////////////////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using Subtext.Framework;
using Subtext.Framework.Components;
using Subtext.Framework.Configuration;

namespace Subtext.Web.Admin.Pages
{
	// TODO: import - reconcile duplicates
	// TODO: CheckAll client-side, confirm bulk delete (add cmd)

	public class EditLinks : AdminPage
	{
		private const string VSKEY_CATEGORYID = "CategoryID";
		private const string VSKEY_LINKID = "LinkID";

		private int _filterCategoryID
		{
			get
			{
				if(ViewState["_filterCategoryID"] == null)
				{
					return NullValue.NullInt32;
				}
				else
				{
					return (int)ViewState["_filterCategoryID"];
				}
			}
			set
			{
				ViewState["_filterCategoryID"] = value;
			}
		}
		private int _resultsPageNumber = 1;
		private bool _isListHidden = false;

		protected System.Web.UI.WebControls.Repeater rprSelectionList;
		protected Subtext.Web.Admin.WebUI.Pager ResultsPager;
		protected Subtext.Web.Admin.WebUI.AdvancedPanel Results;
		protected Subtext.Web.Admin.WebUI.AdvancedPanel ImportExport;
		protected System.Web.UI.HtmlControls.HtmlInputFile OpmlImportFile;
		protected System.Web.UI.WebControls.Button lkbImportOpml;
		protected System.Web.UI.WebControls.Label lblEntryID;
		protected System.Web.UI.WebControls.RequiredFieldValidator RequiredFieldValidator1;
		protected System.Web.UI.WebControls.TextBox txbTitle;
		protected System.Web.UI.WebControls.RequiredFieldValidator Requiredfieldvalidator2;
		protected System.Web.UI.WebControls.TextBox txbUrl;
		protected System.Web.UI.WebControls.CheckBoxList cklCategories;
		protected System.Web.UI.WebControls.Button lkbPost;
		protected System.Web.UI.WebControls.Button lkbCancel;
		protected Subtext.Web.Admin.WebUI.AdvancedPanel Edit;
		protected System.Web.UI.WebControls.CheckBox ckbIsActive;
		protected System.Web.UI.WebControls.TextBox txbRss;
		protected System.Web.UI.WebControls.DropDownList ddlCategories;
		protected Subtext.Web.Admin.WebUI.MessagePanel Messages;
		protected Subtext.Web.Admin.WebUI.Page PageContainer;
		protected System.Web.UI.WebControls.CheckBox chkNewWindow;
		protected System.Web.UI.WebControls.DropDownList ddlImportExportCategories;
	
		#region Accessors
		private int CategoryID
		{
			get
			{
				if (null != ViewState[VSKEY_CATEGORYID])
					return (int)ViewState[VSKEY_CATEGORYID];
				else
					return NullValue.NullInt32;
			}
			set { ViewState[VSKEY_CATEGORYID] = value; }
		}

		public int LinkID
		{
			get
			{
				if(ViewState[VSKEY_LINKID] != null)
					return (int)ViewState[VSKEY_LINKID];
				else
					return NullValue.NullInt32;
			}
			set { ViewState[VSKEY_LINKID] = value; }
		}
	
		#endregion

		private void Page_Load(object sender, System.EventArgs e)
		{
			BindLocalUI();

			if (!IsPostBack)
			{
				if (null != Request.QueryString[Keys.QRYSTR_PAGEINDEX])
					_resultsPageNumber = Convert.ToInt32(Request.QueryString[Keys.QRYSTR_PAGEINDEX]);

				if (null != Request.QueryString[Keys.QRYSTR_CATEGORYID])
					_filterCategoryID = Convert.ToInt32(Request.QueryString[Keys.QRYSTR_CATEGORYID]);

				ResultsPager.PageSize = Preferences.ListingItemCount;
				ResultsPager.PageIndex = _resultsPageNumber;
				Results.Collapsible = false;

				if (NullValue.NullInt32 != _filterCategoryID)
					ResultsPager.UrlFormat += string.Format(System.Globalization.CultureInfo.InvariantCulture, "&{0}={1}", Keys.QRYSTR_CATEGORYID, 
						_filterCategoryID);
				
				BindList();
				//BindImportExportCategories();
			}	
		}

		private void BindImportExportCategories()
		{
			this.ddlImportExportCategories.DataSource = Links.GetCategories(CategoryType.LinkCollection,false);
			this.ddlImportExportCategories.DataTextField = "Title";
			this.ddlImportExportCategories.DataValueField = "CategoryID";
			this.ddlImportExportCategories.DataBind();
		}

		private void BindLocalUI()
		{
			LinkButton lkbNewLink = Utilities.CreateLinkButton("New Link");
			lkbNewLink.Click += new System.EventHandler(lkbNewLink_Click);
			lkbNewLink.CausesValidation =false;
			PageContainer.AddToActions(lkbNewLink);
			HyperLink hlEditCategories = Utilities.CreateHyperLink("Edit Categories","EditCategories.aspx");
			PageContainer.AddToActions(hlEditCategories);
		}

		private void BindList()
		{
			Edit.Visible = false;

			PagedLinkCollection selectionList = Links.GetPagedLinks(_filterCategoryID, _resultsPageNumber,
				ResultsPager.PageSize,true);
			
			if (selectionList.Count > 0)
			{
				ResultsPager.ItemCount = selectionList.MaxItems;
				rprSelectionList.DataSource = selectionList;
				rprSelectionList.DataBind();
			}
			else
			{
				// TODO: no existing items handling. add label and indicate no existing items. pop open edit.
			}
		}

		private void BindLinkEdit()
		{
			Link currentLink = Links.GetSingleLink(LinkID);
		
			Results.Collapsed = true;
			Results.Collapsible = true;
//			ImportExport.Visible = false;
			Edit.Visible = true;

			lblEntryID.Text = currentLink.LinkID.ToString(CultureInfo.InvariantCulture);
			txbTitle.Text = currentLink.Title;
			txbUrl.Text = currentLink.Url;
			txbRss.Text = currentLink.Rss;
		
			chkNewWindow.Checked = currentLink.NewWindow;
			ckbIsActive.Checked = currentLink.IsActive;

			BindLinkCategories();
			ddlCategories.Items.FindByValue(currentLink.CategoryID.ToString(CultureInfo.InvariantCulture)).Selected = true;

			Control container = Page.FindControl("PageContainer");
			if (null != container && container is Subtext.Web.Admin.WebUI.Page)
			{	
				Subtext.Web.Admin.WebUI.Page page = (Subtext.Web.Admin.WebUI.Page)container;
				string title = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Editing Link \"{0}\"", currentLink.Title);

				page.BreadCrumbs.AddLastItem(title);
				page.Title = title;
			}
		}

		public void BindLinkCategories()
		{
			LinkCategoryCollection selectionList = Links.GetCategories(CategoryType.LinkCollection, false);
			if(selectionList != null && selectionList.Count != 0)
			{
				ddlCategories.DataSource = selectionList;
				ddlCategories.DataValueField = "CategoryID";
				ddlCategories.DataTextField = "Title";
				ddlCategories.DataBind();
			}
			else
			{
				this.Messages.ShowError("You need to add a category before you can add links! Click \"Edit Categories\"");
				Edit.Visible = false;
			}

		}

		private void UpdateLink()
		{					
			string successMessage = Constants.RES_SUCCESSNEW;

			try
			{
				Link link = new Link();

				link.Title = txbTitle.Text;				
				link.Url = txbUrl.Text;
				link.Rss = txbRss.Text;
				link.IsActive = ckbIsActive.Checked;
				link.CategoryID = Convert.ToInt32(ddlCategories.SelectedItem.Value);
				link.NewWindow = chkNewWindow.Checked;
				link.LinkID = Config.CurrentBlog.BlogId;
				
				if (LinkID > 0)
				{
					successMessage = Constants.RES_SUCCESSEDIT;
					link.LinkID = LinkID;
					Links.UpdateLink(link);
				}
				else
				{
					LinkID = Links.CreateLink(link);
				}

				if (LinkID > 0)
				{			
					BindList();
					this.Messages.ShowMessage(successMessage);
				}
				else
					this.Messages.ShowError(Constants.RES_FAILUREEDIT 
						+ " There was a baseline problem posting your link.");
			}
			catch(Exception ex)
			{
				this.Messages.ShowError(String.Format(Constants.RES_EXCEPTION, 
					Constants.RES_FAILUREEDIT, ex.Message));
			}
			finally
			{
				Results.Collapsible = false;
			}
		}

		private void ResetPostEdit(bool showEdit)
		{
			LinkID = NullValue.NullInt32;

			Results.Collapsible = showEdit;
			Results.Collapsed = showEdit;
			//ImportExport.Visible = !showEdit;
			Edit.Visible = showEdit;

			lblEntryID.Text = String.Empty;
			txbTitle.Text = String.Empty;
			txbUrl.Text = String.Empty;
			txbRss.Text = String.Empty;
			chkNewWindow.Checked = false;

			ckbIsActive.Checked = Preferences.AlwaysCreateIsActive;

			if (showEdit)
				BindLinkCategories();
	
			ddlCategories.SelectedIndex = -1;
		}

		private void ConfirmDelete(int linkID, string linkTitle)
		{
			this.Command = new DeleteLinkCommand(linkID, linkTitle);
			this.Command.RedirectUrl = Request.Url.ToString();
			Server.Transfer(Constants.URL_CONFIRM);
		}

		private void ImportOpml()
		{
			if(OpmlImportFile.PostedFile.FileName.Trim().Length > 0)
			{
				OpmlItemCollection importedLinks = OpmlProvider.Import(OpmlImportFile.PostedFile.InputStream);
				
				if (importedLinks.Count > 0)
				{
					this.Command = new ImportLinksCommand(importedLinks,Int32.Parse(this.ddlImportExportCategories.SelectedItem.Value));
					this.Command.RedirectUrl = Request.Url.ToString();
					Server.Transfer(Constants.URL_CONFIRM);
				}

				BindList();
			}
		}

		// REFACTOR
		public string CheckHiddenStyle()
		{
			if (_isListHidden)
				return Constants.CSSSTYLE_HIDDEN;
			else
				return String.Empty;
		}

		#region Web Form Designer generated code
		override protected void OnInit(EventArgs e)
		{
			//
			// CODEGEN: This call is required by the ASP.NET Web Form Designer.
			//
			InitializeComponent();
			base.OnInit(e);
		}
		
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{   
			this.rprSelectionList.ItemCommand += new System.Web.UI.WebControls.RepeaterCommandEventHandler(this.rprSelectionList_ItemCommand);
			this.lkbImportOpml.Click += new System.EventHandler(this.lkbImportOpml_Click);
			this.lkbPost.Click += new System.EventHandler(this.lkbPost_Click);
			this.lkbCancel.Click += new System.EventHandler(this.lkbCancel_Click);
			this.Load += new System.EventHandler(this.Page_Load);

		}
		#endregion 

		private void lkbImportOpml_Click(object sender, System.EventArgs e)
		{
			if (Page.IsValid) ImportOpml();
		}

		private void rprSelectionList_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
		{
			switch (e.CommandName.ToLower(System.Globalization.CultureInfo.InvariantCulture)) 
			{
				case "edit" :
					LinkID = Convert.ToInt32(e.CommandArgument);
					BindLinkEdit();
					break;
				case "delete" :
					int id = Convert.ToInt32(e.CommandArgument);
					Link link = Links.GetSingleLink(id);
					ConfirmDelete(id, link.Title);
					break;
				default:
					break;
			}			
		}

		private void lkbCancel_Click(object sender, System.EventArgs e)
		{
			ResetPostEdit(false);
		}

		private void lkbPost_Click(object sender, System.EventArgs e)
		{
			UpdateLink();
		}

		private void lkbNewLink_Click(object sender, System.EventArgs e)
		{
			ResetPostEdit(true);
		}
	}
}
