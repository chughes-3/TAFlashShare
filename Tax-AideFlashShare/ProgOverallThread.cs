using System;
using System.Text;
using System.Threading;

namespace TaxAideFlashShare
{
    class ProgOverallThread
    {
        public static ProgessOverall progOverallWin; //the window
        public delegate void ProgUpdateDelegate(string txtMessAdditional); // delegate for invoking progress window update 1 string parameter
        public static ProgUpdateDelegate progressUpdate; // progress window methods that are invoked on a different thread
        public ProgOverallThread()
        {
            progOverallWin = new ProgessOverall(); //get teh progress form initialized
            Thread progressThread = new Thread(new ThreadStart(progOverallWin.ProgShow)); // starts progress window in new thread initial text is in progshow method
            progressThread.Start();
            Thread.Sleep(200); //allow window to appear
            progressUpdate = new ProgUpdateDelegate(progOverallWin.AddTxtLine); //delegate for later use in updating window
        }
    }
}
