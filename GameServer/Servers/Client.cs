﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Common;
using MySql.Data.MySqlClient;
using GameServer.Tool;
namespace GameServer.Servers
{
    //这个类是每个客户端具体处理通信的一个类
    class Client
    {
        private Socket clientSocket;
        private Server server;
        private Message message = new Message();
        private MySqlConnection mysqlConn;
        public MySqlConnection MySqlConnection 
        {
            get { return mysqlConn; }
        }
        public Client() { }
        public Client(Socket clientSocket, Server server)
        {
            this.clientSocket = clientSocket;
            this.server = server;
            mysqlConn = ConnHelper.Connect();
        }
        public void Start() 
        {
            //开始接收
            clientSocket.BeginReceive(message.Data, message.StartIndex,message.RemainSize,SocketFlags.None,ReceiveCallBack,null);
        }
        private void ReceiveCallBack(IAsyncResult ar) 
        {
            try
            {
                int count = clientSocket.EndReceive(ar);
                if (count == 0)
                {
                    Close();
                }
                //TODO 处理接收到的数据
                message.ReadMessage(count,OnProcessMessage);

                Start();
                clientSocket.BeginReceive(null,0,0,SocketFlags.None,ReceiveCallBack,null);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                Close();
            }
            finally { }
        }
        private void OnProcessMessage(RequestCode requestCode, ActionCode actionCode, string data)
        {
            //消息解析成功后回调
            server.HandlerRequest(requestCode, actionCode, data, this);
        }
        public void Send(ActionCode actionCode,string data) 
        {
            byte[] bytes = Message.PackData(actionCode, data);
            //发送数据
            clientSocket.Send(bytes);
        }


        private void Close()
        {
            //先关闭与数据库的连接
            ConnHelper.CloseConnection(mysqlConn);
            if (clientSocket != null) 
            {
                clientSocket.Close();
                this.server.RemoveClient(this);
            };
        }
    }
}
