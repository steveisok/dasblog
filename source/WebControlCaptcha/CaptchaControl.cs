using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections;
using System.Collections.Specialized;

namespace WebControlCaptcha
{
	/// <summary>

	/// '''

	/// ''' Captcha ASP.NET user control

	/// '''

	/// ''' add a reference to this DLL and add the CaptchaControl to your toolbox;

	/// ''' then just drag and drop the control on a web form and set properties on it.

	/// '''

	/// ''' Jeff Atwood

	/// ''' http://www.codinghorror.com/

	/// '''

	/// ''' </summary>
	[DefaultProperty("Text")]
	public class CaptchaControl : System.Web.UI.WebControls.WebControl, INamingContainer, IPostBackDataHandler
	{
		public enum Layout
		{
			Horizontal,
			Vertical
		}

		private int _intTimeoutSeconds = 0;
		private bool _blnUserValidated = false;
		private bool _blnShowSubmitButton = true;
		private string _strText = "Enter the code shown above:";
		private string _strFont = "";
		private CaptchaImage _captcha = new CaptchaImage();
		private Layout _LayoutStyle = Layout.Horizontal;

		public event UserValidationEventEventHandler UserValidationEvent;

		public delegate void UserValidationEventEventHandler(bool Validated);

		[DefaultValue("Enter the code shown above:")]
		[Description("Instructional text displayed next to CAPTCHA image.")]
		[Category("Captcha")]
		public string Text
		{
			get
			{
				return _strText;
			}
			set
			{
				_strText = value;
			}
		}

		[DefaultValue(typeof(CaptchaControl.Layout), "Horizontal")]
		[Description("Determines if image and input area are displayed horizontally, or vertically.")]
		[Category("Captcha")]
		public Layout LayoutStyle
		{
			get
			{
				return _LayoutStyle;
			}
			set
			{
				_LayoutStyle = value;
			}
		}

		[Description("Returns True if the user was CAPTCHA validated after a postback.")]
		[Category("Captcha")]
		public bool UserValidated
		{
			get
			{
				return _blnUserValidated;
			}
		}

		[DefaultValue(true)]
		[Description("Show a Submit button in the control to enable postback.")]
		[Category("Captcha")]
		public bool ShowSubmitButton
		{
			get
			{
				return _blnShowSubmitButton;
			}
			set
			{
				_blnShowSubmitButton = value;
			}
		}

		[DefaultValue("")]
		[Description("Font used to render CAPTCHA text. If font name is blankd, a random font will be chosen.")]
		[Category("Captcha")]
		public string CaptchaFont
		{
			get
			{
				return _strFont;
			}
			set
			{
				_strFont = value;
				_captcha.Font = _strFont;
			}
		}
        
		[DefaultValue("")]
		[Description("Characters used to render CAPTCHA text. A character will be picked randomly from the string.")]
		[Category("Captcha")]
		public string CaptchaChars
		{
			get
			{
				return _captcha.TextChars;
			}
			set
			{
				_captcha.TextChars = value;
			}
		}

		[DefaultValue(5)]
		[Description("Number of CaptchaChars used in the CAPTCHA text")]
		[Category("Captcha")]
		public int CaptchaLength
		{
			get
			{
				return _captcha.TextLength;
			}
			set
			{
				_captcha.TextLength = value;
			}
		}

		[DefaultValue(0)]
		[Description("Number of seconds this CAPTCHA is valid after it is generated. Zero means valid forever.")]
		[Category("Captcha")]
		public int CaptchaTimeout
		{
			get
			{
				return _intTimeoutSeconds;
			}
			set
			{
				_intTimeoutSeconds = value;
			}
		}

		[DefaultValue(typeof(CaptchaImage.FontWarpFactor), "Low")]
		[Description("Amount of random font warping used on the CAPTCHA text")]
		[Category("Captcha")]
		public CaptchaImage.FontWarpFactor CaptchaFontWarping
		{
			get
			{
				return _captcha.FontWarp;
			}
			set
			{
				_captcha.FontWarp = value;
			}
		}

		// -- viewstate is required, so let's hide this property
		[Browsable(false)]
		public override bool EnableViewState
		{
			get
			{
				return base.EnableViewState;
			}
			set
			{
				base.EnableViewState = value;
			}
		}

		/// <summary>
		///     ''' randomly generated captcha text is stored in the session 
		///     ''' using the control ID as a unique identifier
		///     ''' </summary>
		private string CaptchaText
		{
			get
			{
				if (HttpContext.Current.Session[this.ID + ".String"] == null)
					return "";
				else
					return Convert.ToString(HttpContext.Current.Session[this.ID + ".String"]);
			}
			set
			{
				if (value == "")
					HttpContext.Current.Session.Remove(this.ID + ".String");
				else
					HttpContext.Current.Session[this.ID + ".String"] = value;
			}
		}

