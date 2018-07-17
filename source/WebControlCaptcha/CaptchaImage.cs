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
using Microsoft.VisualBasic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace WebControlCaptcha
{
	/// <summary>

	/// ''' 

	/// ''' Captcha image generation class

	/// '''

	/// ''' Adapted from the excellent code at 

	/// '''    http://www.codeproject.com/aspnet/CaptchaImage.asp

	/// '''

	/// ''' Jeff Atwood

	/// ''' http://www.codinghorror.com/

	/// ''' 

	/// ''' </summary>
	public class CaptchaImage : IDisposable
	{
		private string _strFontFamilyName;
		private int _intHeight;
		private int _intWidth;
		private Random _rand;
		private string _strText;
		private Bitmap _image;
		private int _intRandomTextLength;
		private string _strRandomTextChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
		private FontWarpFactor _fontWarp;

		private const string _strGoodFontList = "arial; arial black; comic sans ms; courier new; estrangelo edessa; franklin gothic medium; " + "georgia; lucida console; lucida sans unicode; mangal; microsoft sans serif; palatino linotype; " + "sylfaen; tahoma; times new roman; trebuchet ms; verdana;";

		/// <summary>
		///     ''' Amount of random font warping to apply to rendered text
		///     ''' </summary>
		public enum FontWarpFactor
		{
			None,
			Low,
			Medium,
			High,
			Extreme
		}


		/// <summary>
		///     ''' returns the rendered Captcha image based on the current property values
		///     ''' </summary>
		public Bitmap Image
		{
			get
			{
				if (_image == null)
					_image = GenerateImagePrivate();
				return _image;
			}
		}

		/// <summary>
		///     ''' Font family to use when drawing the Captcha text 
		///     ''' </summary>
		public string Font
		{
			get
			{
				return _strFontFamilyName;
			}
			set
			{
				try
				{
					Font font1 = new Font(value, (float)12.0);
					_strFontFamilyName = value;
					font1.Dispose();
				}
				catch (Exception ex)
				{
					_strFontFamilyName = FontFamily.GenericSerif.Name;
				}
			}
		}

		/// <summary>
		///     ''' Amount of random warping to apply to the Captcha text.
		///     ''' </summary>
		public FontWarpFactor FontWarp
		{
			get
			{
				return _fontWarp;
			}
			set
			{
				_fontWarp = value;
			}
		}

		/// <summary>
		///     ''' A string of valid characters to use in the Captcha text. 
		///     ''' A random character will be selected from this string for each character.
		///     ''' </summary>
		public string TextChars
		{
			get
			{
				return _strRandomTextChars;
			}
			set
			{
				_strRandomTextChars = value;
			}
		}

		/// <summary>
		///     ''' Number of characters to use in the Captcha text. 
		///     ''' </summary>
		public int TextLength
		{
			get
			{
				return _intRandomTextLength;
			}
			set
			{
				_intRandomTextLength = value;
			}
		}

		/// <summary>
		///     ''' Returns the randomly generated Captcha text.
		///     ''' </summary>
		public string Text
		{
			get
			{
				if (_strText.Length == 0)
					_strText = GenerateRandomText();
				return _strText;
			}
			set
			{
				_strText = value;
			}
		}

		/// <summary>
		///     ''' Width of Captcha image to generate, in pixels 
		///     ''' </summary>
		public int Width
		{
			get
			{
				return _intWidth;
			}
			set
			{
				if ((value <= 60))
					throw new ArgumentOutOfRangeException("width", value, "width must be greater than 60.");
				_intWidth = value;
			}
		}

		/// <summary>
		///     ''' Height of Captcha image to generate, in pixels 
		///     ''' </summary>
		public int Height
		{
			get
			{
				return _intHeight;
			}
			set
			{
				if (value <= 30)
					throw new ArgumentOutOfRangeException("height", value, "height must be greater than 30.");
				_intHeight = value;
			}
		}


		public CaptchaImage()
		{
			_rand = new Random();
			this.FontWarp = FontWarpFactor.Low;
			this.Width = 180;
			this.Height = 50;
			this.TextLength = 5;
			this.Font = RandomFontFamily();
			this.Text = "";
		}

		/// <summary>
		///     ''' Forces a new Captcha image to be generated using current property values
		///     ''' </summary>
		public void GenerateImage()
		{
			_image = GenerateImagePrivate();
		}

		/// <summary>
		///     ''' returns random font family name
		///     ''' .. that's not on our font blacklist (illegible for CAPTCHA)
		///     ''' </summary>
		private string RandomFontFamily()
		{
			System.Drawing.Text.InstalledFontCollection fc = new System.Drawing.Text.InstalledFontCollection();
			System.Drawing.FontFamily[] ff = fc.Families;
			string strFontFamilyName = "bogus";

			while (_strGoodFontList.IndexOf(strFontFamilyName) == -1)
				strFontFamilyName = ff[_rand.Next(0, fc.Families.Length)].Name.ToLower();

			// Debug.WriteLine(strFontFamilyName)
			return strFontFamilyName;
		}

		/// <summary>
		///     ''' generate human friendly random text
		///     ''' eg, try to avoid numbers and characters that look alike
		///     ''' </summary>
		private string GenerateRandomText()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			int n;
			int intMaxLength = _strRandomTextChars.Length;
			for (n = 0; n <= _intRandomTextLength - 1; n++)
				sb.Append(_strRandomTextChars.Substring(_rand.Next(intMaxLength), 1));
			return sb.ToString();
		}

		private PointF RandomPoint(int xmin, int xmax, ref int ymin, ref int ymax)
		{
			return new PointF(_rand.Next(xmin, xmax), _rand.Next(ymin, ymax));
		}

		private Bitmap GenerateImagePrivate()
		{
			RectangleF rectF;
			Rectangle rect;

			SizeF ef1;
			Font font1;
			Bitmap bitmap1 = new Bitmap(_intWidth, _intHeight, PixelFormat.Format32bppArgb);
			Graphics graphics1 = Graphics.FromImage(bitmap1);

			graphics1.SmoothingMode = SmoothingMode.AntiAlias;

			rectF = new RectangleF(0, 0, _intWidth, _intHeight);
			rect = new Rectangle(0, 0, _intWidth, _intHeight);

			HatchBrush brush1 = new HatchBrush(HatchStyle.SmallConfetti, Color.LightGray, Color.White);
			graphics1.FillRectangle(brush1, rect);

			float prevWidth = 0;
			float fsize = Convert.ToInt32(_intHeight * 0.8);
			do
			{
				font1 = new Font(_strFontFamilyName, fsize, FontStyle.Bold);
				ef1 = graphics1.MeasureString(this.Text, font1);

				// -- does our text fit in the rect?
				if (ef1.Width <= _intWidth)
					break;

				// -- doesn't fit in the rect, scale font down and try again.
				if (prevWidth > 0)
				{
					int intEstSize = Convert.ToInt32((ef1.Width - _intWidth) / (double)(prevWidth - ef1.Width));
					if (intEstSize > 0)
						fsize = fsize - intEstSize;
					else
						fsize -= 1;
				}
				else
					fsize -= 1;
				prevWidth = ef1.Width;
			}
			while (true);

			// -- the resulting font size is usally conservative, so bump it up a few sizes.
			fsize += 4;
			font1 = new Font(_strFontFamilyName, fsize, FontStyle.Bold);

			// -- get our textpath for the given font/size combo
			StringFormat sf = new StringFormat();
			sf.Alignment = StringAlignment.Center;
			sf.LineAlignment = StringAlignment.Center;
			GraphicsPath textPath = new GraphicsPath();
			textPath.AddString(this.Text, font1.FontFamily, System.Convert.ToInt32(font1.Style), font1.Size, rect, sf);

			// -- are we warping the text?
			if (this.FontWarp != FontWarpFactor.None)
			{
				int intWarpDivisor = 6;

				switch (_fontWarp)
				{
					case FontWarpFactor.Low:
						{
							intWarpDivisor = 6;
							break;
						}

					case FontWarpFactor.Medium:
						{
							intWarpDivisor = 5;
							break;
						}

					case FontWarpFactor.High:
						{
							intWarpDivisor = 4;
							break;
						}

					case FontWarpFactor.Extreme:
						{
							intWarpDivisor = 3;
							break;
						}
				}

				int intHrange;
				int intWrange;
				int intMin;
				int intMin3;

				intHrange = Convert.ToInt32(rect.Height / (double)intWarpDivisor);
				intWrange = Convert.ToInt32(rect.Width / (double)intWarpDivisor);
				intMin = 0;

                
				PointF p1 = RandomPoint(0, intWrange, ref intMin, ref intHrange);
				PointF p2 = RandomPoint(rect.Width - (intWrange - Convert.ToInt32(p1.X)), rect.Width, ref intMin, ref intHrange);

				intMin3 = rect.Height - (intHrange - Convert.ToInt32(p1.Y));
				intHrange = rect.Height;

				PointF p3 = RandomPoint(0, intWrange, ref intMin3, ref intHrange);

				intMin3 = rect.Height - (intHrange - Convert.ToInt32(p2.Y));


				PointF p4 = RandomPoint(rect.Width - (intWrange - Convert.ToInt32(p3.X)), rect.Width, ref intMin3, ref intHrange);

				PointF[] points = new PointF[] { p1, p2, p3, p4 };
				Matrix m = new Matrix();
				m.Translate(0, 0);
				textPath.Warp(points, rectF, m, WarpMode.Perspective, 0);
			}
            
			// -- write our (possibly warped) text
			brush1 = new HatchBrush(HatchStyle.LargeConfetti, Color.LightGray, Color.DarkGray);
			graphics1.FillPath(brush1, textPath);

			// -- add noise to image
			int intMaxDim = Math.Max(rect.Width, rect.Height);
			int i;
			for (i = 0; i <= Convert.ToInt32(((rect.Width * rect.Height) / (double)30)); i++)
				graphics1.FillEllipse(brush1, _rand.Next(rect.Width), _rand.Next(rect.Height), _rand.Next(Convert.ToInt32(intMaxDim / (double)50)), _rand.Next(Convert.ToInt32(intMaxDim / (double)50)));

			// -- it's important to clean up unmanaged resources
			font1.Dispose();
			brush1.Dispose();
			graphics1.Dispose();

			return bitmap1;
		}


		public virtual void Dispose()
		{
			GC.SuppressFinalize(this);
			this.Dispose(true);
		}

		public virtual void Dispose(bool disposing)
		{
			if (disposing)
				this.Image.Dispose();
		}

		~CaptchaImage()
		{
			this.Dispose(false);
		}
	}
}