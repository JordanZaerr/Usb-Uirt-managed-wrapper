using System;
using System.Threading;
using System.Threading.Tasks;
using UsbUirt.Enums;
using UsbUirt.EventArgs;

namespace UsbUirt
{
    /// <summary>
    /// This class returns the code that you send and the 
    /// code that you should expect to receive for a given command.
    /// </summary>
    public class LearnHelper : DriverUserBase
    {
        private Receiver _receiver;
        private Learner _learner;
        private string _lastReceivedCode;
        private ManualResetEvent _waitHandle = new ManualResetEvent(false);

        public event EventHandler<LearnHelperCompletedEventArgs> LearningComplete;

        public LearnHelper(CodeFormat defaultCodeFormat = CodeFormat.Pronto, 
                       LearnCodeModifier defaultLearnCodeModifier = LearnCodeModifier.Default)
        {
            SetUpLearner(null, defaultCodeFormat, defaultLearnCodeModifier);
            SetUpReceiver();
        }

        public LearnHelper(Driver driver,
                       CodeFormat defaultCodeFormat = CodeFormat.Pronto,
                       LearnCodeModifier defaultLearnCodeModifier = LearnCodeModifier.Default)
            : base(driver)
        {
            SetUpLearner(driver, defaultCodeFormat, defaultLearnCodeModifier);
            SetUpReceiver(driver);
        }

        public void Learn(CodeFormat? codeFormat = null,
            LearnCodeModifier? learnCodeFormat = null,
            uint? forcedFrequency = null,
            TimeSpan? timeout = null)
        {
            _waitHandle.WaitOne();
            _learner.Learn(codeFormat, learnCodeFormat, forcedFrequency, timeout);
        }

        public Task LearnAsync(CodeFormat? codeFormat = null,
            LearnCodeModifier? learnCodeModifier = null,
            uint? forcedFrequency = null,
            object userState = null)
        {
            _waitHandle.WaitOne();
            return _learner.LearnAsync(codeFormat, learnCodeModifier, forcedFrequency, userState);
        }

        public void LearnCancel()
        {
            _learner.LearnAsyncCancel();
        }

        public void LearnCancel(object userState)
        {
            _learner.LearnAsyncCancel(userState);
        }

        private void SetUpLearner(Driver driver, CodeFormat defaultCodeFormat, LearnCodeModifier defaultLearnCodeModifier)
        {
            _learner = driver == null ? new Learner(defaultCodeFormat, defaultLearnCodeModifier) : new Learner(driver, defaultCodeFormat, defaultLearnCodeModifier);
            _learner.LearnCompleted += OnLearnComplete;
        }

        private void OnLearnComplete(object sender, LearnCompletedEventArgs e)
        {
            _waitHandle.Reset();
            var temp = LearningComplete;
            if (temp != null)
            {
                temp(this, new LearnHelperCompletedEventArgs(_lastReceivedCode, e));
            }
        }

        private void SetUpReceiver(Driver driver = null)
        {
            _receiver = driver == null ? new Receiver() : new Receiver(driver);
            _receiver.Received += OnRecieved;
        }

        private void OnRecieved(object sender, ReceivedEventArgs e)
        {
            _lastReceivedCode = e.IRCode;
            _waitHandle.Set();
        }
    }
}