		/// <summary>
		///     ''' date and time of Captcha generation is stored in the Session 
		///     ''' using the control ID as a unique tag
		///     ''' </summary>
		private Nullable<DateTime> GeneratedAt
		{
			get
			{
				if (HttpContext.Current.Session[this.ID + ".date"] == null)
					return DateTime.Now;
				else
					return Convert.ToDateTime(HttpContext.Current.Session[this.ID + ".date"]);
			}
			set
			{
				if (value.HasValue == false)
					HttpContext.Current.Session.Remove(this.ID + ".date");
				else
					HttpContext.Current.Session[this.ID + ".date"] = value;
			}
		}

		/// <summary>
		///     ''' guid used to identify the unique captcha image is stored in the page ViewState
		///     ''' </summary>
		private string LocalGuid
		{
			get
			{
				return Convert.ToString(ViewState["guid"]);
			}
			set
			{
				ViewState["guid"] = value;
			}
		}

		/// <summary>
		///     ''' are we in design mode?
		///     ''' </summary>
		private bool IsDesignMode
		{
			get
			{
				return HttpContext.Current == null;
			}
		}

		/// <summary>
		///     ''' Returns true if user input valid CAPTCHA text, and raises UserValidationEvent
		///     ''' </summary>
		private bool ValidateCaptcha(string strUserEntry)
		{
			if (string.Compare(strUserEntry, this.CaptchaText, true) == 0)
			{
				if (this.CaptchaTimeout == 0)
					_blnUserValidated = true;
				else
					// -- ok, it's valid, but was it entered quickly enough?
					_blnUserValidated = (this.GeneratedAt.HasValue) && this.GeneratedAt.Value.AddSeconds(this.CaptchaTimeout) > DateTime.Now;
			}
			else
				_blnUserValidated = false;

			UserValidationEvent?.Invoke(_blnUserValidated);

			return _blnUserValidated;
		}

		public bool LoadPostData(string PostDataKey, NameValueCollection Values)
		{
			ValidateCaptcha(Convert.ToString(Values[this.UniqueID]));
			GenerateNewCaptcha();
			return false;
		}

		public void RaisePostDataChangedEvent()
		{
		}

		/// <summary>
		///     ''' returns HTML-ized color strings
		///     ''' </summary>
		private string HtmlColor(Color color)
		{
			if (color.IsEmpty)
				return "";
			if (color.IsNamedColor)
				return color.ToKnownColor().ToString();
			if (color.IsSystemColor)
				return color.ToString();
			return "#" + color.ToArgb().ToString("x").Substring(2);
		}

		/// <summary>
		///     ''' returns css "style=" tag for this control
		///     ''' based on standard control visual properties
		///     ''' </summary>
		private string CssStyle()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			string strColor;

			{
				var withBlock = sb;
				withBlock.Append(" style='");

				if (BorderWidth.ToString().Length > 0)
				{
					withBlock.Append("border-width:");
					withBlock.Append(BorderWidth.ToString());
					withBlock.Append(";");
				}
				if (BorderStyle != System.Web.UI.WebControls.BorderStyle.NotSet)
				{
					withBlock.Append("border-style:");
					withBlock.Append(BorderStyle.ToString());
					withBlock.Append(";");
				}
				strColor = HtmlColor(BorderColor);
				if (strColor.Length > 0)
				{
					withBlock.Append("border-color:");
					withBlock.Append(strColor);
					withBlock.Append(";");
				}

				strColor = HtmlColor(BackColor);
				if (strColor.Length > 0)
					withBlock.Append("background-color:" + strColor + ";");

				strColor = HtmlColor(ForeColor);
				if (strColor.Length > 0)
					withBlock.Append("color:" + strColor + ";");

				if (Font.Bold)
					withBlock.Append("font-weight:bold;");

				if (Font.Italic)
					withBlock.Append("font-style:italic;");

				if (Font.Underline)
					withBlock.Append("text-decoration:underline;");

				if (Font.Strikeout)
					withBlock.Append("text-decoration:line-through;");

				if (Font.Overline)
					withBlock.Append("text-decoration:overline;");

				if (Font.Size.ToString().Length > 0)
					withBlock.Append("font-size:" + Font.Size.ToString() + ";");

				if (Font.Names.Length > 0)
				{
               
					withBlock.Append("font-family:");
					foreach (var strFontFamily in Font.Names)
					{
						withBlock.Append(strFontFamily);
						withBlock.Append(",");
					}
					withBlock.Length = withBlock.Length - 1;
					withBlock.Append(";");
				}

				if (Height.ToString() != "")
					withBlock.Append("height:" + Height.ToString() + ";");
				if (Width.ToString() != "")
					withBlock.Append("width:" + Width.ToString() + ";");

				withBlock.Append("'");
			}
			if (sb.ToString() == " style=''")
				return "";
			else
				return sb.ToString();
		}

