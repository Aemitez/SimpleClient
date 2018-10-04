using SimpleClient.Properties;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SimpleClient
{
    internal class SocketSend
    {
        private TcpClient _clientSocketSend = null;

        public void StartSend(string msg)
        {
            try
            {
                SentMessage(msg);
            }
            catch (Exception ex)
            {
                WriteLogSend("Error for StartSend, " + ex);
            }
        }

        private bool ConToServ(bool isChkAndClose = false)
        {
            try
            {
                if (_clientSocketSend == null)
                {
                    _clientSocketSend = new TcpClient { ReceiveTimeout = Settings.Default.Socket_Timeout };
                }

                //Check socket status
                if (_clientSocketSend.Client.Poll(0, SelectMode.SelectRead))
                {
                    var buff = new byte[1];
                    if (_clientSocketSend.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        _clientSocketSend = new TcpClient { ReceiveTimeout = Settings.Default.Socket_Timeout };
                    }
                }

                if (!_clientSocketSend.Client.Connected)
                {
                    _clientSocketSend.Client.Connect(IPAddress.Parse(Settings.Default.Socket_Server), Settings.Default.Socket_Port);
                }

                if (isChkAndClose) CloseSendConnection();
                return true;
            }
            catch (Exception e)
            {
                Thread.Sleep(100);
                WriteLogSend(string.Format("Error on ConToServ : {0}", e.Message));

                if (_clientSocketSend != null)
                {
                    _clientSocketSend.Client.Close();
                    _clientSocketSend = null;
                }
                return false;
            }
        }

        private void CloseSendConnection()
        {
            try
            {
                Thread.Sleep(10);
                if (_clientSocketSend != null)
                {
                    var ipClient = _clientSocketSend.Client.LocalEndPoint.ToString();
                    _clientSocketSend.Client.Close();
                    _clientSocketSend = null;

                    WriteLogSend(string.Format("Client [{0}] was Closed.", ipClient));
                }
            }
            catch (Exception ex)
            {
                WriteLogSend(string.Format("Error {0} on CloseSendConnection : {0}", ex.Message));
            }
        }

        private bool SentMessage(string msg)
        {
            try
            {
                string strToSend = msg;
                if (string.IsNullOrEmpty(strToSend))
                {
                    WriteLogSend("Send : skip an empty message");
                    return false;
                }

                int countSend = 0;
                while (countSend < 3)
                {
                    countSend++;
                    if (!ConToServ()) continue;

                    try
                    {
                        Thread.Sleep(200);

                        byte[] bytes = Encoding.GetEncoding(Settings.Default.Socket_Encode).GetBytes(strToSend);
                        WriteLogSend(string.Format("Client [{0}], Send : [{1}]", _clientSocketSend.Client.LocalEndPoint, strToSend));
                        _clientSocketSend.Client.Send(bytes);

                        //var byteAck = new byte[ConfigSocket.AckOK.Length];
                        var byteAck = new byte[4];
                        var amountData = _clientSocketSend.Client.Receive(byteAck, byteAck.Length, SocketFlags.None);
                        if (amountData < 1)
                        {
                            WriteLogSend(string.Format("Client [{0}], No acknowkedge message was reply.", _clientSocketSend.Client.LocalEndPoint));
                            CloseSendConnection();
                            return false;
                        }

                        var tmpAck = Encoding.GetEncoding(Settings.Default.Socket_Encode).GetString(byteAck).TrimEnd('\0');
                        if (String.Compare(tmpAck, "OK", StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            WriteLogSend(string.Format("Client [{0}], Acknowledge message receive : [{1}]", _clientSocketSend.Client.LocalEndPoint, tmpAck));

                            CloseSendConnection();
                            return true;
                        }
                        WriteLogSend(string.Format("Client [{0}], Invalid acknowledge message received : [{1}]", _clientSocketSend.Client.LocalEndPoint, tmpAck));

                        CloseSendConnection();
                        return false;
                    }
                    catch (Exception e1)
                    {
                        Thread.Sleep(100);
                        WriteLogSend(string.Format("Error SendMessage : {0}", e1.Message));
                        CloseSendConnection();
                    }
                }
                CloseSendConnection();
                return false;
            }
            catch (Exception e)
            {
                WriteLogSend(string.Format("Error SendMessage : {0}", e.Message));
                CloseSendConnection();
                return false;
            }
        }

        private void WriteLogSend(string logtext)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine(logtext);
            }
            //Logs.WriteData(logtext, appendFilename: "_Send");
        }
    }
}