using SimpleClient.Properties;
using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace SimpleClient
{
    internal class Program
    {
 
        private static void Main(string[] args)
        {


            try
            {
                SocketSend sd = new SocketSend();

                /*----------START HERE---------*/
                while (true)
                {
                    XElement xReader = XElement.Load(AppDomain.CurrentDomain.BaseDirectory + "Msg.xml");
                    var message = xReader.Elements().Select(xName => xName.Elements());
                    Console.Clear();
                    Console.WriteLine("Update at : " + DateTime.Now);

                    foreach (var subElement in message)
                    {
                        if (subElement.Where(xName => xName.Name.LocalName.ToLower() == "active" && xName.Value == "1")
                                .Select(x => x)
                                .FirstOrDefault() == null) continue;

                        var s = subElement.Where(xCol => xCol.Name.LocalName.ToLower() == "data").Select(xData => xData.Value.ToString()).FirstOrDefault();

                        sd.StartSend(s);
                    }

                    //Refresh XML
                    Thread.Sleep(Settings.Default.RefreshRate);
                }

                /*-----------------------------*/
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}