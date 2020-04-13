using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;

namespace wpf_scrollviewer
{
	/// <summary>
	/// ScrollViewerBitmap
	/// </summary>
	public partial class ScrollViewerBitmap
    {
		public bool InitializeBitmap( BitmapImage bitmapimage )
		{
		
		    try
                {
		            using (var outStream = new MemoryStream())
				       {
					        BitmapEncoder enc = new BmpBitmapEncoder();
					        enc.Frames.Add(BitmapFrame.Create(bitmapimage));
					        enc.Save(outStream);
					        bitmap = new Bitmap(outStream);
				       }
					float  fBrightness;   
					for(int x=0; x<bitmap.Width; x++)
                        {
                           for(int y=0; y<bitmap.Height; y++)
                                {
                                    fBrightness = ((Color)(bitmap.GetPixel(x, y))).GetBrightness();
								    if (fBrightness > fMax)
								        fMax = fBrightness;
								 	if (fBrightness < fMin)
								     fMin = fBrightness;
                                }
                        }
                    dC =  (	dMaxHeight - dMinHeight ) / ( fMax - fMin ) ;
				return true;
			    }
            catch(ArgumentException)
                {
                    MessageBox.Show("There was an error." +
                    "Check the path to the image file.");
				return false;
     		    }
		}

		Bitmap bitmap;
		float fMin = 1;
		float fMax = 0;
		double dMinHeight = 0;
		double dMaxHeight = 500;
		double dC;

        public double GetHeight (int x, int y)
        {
  		   return (((Color)(bitmap.GetPixel(x, y))).GetBrightness() - fMin) * dC;
		}
	}
}
