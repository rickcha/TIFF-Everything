using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApplication1
{
    public partial class Main : Form
    {
        /*
         * Variables to store header information
         */

        // File header from byte number 0 to 11
        private string ByteOrder;
        private int FileType;
        private int OffsetIFD1;
        private int NumOfDirectories;

        // Tag information from byte number 0 to 11
        private int SubfileType;
        private int ImageWidth;
        private int ImageLength;
        private int BitsPerSample;
        private int Compression;
        private int PhotometricInterpretation;
        private int StripOffsets;
        private int SamplesPerPixel;
        private int XResolution;
        private int YResolution;
        private int PlanarConfiguration;

        // Necessarily, the following variables are also set global
        private string fileDirectory;    // File location
        private byte[] buff;                    // Converts a TIFF image file onto a byte array
        private int[] arrGray;                  // Sequential values of the grayscale image
        private int countArrGray;               // Counts the number of elements in the array
        private int fileLength;                 // The length of a file in byte
        private int colorType = 0;              // Initially 0, 1 for RGB color, and 2 for Grayscale
        private int red = 0;                    // Indicates the red scale from 0 to 255
        private int green = 0;                  // Sets Green Colour Index
        private int blue = 0;                   // Sets Blue Colour Index
        private int gray = 0;                   // Sets Red Colour Index
        private int countRow = 1;               // Y value of the single pixel
        private int countColumn = 1;            // X value of the single pixel
        private int[] sorted_histogram;

        public Main()
        {
            InitializeComponent();
            btnGrayscale.Hide();
            btnHistogram.Hide();
            btnDithering.Hide();

            // Adjust form1 window size
            this.Width = 650;           
            this.Height = 250;
        }

        private void identify_tag(int number)
        {
            int tagStarts = OffsetIFD1 + 2;
            int tagEnds = OffsetIFD1 + 2 + number * 12; // 142 in our sample file
            while(tagStarts < tagEnds)
            {
                // Extracts Four Field Informations From 12 Byte IFD
                int tagID;
                int Type;
                int Count;
                int Value;

                int i = tagStarts;
                byte[] bTagID = { buff[i], buff[i + 1] };
                byte[] bType = { buff[i + 2], buff[i + 3] };
                byte[] bCount = { buff[i + 4], buff[i + 5], buff[i + 6], buff[i + 7] };
                byte[] bValue = { buff[i + 8], buff[i + 9], buff[i + 10], buff[i + 11] };

                if (ByteOrder == "II")
                {
                    bTagID.Reverse();
                    bType.Reverse();
                    bCount.Reverse();
                    bValue.Reverse();
                }

                tagID = BitConverter.ToInt16(bTagID, 0);
                Type = BitConverter.ToInt16(bType, 0);
                Count = BitConverter.ToInt32(bCount, 0);
                Value = BitConverter.ToInt32(bValue, 0);

                if (tagID == 256)
                    ImageWidth = Value;

                if (tagID == 257)
                    ImageLength = Value;

                if (tagID == 273)
                    StripOffsets = Value;

                tagStarts = tagStarts + 12;
            }
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            // Initially, no value is stored.
            red = 0;
            green = 0;
            blue = 0;
            countRow = 1;
            countColumn = 1;

            // if the window is open and file is selected,
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Saves file location and reads it into a byte array
                fileDirectory = openFileDialog1.FileName;
                buff = File.ReadAllBytes(fileDirectory);
                fileLength = buff.Length;
                
                /*
                 * From here, extract header information
                 */
                
                ASCIIEncoding ascii = new ASCIIEncoding();

                // Identify whether the order of bytes are little-endian or big-endian
                // If "II", then it is little-endian
                byte[] bByteOrder = { buff[0], buff[1] };
                ByteOrder = ascii.GetString(bByteOrder);

                // Identify whether the order of bytes are little-endian or big-endian
                // If 42, then it is TIFF file format
                byte[] bFileType = { buff[2], buff[3] };
                if (ByteOrder == "II")
                    bFileType.Reverse();
                
                FileType = BitConverter.ToInt16(bFileType, 0);

                // Identify the first IFD offset and store the value into OffsetIFD1
                byte[] bOffsetIFD1 = { buff[4], buff[5], buff[6], buff[7] };
                if (ByteOrder == "II")
                    bOffsetIFD1.Reverse();

                OffsetIFD1 = BitConverter.ToInt32(bOffsetIFD1, 0);

                // Identify the first IFD offset and store the value into OffsetIFD1
                byte[] bNumOfDirectories = { buff[8], buff[9] };
                if (ByteOrder == "II")
                    bNumOfDirectories.Reverse();

                NumOfDirectories = BitConverter.ToInt16(bNumOfDirectories, 0);

                identify_tag(NumOfDirectories); // Call the "identify_tag" methods to dig IFD information

                // Declares colour variable
                Color myRgbColor = new Color();
                
                // Reads file from 
                for (int i = StripOffsets; i <= fileLength - 2; i = i + 3)
                {
                    // The sequence is RGBRGB..
                    red = buff[i];
                    green = buff[i + 1];
                    blue = buff[i + 2];

                    // Create color and brush
                    myRgbColor = Color.FromArgb(red, green, blue);
                    System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(myRgbColor);
                    System.Drawing.Graphics formGraphics;
                    formGraphics = this.CreateGraphics();

                    // Choose the location where you would like to draw a rectangle
                    // And pick the size of a rectangle (one pixel)
                    formGraphics.FillRectangle(myBrush, new Rectangle(countColumn++, countRow, 1, 1));
                    myBrush.Dispose();
                    formGraphics.Dispose();

                    // If one row is filled with ImageWidth number of rectangles with one pixel, go to next row
                    if (countColumn % ImageWidth == 1)
                    {
                        countRow++;
                        countColumn = 1;
                    }
                }

            }
            btnGrayscale.Show();
            colorType = 1;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnGrayscale_Click(object sender, EventArgs e)
        {
            red = 0;
            green = 0;
            blue = 0;
            gray = 0;
            countRow = 1;
            countColumn = 1;
            countArrGray = 0;
            arrGray = new int[fileLength];

            Color myRgbColor = new Color();
            
            for (int i = StripOffsets; i <= fileLength - 2; i = i + 3)
            {
                if (ByteOrder == "II")
                {
                    blue = buff[i];
                    green = buff[i + 1];
                    red = buff[i + 2];
                }
                else 
                {
                    red = buff[i];
                    green = buff[i + 1];
                    blue = buff[i + 2];
                }

                // Convert RGB to grayscale and the formula is given as follows
                gray =  (int)(0.21 * red + 0.72 * green + 0.07 * blue);
                myRgbColor = Color.FromArgb(gray, gray, gray);
                arrGray[countArrGray++] = gray;

                // Same applies as the above
                System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(myRgbColor);
                System.Drawing.Graphics formGraphics;
                formGraphics = this.CreateGraphics();

                formGraphics.FillRectangle(myBrush, new Rectangle(250 + countColumn++, countRow, 1, 1));
                myBrush.Dispose();
                formGraphics.Dispose();

                if (countColumn % ImageWidth == 1)
                {
                    countRow++;
                    countColumn = 1;
                }
            }

            colorType = 2;
            btnHistogram.Show();

        }

        private void array_histogram ()
        {
            sorted_histogram = new int[256];
            for (int i = 0; i < 256; i++)
            {
                sorted_histogram[i] = 0;
            }

            foreach (int j in arrGray)
            {
                sorted_histogram[j]++;
            }
        }

        private void btnHistogram_Click(object sender, EventArgs e)
        {
            this.Width = 650;   // Adjust form1 window size
            this.Height = 560;

            array_histogram();

            PointF xAxis;       // A starting point - I am going to connect two points
            float x;            // X value of a starting point
            float xWave;        // X value of an end point
            float yWave;        // Y value of an end point
            
            PointF[] wavePts = new PointF[256];

            double interval = (double)600 / 256;

            for (int j = 0; j < 256; j++)
            {
                xWave = (float)(interval * j + 10);
                yWave = (float)(450 - sorted_histogram[j]*0.5);
                wavePts[j] = new PointF(xWave, yWave);
            }

            // Plot points from the "wavePts" array
            // And draw a line by connecting two points
            for (int l = 0; l < 256; l++)
            {
                x = (float)(interval * l + 10);
                xAxis = new PointF(x, 520);
                this.CreateGraphics().DrawLine(new Pen(Brushes.Black, 1), xAxis, wavePts[l]);
            }
            btnDithering.Show();
        }

        // Inputs X and Y position of a pixel and returns the grayscale value in the position
        private int I(int x, int y)
        {
            int value = y * ImageWidth + x;
            return arrGray[value];
        }

        // Inputs the position of a pixel and colour and outputs drawing
        private void O(int x, int y, int black)
        {
            // Make black and white colour
            Color myRgbColor;
            if (black == 1)
                myRgbColor = Color.FromArgb(255, 255, 255);
            else
                myRgbColor = Color.FromArgb(0, 0, 0);

            System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(myRgbColor);
            System.Drawing.Graphics formGraphics;
            formGraphics = this.CreateGraphics();

            // Plot the points in desired colour
            formGraphics.FillRectangle(myBrush, new Rectangle(x + 251, y + 1, 1, 1));
            myBrush.Dispose();
            formGraphics.Dispose();
        }

        // Returns pivot value at i and j position of the dithering matrix
        private int D(int i, int j)
        {
            int d = 255 / 5;
            // Two-dimensional array. 
            int[,] dithMat = new int[,] { { 0, 2 }, { 3, 1 } };
            int value = d * (dithMat[i, j] + 1);
            return value;
        }

        private void btnDithering_Click(object sender, EventArgs e)
        {
            int i;
            int j;
            for (int x = 0; x < ImageWidth; x++)
            {
                for (int y = 0; y < ImageLength; y++) 
                {
                    i = x % 2;
                    j = y % 2;
                    // if the grayscale value is greater than the pivot value
                    if (I(x, y) > D(i, j))
                        O(x, y, 1);     // Colour it in black
                    else
                        O(x, y, 0);     // Colour it in white
                }
            }
        }
    }
}