		/// <summary>
		///     ''' render raw control HTML to the page
		///     ''' </summary>
		protected override void Render(HtmlTextWriter Output)
		{
			{
				var withBlock = Output;
				// -- master DIV
				withBlock.Write("<div");
				if (CssClass != "")
					withBlock.Write(" class='" + CssClass + "'");
				withBlock.Write(CssStyle());
				withBlock.Write(">");

				// -- image DIV/SPAN
				if (this.LayoutStyle == Layout.Vertical)
					withBlock.Write("<div style='text-align:center;margin:5px;'>");
				else
					withBlock.Write("<span style='margin:5px;float:left;'>");
				// -- this is the URL that triggers the CaptchaImageHandler
				withBlock.Write("<img src=\"CaptchaImage.aspx");
				if (!IsDesignMode)
					withBlock.Write("?guid=" + Convert.ToString(ViewState["guid"]));
				withBlock.Write("\" border='0' alt=\"[Captcha]\"");
				if (ToolTip.Length > 0)
					withBlock.Write(" alt='" + ToolTip + "'");
				withBlock.Write(" />");
				if (this.LayoutStyle == Layout.Vertical)
					withBlock.Write("</div>");
				else
					withBlock.Write("</span>");

				// -- text input and submit button DIV/SPAN
				if (this.LayoutStyle == Layout.Vertical)
					withBlock.Write("<div style='text-align:center;margin:5px;'>");
				else
					withBlock.Write("<span style='margin:5px;float:left;'>");
				if (_strText.Length > 0)
				{
					withBlock.Write(_strText);
					withBlock.Write("<br />");
				}
				withBlock.Write("<input name=\"" + UniqueID + "\" type=\"text\" size=\"");
				withBlock.Write(_captcha.TextLength.ToString());
				withBlock.Write("\" maxlength=\"");
				withBlock.Write(_captcha.TextLength.ToString());
				if (AccessKey.Length > 0)
					withBlock.Write("\" accesskey=\"" + AccessKey);
				if (!Enabled)
					withBlock.Write("\" disabled=\"disabled\"");
				if (TabIndex > 0)
					withBlock.Write("\" tabindex=\"" + TabIndex.ToString());
				withBlock.Write("\" value='' />");
				if (_blnShowSubmitButton)
				{
					withBlock.Write("&nbsp;");
					withBlock.Write("<input type=\"Submit\" value=\"Submit\"");
					if (!Enabled)
						withBlock.Write(" disabled=\"disabled\"");
					if (TabIndex > 0)
						withBlock.Write(" tabindex=\"" + TabIndex.ToString() + "\"");
					withBlock.Write(" />");
				}
				if (this.LayoutStyle == Layout.Vertical)
					withBlock.Write("</div>");
				else
				{
					withBlock.Write("</span>");
					withBlock.Write("<br clear='all' />");
				}

				// -- closing tag for master DIV
				withBlock.Write("</div>");
			}
		}

		/// <summary>
		///     ''' generate a new captcha and store it in the ASP.NET Cache by unique GUID
		///     ''' </summary>
		private void GenerateNewCaptcha()
		{
			LocalGuid = Guid.NewGuid().ToString();
			if (!IsDesignMode)
				// HttpContext.Current.Cache.Add(LocalGuid, _captcha, Nothing, DateTime.Now.AddMinutes(HttpContext.Current.Session.Timeout), TimeSpan.Zero, Caching.CacheItemPriority.Normal, Nothing)
				// No reason to keep the captcha in the cache very long.
				HttpContext.Current.Cache.Add(LocalGuid, _captcha, null/* TODO Change to default(_) if this is not a reference type */, DateTime.Now.AddMinutes(1), TimeSpan.Zero, System.Web.Caching.CacheItemPriority.High, null/* TODO Change to default(_) if this is not a reference type */);
			this.CaptchaText = _captcha.Text;
			this.GeneratedAt = DateTime.Now;
		}

		protected override void OnPreRender(EventArgs E)
		{
			if (LocalGuid == "")
				GenerateNewCaptcha();
		}
	}
}