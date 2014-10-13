using System;
using System.Collections.Generic;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TwitchChatBot
{
    public class QuizObject : IEquatable<QuizObject>
    {
        public QuizObject(String inQuestion, String inAnswer)
        {
            Question = inQuestion;
            Answer = inAnswer;
        }

        public bool Equals(QuizObject other)
        {
            if (other == null)
            {
                return false;
            }
            else if (this.Question == other.Question)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            QuizObject quizObj = obj as QuizObject;
            if (quizObj == null)
            {
                return false;
            }
            else
            {
                return Equals(quizObj);
            }
        }

        public override int GetHashCode()
        {
            return Question.GetHashCode();
        }

        public static bool operator ==(QuizObject score1, QuizObject score2)
        {
            if ((object)score1 == null || (object)score2 == null)
            {
                return Object.Equals(score1, score2);
            }
            else
            {
                return score1.Equals(score2);
            }
        }

        public static bool operator !=(QuizObject score1, QuizObject score2)
        {
            if ((object)score1 == null || (object)score2 == null)
            {
                return !Object.Equals(score1, score2);
            }
            else
            {
                return !(score1.Equals(score2));
            }
        }


        public String Question { get; set; }
        public String Answer { get; set; }
    }

    public class ScoreObject : IEquatable<ScoreObject>
    {
        public ScoreObject(String inName, int inScore)
        {
            Name = inName;
            Score = inScore;
        }

        public ScoreObject(String inName) : this(inName, 0) { }

        public bool Equals(ScoreObject other)
        { 
            if( other == null)
            {
                return false;
            }
            else if (this.Name == other.Name)
            {
                return true;
            }
            else 
            {
                return false;    
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            ScoreObject scoreObj = obj as ScoreObject;
            if (scoreObj == null)
            {
                return false;
            }
            else {
                return Equals(scoreObj);
            }
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public static bool operator == (ScoreObject score1, ScoreObject score2)
        {
            if ((object)score1 == null || (object)score2 == null)
            {
                return Object.Equals(score1,score2);
            }
            else {
                return score1.Equals(score2);
            }
        }

        public static bool operator != (ScoreObject score1, ScoreObject score2)
        {
            if ((object)score1 == null || (object)score2 == null)
            {
                return ! Object.Equals(score1, score2);
            }
            else
            {
                return ! (score1.Equals(score2));
            }
        }

        public String Name { get; set; }
        public int Score { get; set; }
    }

    public static class ScoreObjectExtensions
    {
        public static ScoreObject AsScoreObject(this String inName)
        {
            return new ScoreObject(inName);
        }
    }

    public class QuizHint
    {
        public QuizHint(string inAnswer)
        {
            mAnswer = inAnswer;
            mHint = new string('_', inAnswer.Length);
            HintNum = 0;
        }

        public string GiveAHint()
        {
            HintNum++;
            OpenChar();
            return mHint;
        }

        void OpenChar()
        {
            if ( (HintNum - 2) >= 0 && (HintNum - 1) < mHint.Length ) {
                //mHint[HintNum - 1] = mAnswer[HintNum - 1];
                char[] temp = new char[mHint.Length];
                mHint.CopyTo(0, temp, 0, mHint.Length);
                temp[HintNum - 2] = mAnswer[HintNum - 2];
                mHint = new string(temp);
            }
        }

        static int HintNum
        {
            get;
            set;
        }

        string mHint;

        string mAnswer;
    }

	public class QuizEngine : INotifyPropertyChanged
	{
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

		public QuizEngine ()
		{
			mQuizQueue = new Queue<Tuple<string, string>>();
            mQuizList = new List<QuizObject>();
            mScoreList = new List<ScoreObject>();


            mIncomingMessagesQueue = new Queue<IrcCommand>();
            mScore = new Dictionary<string, int>();
            mTimeBetweenQuestions = 60000;
            mTimeBetweenHints = 15000;
		}

		public QuizEngine (string inFileWithQuiz) : this()
		{
            AddQuiz(inFileWithQuiz);
		}

		public QuizEngine (string[] inQuiz) : this()
		{
			ProcessStringsArrayAsQuiz(inQuiz);
		}

        public async Task AddQuiz() {
            await AddQuiz(QuizFile);
        }

        void Processing()
        {
            using (FileStream fs = File.OpenRead(QuizFile))
            {

                string[] linesOfFile = File.ReadAllLines(QuizFile);
                ProcessStringsArrayAsQuiz(linesOfFile);
            }
           
        }

        async public Task  AddQuiz(string inFileWithQuiz)
        {
            QuizFile = inFileWithQuiz;
        	if (File.Exists (inFileWithQuiz)) {
                await Task.Run((System.Action)Processing);
			} else {
				throw new FileNotFoundException();
			}
        }

		void ProcessStringsArrayAsQuiz(string[] inStringsArray)
		{
			string[] separators = new string[]{"{Question}","{Answer}"};

			for(int i = 0; i < inStringsArray.Length; i++ ){
				string[] result = inStringsArray[i].Split(separators,StringSplitOptions.RemoveEmptyEntries);
				if(result.Length == 2)
				{
                    mQuizList.Add(new QuizObject(result[0], result[1]));
					mQuizQueue.Enqueue(new Tuple<string, string>(result[0],result[1]));
				}
			}

            QuizList = QuizList;
		}

		public void Process (IrcCommand inCommand)
		{
			if (inCommand.Name == "PRIVMSG") {
				mIncomingMessagesQueue.Enqueue(inCommand);
			}
		}

        async Task<IrcCommand> GetMessageFromQ()
        { 
            while(mIncomingMessagesQueue.Count == 0)
            {
                await Task.Delay(100);
            }
            return mIncomingMessagesQueue.Dequeue();
        }

        async Task ReadIncomingMessages(CancellationToken ct)
        {
            while (true)
            {
                Task<IrcCommand> tic = Task.Run((Func<Task<IrcCommand>>)GetMessageFromQ,ct);
                IrcCommand ic = await tic;

                //here we have a privmsg and have to check for a valid answer
                if (mCurrentQAPair != null && ic.Prefix != null)
                {
                   
                    Console.WriteLine("{0} is guessed it is \"{1}\" ({2})!", ic.Prefix, ic.Parameters[ic.Parameters.Length - 1].Value, mCurrentQAPair.Item2);

                    if (ic.Parameters[ic.Parameters.Length - 1].Value == mCurrentQAPair.Item2)
                    {

                        int indexOfExclamationSign = ic.Prefix.IndexOf('!');
                        string name = ic.Prefix.Substring(0, indexOfExclamationSign);

                        if (mScore.ContainsKey(name))
                        {
                            mScore[name]++;
                        }
                        else 
                        {
                            mScore[name] = 1;
                        }

                        ScoreObject scoreObj = name.AsScoreObject();
                        if (mScoreList.Contains(scoreObj))
                        {
                            mScoreList[mScoreList.IndexOf(scoreObj)].Score++;
                        }
                        else 
                        {
                            scoreObj.Score++;
                            mScoreList.Add(scoreObj);
                        }
                        Score = Score; //Notifying


                        //Console.WriteLine("{0} is right, it is \"{1}\" !", name, mCurrentQAPair.Item2);
                        string message = String.Format("{0} is right, it is \"{1}\", {0}'s score is {2}!", name, mCurrentQAPair.Item2, mScore[name]);
                        //SendMessage(new IrcCommand(null, "PRIVMSG", new IrcCommandParameter("#sovietmade", false), new IrcCommandParameter(message, true)).ToString() + "\r\n");
                        SendMessage(message);
                        OnTimeToAskAQuestion(null, null);
                    }
                    Console.WriteLine("after GetMessageFromQ:{0} ", ic.Name);
                }
            }
        }

        public void StopQuiz()
        {
            if (cts != null)
            {
                mTimeToAskAQuestion.Enabled = false;
                mTimeToGiveAHint.Enabled = false;
                cts.Cancel();
                cts = null;
            }
        }

        async public void StartQuiz()
        {
            if (mQuizQueue.Count == 0) {
                throw new InvalidDataException("mQuizQueue is empty!");
            }

            if (cts != null) {
                mTimeToAskAQuestion.Enabled = false;
                mTimeToGiveAHint.Enabled = false;
                cts.Cancel();
            }
            cts = new CancellationTokenSource();

            mTimeToAskAQuestion = new System.Timers.Timer(mTimeBetweenQuestions);
            mTimeToGiveAHint = new System.Timers.Timer(mTimeBetweenHints);

            mTimeToAskAQuestion.Elapsed += OnTimeToAskAQuestion;
            mTimeToAskAQuestion.Enabled = true;

            mTimeToGiveAHint.Elapsed += OnTimeToGiveAHint;

            //OnTimeToAskAQuestion(null,null);
            OnTimeToAskAQuestion(null, null);
            await ReadIncomingMessages(cts.Token);
        }

        void OnTimeToAskAQuestion(object source, ElapsedEventArgs e)
        {
            //if there is no more items in Q, then the exception will be rised
            if (mQuizQueue.Count == 0) {
                return;
                //throw new InvalidDataException("mQuizQueue is empty!");
            }
            
            mCurrentQAPair = mQuizQueue.Dequeue();
            CurrentQuizObject = new QuizObject(mCurrentQAPair.Item1, mCurrentQAPair.Item2);
            mQuizHint = new QuizHint(mCurrentQAPair.Item2);
            mTimeToGiveAHint.Enabled = true;

            //SendMessage(new IrcCommand(null,"PRIVMSG", new IrcCommandParameter("#sovietmade",false), new IrcCommandParameter(mCurrentQAPair.Item1,true)).ToString() + "\r\n");
            SendMessage(mCurrentQAPair.Item1);
        }

        void OnTimeToGiveAHint(object source, ElapsedEventArgs e)
        {
            string currentHint = mQuizHint.GiveAHint();
            //SendMessage(new IrcCommand(null, "PRIVMSG", new IrcCommandParameter("#sovietmade", false), new IrcCommandParameter(currentHint, true)).ToString() + "\r\n");
            SendMessage(currentHint);
        }

        public int TimeBetweenQuestions 
        {
            get {
                return mTimeBetweenQuestions;
            }
            set {
                mTimeBetweenQuestions = value;
            }
        }

        public int TimeBetweenHints
        {
            get
            {
                return mTimeBetweenHints;
            }
            set
            {
                mTimeBetweenHints = value;
            }
        }

        public QuizObject[] QuizList
        {
            get {
                return mQuizList.ToArray();
            }
            set
            {
                NotifyPropertyChanged();
            }
        }

        public ScoreObject[] Score
        {
            get {
                return mScoreList.ToArray();
            }
            set {
                NotifyPropertyChanged();
            }
        }

        public string QuizFile { get; set; }

        //delegate for communicating back to the outer world (assigned to SendMessageToCurrentChannel in TwitchBot contructor)
		public Action<string> SendMessage;

        //Queue of PRIVMSGes - source of answers
		Queue<IrcCommand> mIncomingMessagesQueue;
		
        //Queue of tuples, containing questions and answers
        Queue<Tuple<string,string>> mQuizQueue;
        List<QuizObject> mQuizList;
        List<ScoreObject> mScoreList;

        public QuizObject CurrentQuizObject
        {
            get {
                return mCurrentObject;
            }
            set {
                mCurrentObject = value;
                NotifyPropertyChanged();
            }
        }

        QuizObject mCurrentObject;

        System.Timers.Timer mTimeToAskAQuestion;
        System.Timers.Timer mTimeToGiveAHint;

        Tuple<string, string> mCurrentQAPair;
        Dictionary<string, int> mScore;

        CancellationTokenSource cts;
        
        int mTimeBetweenQuestions;
        int mTimeBetweenHints;

        QuizHint mQuizHint;
	}
}

