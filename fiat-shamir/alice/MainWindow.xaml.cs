using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace alice
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IPAddress mcast = IPAddress.Parse("239.255.10.10");
        List<BigInteger> w = new List<BigInteger>();
        List<BigInteger> vAll = new List<BigInteger>();
        Socket soc = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        Random rnd = new Random();
        BigInteger k = 5, t = 3,tempT,v,tempV,n;
        int vCount=0;
        List<int> b = new List<int>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(mcast, IPAddress.Any));
            socket.Bind(new IPEndPoint(IPAddress.Any, 5555));

            Thread thread = new Thread(this.Listen);
            thread.IsBackground = true;
            thread.Start(socket);
        }
        private void Listen(object o)
        {
            Socket socket = (Socket)o;
            byte[] buffer = new byte[1024];
            int bytes;
            EndPoint from = new IPEndPoint(IPAddress.Loopback, 0);
            while (true)
            {
                bytes = socket.ReceiveFrom(buffer, ref from);
                //received bytes in byte[] buffer
                switch ((char)buffer[0])
                {
                    case 'w':
                        byte[] wListDeSer = new byte[bytes - 1];
                        Buffer.BlockCopy(buffer, 1, wListDeSer, 0, bytes - 1);
                        BinaryFormatter bft = new BinaryFormatter();
                        MemoryStream ms = new MemoryStream(wListDeSer);
                        w = (List<BigInteger>)bft.Deserialize(ms);
                        //foreach (BigInteger l in w)
                        //{
                        //    MessageBox.Show(l.ToString());
                        //}
                        byte[] sendArr = new byte[1];
                        sendArr[0] = (byte)'r';
                        soc.SendTo(sendArr, new IPEndPoint(mcast, 5555));
                        tempT = t - 1;
                        n = w[w.Count-1];
                        w.Remove(w[w.Count-1]);
                        break;
                    case 's':
                        if(tempT >= 0)
                        {

                            byte[] vDeSer = new byte[bytes - 1];
                            Buffer.BlockCopy(buffer, 1, vDeSer, 0, bytes - 1);
                            ms = new MemoryStream(vDeSer);
                            bft = new BinaryFormatter();
                            v = (BigInteger)bft.Deserialize(ms);
                            vAll.Add(v);
                            for (int j = 0; j < k; j++)
                            {
                                b.Add(rnd.Next(0, 2));
                            }
                            ms = new MemoryStream();
                            bft = new BinaryFormatter();
                            bft.Serialize(ms, b);
                            byte[] bListSer = ms.ToArray();
                            sendArr = new byte[bListSer.Length + 1];
                            sendArr[0] = (byte)'b';
                            Buffer.BlockCopy(bListSer, 0, sendArr, 1, bListSer.Length);
                            soc.SendTo(sendArr, new IPEndPoint(mcast, 5555));
                            tempT -= 1;

                        }
                        break;
                     case 'u':
                        //MessageBox.Show(b.Count + ":" + w.Count);
                        byte[] uDeSer = new byte[bytes - 1];
                        Buffer.BlockCopy(buffer, 1, uDeSer, 0, bytes - 1);
                        ms = new MemoryStream(uDeSer);
                        bft = new BinaryFormatter();
                        BigInteger u = (BigInteger)bft.Deserialize(ms);
                        tempV = (u * u);
                        for (int i = 0; i < w.Count; i++)
                        {
                            if (b[i] == 1)
                            {
                                tempV *= w[i];
                            }
                        }
                        // modulo n noch machen hier.
                        tempV %= n;

                        if (tempV != vAll[vCount])
                        {
                            tempV = n - tempV;
                        }

                            Dispatcher.Invoke((Action)delegate
                            {
                                lst1.Items.Add("v': "+tempV.ToString());
                                lst1.Items.Add("v : "+vAll[vCount].ToString());
                            });



                        

                        

                        vCount++;
                        b.Clear();
                        byte[] repeat = new byte[1];
                        repeat[0] = (byte)'r';
                        soc.SendTo(repeat, new IPEndPoint(mcast, 5555));
                        break;
                }
            }
        }
    }
}
