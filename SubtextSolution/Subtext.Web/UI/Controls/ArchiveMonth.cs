using System.Globalization;
using Subtext.Framework;
using Subtext.Framework.Data;
using Subtext.Framework.Util;
using Subtext.Framework.Web;

#region Disclaimer/Info
///////////////////////////////////////////////////////////////////////////////////////////////////
// Subtext WebLog
// 
// Subtext is an open source weblog system that is a fork of the .TEXT
// weblog system.
//
// For updated news and information please visit http://subtextproject.com/
// Subtext is hosted at Google Code at http://code.google.com/p/subtext/
// The development mailing list is at subtext-devs@lists.sourceforge.net 
//
// This project is licensed under the BSD license.  See the License.txt file for more information.
///////////////////////////////////////////////////////////////////////////////////////////////////
#endregion

namespace Subtext.Web.UI.Controls
{
	using System;
    using Subtext.Web.Properties;

	/// <summary>
	///		Summary description for ArchiveMonth.
	/// </summary>
	public  class ArchiveMonth : BaseControl
	{
		protected EntryList Days;

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad (e);
			
			try
			{
                DateTime dt = SubtextContext.RequestContext.GetDateFromRequest();
				Days.EntryListItems = Cacher.GetMonth(dt, SubtextContext);
				Days.EntryListTitle = string.Format(CultureInfo.InvariantCulture, "{0} " + Resources.Label_Entries, dt.ToString("MMMM yyyy", CultureInfo.CurrentCulture));
				Globals.SetTitle(string.Format(CultureInfo.InvariantCulture, "{0} - {1} " + Resources.Label_Entries, Blog.Title, dt.ToString("MMMM yyyy", CultureInfo.CurrentCulture)),Context);
			}
			catch(FormatException)
			{
				HttpHelper.SetFileNotFoundResponse();
			}
			
		}
	}
}

