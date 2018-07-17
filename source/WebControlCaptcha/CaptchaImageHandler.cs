using System;
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
using System.Web;
using System.Drawing;


namespace WebControlCaptcha
{
	/// <summary>

	/// ''' 

	/// ''' Captcha image stream HttpModule

	/// ''' allows us to generate images in memory and stream them to the browser

	/// '''

	/// ''' You *MUST* enable this HttpHandler in your web.config, like so:

	/// '''

	/// '''   &lt;httpHandlers&gt;

	/// '''       &lt;add verb="GET" path="CaptchaImage.aspx" type="WebControlCaptcha.CaptchaImageHandler, WebControlCaptcha" /&gt;

	/// '''   &lt;/httpHandlers&gt;

	/// '''

	/// ''' Jeff Atwood

	/// ''' http://www.codinghorror.com/

	/// '''

	/// ''' </summary>
	public class CaptchaImageHandler : IHttpHandler
	{
		public void ProcessRequest(System.Web.HttpContext context)
		{
			HttpApplication app = context.ApplicationInstance;

			// -- get the unique GUID of the captcha; this must be passed in via querystring 
			string guid = app.Request.QueryString["guid"];
			CaptchaImage ci;
			object o;

			if (guid == "")
				// -- mostly for display purposes when in design mode
				// -- builds a CAPTCHA image with all default settings 
				// -- (this won't reflect any design time changes)
				ci = new CaptchaImage();
			else
			{
				// -- get the CAPTCHA from the ASP.NET cache by GUID
				o = app.Context.Cache.Get(guid);
				ci = (CaptchaImage)o;
				app.Context.Cache.Remove(guid);
			}

			if (ci == null)
			{
				app.Response.StatusCode = 404;
				app.Response.End();
				return;
			}

			// -- write the image to the HTTP output stream as an array of bytes 
			ci.Image.Save(app.Context.Response.OutputStream, System.Drawing.Imaging.ImageFormat.Jpeg);
			app.Response.ContentType = "image/jpeg";
			app.Response.StatusCode = 200;
			app.Response.End();
		}


		public bool IsReusable
		{
			get
			{
				return true;
			}
		}
	}
}