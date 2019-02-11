using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZombieUpdater
{
    public class App
    {
        private static void Main(string[] args)
        {
            var counter = 0;
            while (counter < 5)
            {
                Thread.Sleep(10000);
                counter++;
            }
        }
    }
}
