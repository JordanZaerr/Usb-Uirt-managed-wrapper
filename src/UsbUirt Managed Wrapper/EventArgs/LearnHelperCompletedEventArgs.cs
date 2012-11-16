namespace UsbUirt.EventArgs
{
    public class LearnHelperCompletedEventArgs : LearnCompletedEventArgs
    {
        /// <summary>
        /// The code you should expect to receive from this command.
        /// </summary>
        public string ReceiveCode { get; set; }

        internal LearnHelperCompletedEventArgs(string receiveCode, LearnCompletedEventArgs eventArgs)
            : base(eventArgs.Error, eventArgs.Cancelled, eventArgs.Code, eventArgs.UserState)
        {
            ReceiveCode = receiveCode;
        }
    }
}