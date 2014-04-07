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

namespace bob
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BigInteger n, p, q, R;
        int k = 5, t = 3;
        Random rnd = new Random();
        List<BigInteger> s = new List<BigInteger>();
        List<BigInteger> w = new List<BigInteger>();
        BigInteger u = 1;
        Socket soc = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        List<int> b = new List<int>();
        IPAddress mcast = IPAddress.Parse("239.255.10.10");
        BinaryFormatter bft;
        MemoryStream ms;
        byte[] sendArr;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(500);
            //primzahl & bum bang blum zahl finden 
            while (true)
            {
                p = Primes();
                q = Primes();
                
                
                if ((MillerRabin(p) && MillerRabin(q)) && BumBumBlum(p,q))
                {
                    n = p * q;               
                    break;  
                } 
            }
            //zufalls vektor s
            for (int i = 0; i < k; i++)
            {
                while (true)
                {
                    byte[] temp2 = new byte[8];              
                    rnd.NextBytes(temp2);
                    BigInteger temp = 0;
                    try { temp = BitConverter.ToUInt64(temp2, 0); }
                    catch (Exception ex) { MessageBox.Show(ex.Message); }

                    if (temp > 0 && temp <= (n - 1) && (ModInverse(temp*temp,n) *(temp*temp)) % n == 1)
                    {
                        s.Add(temp);
                        break;
                    } 
                }   
            }
            //public w
            //zuerst n ganz vorne dran hängen damits auch rüber kommt, pfuscher code.
            //n noch nicht versendet brate!
            for (int i = 0; i < k; i++)
            {
                BigInteger temp = (BigInteger)Math.Pow(-1, rnd.Next(0, 2)) * ModInverse((s[i] * s[i]), n);
                if (temp < 0)
                {
                    w.Add(temp + n);
                }
                else
                {
                    w.Add(temp);
                }
            }
            w.Add(n);

            foreach (BigInteger l in w)
            {
                lst1.Items.Add(l.ToString());
            }

            BinaryFormatter bft = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bft.Serialize(ms, w);
            byte[] wListSer = ms.ToArray();
            byte[] sendArr = new byte[wListSer.Length+1];
            sendArr[0] = (byte)'w';
            Buffer.BlockCopy(wListSer,0,sendArr,1,wListSer.Length);
            soc.SendTo(sendArr, new IPEndPoint(mcast, 5555));



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
                    case 'r':
                        while (true)
                        {
                            BigInteger v=0;
                            byte[] temp2 = new byte[8];
                            rnd.NextBytes(temp2);
                            BigInteger temp = BitConverter.ToUInt64(temp2, 0);
                            if (temp > 0 && temp <= (n - 1))
                            {
                                R = temp;
                                int wurf = rnd.Next(0,1001);
                                if(wurf % 2 == 0)
                                {
                                v = (R * R) % n;
                                }
                                else
                                {
                                    v = ((R * R)%n) * - 1;
                                    v += n;
                                }
                                bft = new BinaryFormatter();
                                ms = new MemoryStream();
                                bft.Serialize(ms, v);
                                byte[] vSer = ms.ToArray();
                                sendArr = new byte[vSer.Length + 1];
                                sendArr[0] = (byte)'s';
                                Buffer.BlockCopy(vSer, 0, sendArr, 1, vSer.Length);
                                soc.SendTo(sendArr, new IPEndPoint(mcast, 5555));
                                break;
                            } 
                        }
                        break;
                    case'b':
                        bft = new BinaryFormatter();                       
                        byte[] bListDeSer = new byte[bytes - 1];
                        Buffer.BlockCopy(buffer, 1, bListDeSer, 0, bytes - 1);
                        ms = new MemoryStream(bListDeSer);
                        b = (List<int>)bft.Deserialize(ms);
                        u = R;
                        for (int i = 0; i < k; i++)
                        {
                            if (b[i] == 1)
                            {
                                u *= s[i] % n;
                            }
                        }
                        ms = new MemoryStream();
                        bft.Serialize(ms, u);
                        byte[] uSer = ms.ToArray();
                        sendArr = new byte[uSer.Length + 1];
                        sendArr[0] = (byte)'u';
                        Buffer.BlockCopy(uSer, 0, sendArr, 1, uSer.Length);
                        soc.SendTo(sendArr, new IPEndPoint(mcast, 5555));
                        Dispatcher.Invoke((Action)delegate 
                        {
                            lst1.Items.Add("u: " + u.ToString());
                        });
                        
                        ////HIER GESTOPPT;

                        break;
                }
            }
        }

        private ulong Primes()
        {
            byte[] array = new byte[sizeof(UInt64)];
            rnd.NextBytes(array);
            ulong p = BitConverter.ToUInt64(array, 0);
            p = p | 0x00000001;
            return p;
        }
        private bool MillerRabin(BigInteger zahl)
        {
            if (zahl == 2 || zahl == 3 || zahl == 5)
                return true;
            if (zahl < 2 || (zahl & 1) == 0)
                return false;

            BigInteger d = zahl - 1;
            int s = 0;

            while ((d & 1) == 0)
            {
                d /= 2;
                s++;
            }

            /* nach vier Versuchen hat man bereits eine Wahrscheinlichkeit von unter 0,04% */
            for (int a = 2; a < 6; a++)
            {
                BigInteger x = BigInteger.ModPow(a, d, zahl);
                if (x == 1 || x == zahl - 1)
                    continue;

                for (int r = 1; r < s; r++)
                {
                    x = BigInteger.ModPow(x, 2, zahl);
                    if (x == 1)
                        return false;
                    if (x == zahl - 1)
                        break;
                }

                if (x != zahl - 1)
                    return false;
            }
            return true;
        }
        private bool BumBumBlum(BigInteger eins, BigInteger zwei)
        {
            if ((eins % 4 == 3) && (zwei % 4 == 3))
            {
                return true;
            }
            else return false;
        }
        private BigInteger ModInverse(BigInteger a, BigInteger b)
        {
            BigInteger dividend = a % b;
            BigInteger divisor = b;

            BigInteger last_x = BigInteger.One;
            BigInteger curr_x = BigInteger.Zero;

            while (divisor.Sign > 0)
            {
                BigInteger quotient = dividend / divisor;
                BigInteger remainder = dividend % divisor;
                if (remainder.Sign <= 0)
                {
                    break;
                }

                /* This is quite clever, in the algorithm in form
                 * ax + by = gcd(a, b) we only keep track of the
                 * value curr_x and the last_x from last iteration,
                 * the y value is ignored anyway. After remainder
                 * runs to zero, we get our inverse from curr_x */
                BigInteger next_x = last_x - curr_x * quotient;
                last_x = curr_x;
                curr_x = next_x;

                dividend = divisor;
                divisor = remainder;
            }

            if (divisor != BigInteger.One)
            {
                throw new Exception("Numbers a and b are not relatively primes");
            }
            return (curr_x.Sign < 0 ? curr_x + b : curr_x);
        }
    
    }
}
