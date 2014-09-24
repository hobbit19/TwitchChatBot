using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
namespace TwitchChatBot
{
	public class TwitchBot
	{
		public TwitchBot () 
		{
			mTcpConnection = new TcpConnection();
			mTcpConnection.DataReceived += ProccessMessageData;
			mIrcCommandAnalyzer = new SimpleTwitchBotIrcCommandAnalyzer();
			//string[] Quiz = new string[]{"{Question}hey?{Answer}hey there!"};
            //string Quiz = @"C:\Users\����\Documents\GitHub\TwitchChatBot\Quiz.txt";
			//mQE = new QuizEngine(Quiz);
            //mQE.SendMessage = SendMessage;
            //mQE.StartQuiz();
            StartHttpListener();
            mQE = new QuizEngine();
            mQE.SendMessage = SendMessage;
		}

        public void StartHttpListener()
        {
            mListener = new HttpListener();
            mListener.Prefixes.Add("http://localhost:6555/");
            mListener.Start();
            mListener.BeginGetContext(ListenerCallback, mListener);
        }

        void ListenerCallback(IAsyncResult result)
        {
            HttpListenerContext context = mListener.EndGetContext(result);
            //HttpListenerRequest request = context.Request;
            //Console.WriteLine(request.RawUrl);
            HttpListenerResponse response = context.Response;
            string stringResponse = "<HTML>" +
                                        "<BODY>" +
                                            "QuizBot" +
                                            //"<button type=\"button\" onclick=\"myFunction()\">Date</button>"+
                                            "<p id=\"demo\"></p>" +
                                            "<script>" +
                                                "var x = location.hash;" +
                                                "document.getElementById(\"demo\").innerHTML = x;" +
                                            "</script>" +
                                            //"<script>" +
                                            //"function myFunction() {" +
                                            //    "document.getElementById(\"demo\").innerHTML = Date();" +
                                            //"}" +
                                            //"</script>" +
                                        "</BODY>" +
                                    "</HTML>";
            byte[] bufferResponse = Encoding.UTF8.GetBytes(stringResponse);
            response.ContentLength64 = bufferResponse.Length;
            response.OutputStream.Write(bufferResponse, 0, bufferResponse.Length);
            response.OutputStream.Close();
            mListener.BeginGetContext(ListenerCallback, mListener);
        }

		public Endpoint Proxy {
			get {
				return mTcpConnection.Proxy;
			}
			set{
				mTcpConnection.Proxy = value;
			}
		}

		public Endpoint Destination {
			get {
				return mTcpConnection.Destination;
			}
			set{
				mTcpConnection.Destination = value;
			}
		}

		public void Connect ()
		{
			mTcpConnection.Connect();
		}


		public void SendMessage (string inMessage)
		{
			mTcpConnection.SendMessage(inMessage);
		}

		void ProccessMessageData(object sender, ReceivedDataArgs ea)
		{
			//Collecting all data from mMessageBuffer and ReceivedDataArgs in the single buffer - tempTotalData.
			//Collected data will be parsed for distinct commands.
			//Collecting data...
			int MessagesBufferLength = (int)mMessagesBuffer.Length;
			byte[] tempTotalData = new byte[MessagesBufferLength + ea.Data.Length];
			mMessagesBuffer.ToArray().CopyTo(tempTotalData,0);
			Array.Copy(ea.Data,0,tempTotalData,MessagesBufferLength,ea.Data.Length);

			mMessagesBuffer.SetLength(0);
			//Collecting finishes here.
			//Parsing data...

		    int msgStart = 0;
		    for (int i = 0; i < tempTotalData.Length - 1; i++)
		    {
		        if (tempTotalData[i] == 13 && tempTotalData[i + 1] == 10) // byte 10 = LF and byte 13 = CR
		        {
		            byte[] message = new byte[i - msgStart];
		            Array.Copy(tempTotalData, msgStart, message, 0, i - msgStart); // Copy data[msgStart:i] to message
					//Console.WriteLine("Command Received: {0}",Encoding.UTF8.GetString(message));
					string inMessage = Encoding.UTF8.GetString(message);
					if(inMessage.Length > 0){
						mMessageQ.Enqueue(inMessage);
						Console.WriteLine("COMMAND NAME: {0}",IrcCommand.Parse(inMessage).Name);
                        Console.WriteLine("PARAMETER: ");
                        foreach (var p in IrcCommand.Parse(inMessage).Parameters) {
                            Console.WriteLine(p.ToString());
                        }
                        IrcCommand incCommand = IrcCommand.Parse(inMessage);
                        IrcCommand outCommand = mIrcCommandAnalyzer.GetResponse(IrcCommand.Parse(inMessage));
                        if (outCommand != null)
                        {
                            SendMessage(outCommand.ToString());
                        }
                        if (incCommand.Name == "PRIVMSG")
                        {
                            mQE.Process(incCommand);
                        }
					}
					msgStart = i = i + 2;
		        }
		    }
 
		    // What is left from msgStart til the end of data is only a partial message.
		    // We want to save that for when the rest of the message arrives.
		    mMessagesBuffer.Write(tempTotalData, msgStart, tempTotalData.Length - msgStart);

		}

        public void StartQuiz()
        {
            string Quiz = @"C:\Users\Yuri\Documents\GitHub\TwitchChatBot\TwitchChatBotGUI\Quiz.txt";
            //string Quiz = @"C:\Quiz.txt";
            mQE.AddQuiz(Quiz);
            mQE.StartQuiz();
        }

		public void DumpMessageQ ()
		{
			foreach (var i in mMessageQ) {
				Console.WriteLine(i);
			}
		}

		TcpConnection mTcpConnection;
		Queue<string> mMessageQ = new Queue<string>();
		MemoryStream mMessagesBuffer = new MemoryStream();
		IrcCommandAnalyzer mIrcCommandAnalyzer;
		QuizEngine mQE;
        HttpListener mListener;
	}
}

